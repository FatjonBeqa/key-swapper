using static KeySwapper.Theme;

namespace KeySwapper;

/// <summary>A themed replacement for MessageBox, so prompts follow the Windows light/dark setting.</summary>
sealed class MessageDialog : Form
{
    /// <param name="action">Plain button that returns OK (e.g. "Quit"), or null for a single-button notice.</param>
    /// <param name="safe">Highlighted default button (Enter/Esc) that returns Cancel.</param>
    public MessageDialog(string heading, string body, string? action, string safe)
    {
        Text = "Key Swapper";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font(UiFont, 9.75f);
        BackColor = Theme.Card;
        ForeColor = Theme.Text;

        int pad = D(24), width = D(420), textWidth = width - 2 * pad;

        var title = new Label
        {
            Text = heading, AutoSize = true, Location = new Point(pad, D(20)),
            Font = new Font("Segoe UI Semibold", 12f),
        };
        var message = new Label
        {
            Text = body, ForeColor = SubText, Location = new Point(pad, D(56)),
            MaximumSize = new Size(textWidth, 0), AutoSize = true,
        };

        var footer = new Panel { Dock = DockStyle.Bottom, Height = D(64), BackColor = Back };
        const int buttonWidth = 120;
        int right = width - pad - D(buttonWidth);
        var safeButton = new Button
        {
            Text = safe, Size = new Size(D(buttonWidth), D(32)), DialogResult = DialogResult.Cancel,
            Location = new Point(right, D(16)),
        };
        footer.Controls.Add(safeButton);
        if (action != null)
        {
            // Windows 11 order: highlighted default on the left, the other choice on the right.
            var actionButton = new Button
            {
                Text = action, Size = new Size(D(buttonWidth), D(32)), DialogResult = DialogResult.OK,
                Location = new Point(right, D(16)),
            };
            safeButton.Left = right - D(8) - D(buttonWidth);
            safeButton.FlatStyle = FlatStyle.Flat;
            safeButton.BackColor = Accent;
            safeButton.ForeColor = OnAccent;
            safeButton.FlatAppearance.BorderSize = 0;
            footer.Controls.Add(actionButton);
        }

        Controls.Add(title);
        Controls.Add(message);
        Controls.Add(footer);

        // The safe choice is the default, so a stray Enter never does anything drastic.
        AcceptButton = safeButton;
        CancelButton = safeButton;

        ClientSize = new Size(width, message.Bottom + D(24) + footer.Height);
        Shown += (_, _) => safeButton.Focus();
    }

    public static bool Confirm(IWin32Window? owner, string heading, string body, string action, string safe)
    {
        using var dialog = new MessageDialog(heading, body, action, safe);
        return dialog.ShowDialog(owner) == DialogResult.OK;
    }

    public static void Notice(IWin32Window? owner, string heading, string body)
    {
        using var dialog = new MessageDialog(heading, body, null, "OK");
        dialog.ShowDialog(owner);
    }
}
