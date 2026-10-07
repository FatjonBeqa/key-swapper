using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace KeySwapper;

sealed class Rule
{
    public int Vk { get; set; }
    public bool Shift { get; set; }
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

sealed class Settings
{
    public bool Enabled { get; set; } = true;
    public List<Rule> Rules { get; set; } = new();

    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KeySwapper", "settings.json");

    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Set when the saved file existed but couldn't be read; the app should tell the user.</summary>
    public static string? LoadProblem { get; private set; }

    /// <summary>True when there was no saved file, i.e. the app has never run on this account.</summary>
    public static bool IsFirstRun { get; private set; }

    public static Settings Load()
    {
        if (!File.Exists(FilePath))
        {
            IsFirstRun = true;
            return Defaults();
        }

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath, Encoding.UTF8))
                       ?? throw new JsonException("The file is empty.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && attempt < 10)
            {
                Thread.Sleep(200); // briefly locked, e.g. by antivirus; try again
            }
            catch (Exception ex)
            {
                // Keep a copy so the defaults saved later never destroy the user's rules.
                string backup = $"{FilePath}.backup-{DateTime.Now:yyyyMMdd-HHmmss}";
                try { File.Copy(FilePath, backup, overwrite: true); } catch { }
                LoadProblem = "The default rules are loaded for now. A copy of your old settings was kept here:\n\n" +
                              backup + "\n\nDetails: " + ex.Message;
                return Defaults();
            }
        }
    }

    static Settings Defaults() => new()
    {
        Rules =
        {
            new Rule { Vk = (int)Keys.Oemtilde, Shift = false, From = "`", To = "ë" },
            new Rule { Vk = (int)Keys.Oemtilde, Shift = true, From = "~", To = "Ë" },
            new Rule { Vk = (int)Keys.OemPipe, Shift = true, From = "|", To = "ç" },
        },
    };

    public void Save()
    {
        string json = JsonSerializer.Serialize(this, JsonOptions);
        for (int attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, json, Encoding.UTF8);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(100); // briefly locked; try again
            }
            catch
            {
                return; // not worth crashing over; the rules still work for this session
            }
        }
    }

    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "KeySwapper";

    public static bool StartWithWindows
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(RunName) is string;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value)
                key.SetValue(RunName, $"\"{Environment.ProcessPath}\" --tray");
            else
                key.DeleteValue(RunName, throwOnMissingValue: false);
        }
    }
}

static class KeyNames
{
    /// <summary>What the key types on the current layout (e.g. "`" or "~"), or its name as a fallback.</summary>
    public static string Describe(int vk, bool shift)
    {
        var state = new byte[256];
        if (shift)
            state[Native.VK_SHIFT] = 0x80;

        var buffer = new StringBuilder(8);
        uint scan = Native.MapVirtualKey((uint)vk, 0);
        // Flag 0x4: don't disturb the keyboard's dead-key state.
        int n = Native.ToUnicodeEx((uint)vk, scan, state, buffer, buffer.Capacity, 0x4, Native.GetKeyboardLayout(0));
        string typed = n > 0 ? buffer.ToString(0, n) : n < 0 && buffer.Length > 0 ? buffer.ToString(0, 1) : "";

        if (typed.Length > 0 && !string.IsNullOrWhiteSpace(typed) && !char.IsControl(typed[0]))
            return typed;

        string name = ((Keys)vk).ToString();
        return shift ? "Shift+" + name : name;
    }
}
