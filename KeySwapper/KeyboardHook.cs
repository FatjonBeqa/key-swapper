using System.ComponentModel;
using System.Runtime.InteropServices;
using static KeySwapper.Native;

namespace KeySwapper;

/// <summary>System-wide low-level keyboard hook that replaces matching keys with Unicode text.</summary>
sealed class KeyboardHook : IDisposable
{
    readonly Settings settings;
    readonly LowLevelKeyboardProc proc; // kept in a field so the GC doesn't collect the callback
    readonly HashSet<int> swallowKeyUp = new();
    IntPtr hookId;

    /// <summary>When true, every key passes through untouched (used while recording a key).</summary>
    public bool Suspended { get; set; }

    /// <summary>Raised when Ctrl+Alt+K is pressed. Runs inside the hook, so handlers should post work.</summary>
    public event Action? ToggleRequested;

    public KeyboardHook(Settings settings)
    {
        this.settings = settings;
        proc = Callback;
        hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(null), 0);
        if (hookId == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Couldn't install the keyboard hook.");
    }

    IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0 && !Suspended && Handle((int)wParam, Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam)))
                return 1;
        }
        catch
        {
            // Never let an exception escape into the hook chain.
        }
        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    /// <returns>true to swallow the key.</returns>
    bool Handle(int msg, KBDLLHOOKSTRUCT k)
    {
        if ((k.flags & LLKHF_INJECTED) != 0)
            return false; // our own SendInput output, or another tool's

        int vk = (int)k.vkCode;
        bool down = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
        bool up = msg is WM_KEYUP or WM_SYSKEYUP;

        if (up)
            return swallowKeyUp.Remove(vk);
        if (!down)
            return false;

        bool ctrl = IsDown(VK_CONTROL), alt = IsDown(VK_MENU), shift = IsDown(VK_SHIFT);
        bool win = IsDown(VK_LWIN) || IsDown(VK_RWIN);

        if (ctrl && alt && !shift && !win && vk == (int)Keys.K)
        {
            swallowKeyUp.Add(vk);
            ToggleRequested?.Invoke();
            return true;
        }

        if (!settings.Enabled || ctrl || alt || win)
            return false; // leave shortcuts like Ctrl+` alone

        var rule = settings.Rules.FirstOrDefault(r => r.Enabled && r.Vk == vk && r.Shift == shift);
        if (rule == null || rule.To.Length == 0)
            return false;

        swallowKeyUp.Add(vk);
        SendText(rule.To);
        return true;
    }

    static void SendText(string text)
    {
        var inputs = new INPUT[text.Length * 2];
        for (int i = 0; i < text.Length; i++)
        {
            inputs[i * 2] = Unicode(text[i], keyUp: false);
            inputs[i * 2 + 1] = Unicode(text[i], keyUp: true);
        }
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    static INPUT Unicode(char c, bool keyUp) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wScan = c,
                dwFlags = KEYEVENTF_UNICODE | (keyUp ? KEYEVENTF_KEYUP : 0),
            },
        },
    };

    public void Dispose()
    {
        if (hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(hookId);
            hookId = IntPtr.Zero;
        }
    }
}
