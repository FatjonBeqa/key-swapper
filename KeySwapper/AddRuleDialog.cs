using static KeySwapper.Theme;

namespace KeySwapper;

sealed class AddRuleDialog : Form
{
    static readonly string[] QuickChars = { "ë", "Ë", "ç", "Ç", "€" };

    public Rule? Result { get; private set; }

    public AddRuleDialog(IReadOnlyList<Rule> existing)
    {
        Text = "Add rule";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font(UiFont, 9.75f);
        BackColor = Theme.Card;
        ForeColor = Theme.Text;
        ClientSize = new Size(D(360), D(300));

        int x = D(20), width = ClientSize.Width - 2 * x;

        var fromLabel = new Label { Text = "When I press", AutoSize = true, ForeColor = SubText, Location = new Point(x, D(16)) };
        var capture = new KeyCaptureBox { Location = new Point(x, D(38)), Size = new Size(width, D(46)) };

        var toLabel = new Label { Text = "Type instead", AutoSize = true, ForeColor = SubText, Location = new Point(x, D(98)) };
        var toBox = new TextBox { Location = new Point(x, D(120)), Width = width, MaxLength = 8, Font = new Font(MonoFont, 12f) };

        var error = new Label
        {
            AutoSize = false, Location = new Point(x, D(206)), Size = new Size(width, D(22)), ForeColor = Danger,
        };

        const int buttonWidth = 90;
        var save = new Button
        {
            Text = "Save", Size = new Size(D(buttonWidth), D(32)), FlatStyle = FlatStyle.Flat,
            BackColor = Accent, ForeColor = OnAccent,
            Location = new Point(ClientSize.Width - x - D(buttonWidth), D(248)),
        };
        save.FlatAppearance.BorderSize = 0;
        var cancel = new Button
        {
            Text = "Cancel", Size = new Size(D(buttonWidth), D(32)), DialogResult = DialogResult.Cancel,
            Location = new Point(save.Left - D(8) - D(buttonWidth), D(248)),
        };

        Controls.AddRange(new Control[] { fromLabel, capture, toLabel, toBox, error, cancel, save });

        for (int i = 0; i < QuickChars.Length; i++)
        {
            string c = QuickChars[i];
            var quick = new Button
            {
                Text = c, Size = new Size(D(36), D(32)), Location = new Point(x + i * D(42), D(160)),
                Font = new Font(MonoFont, 11f), TabStop = false,
            };
            quick.Click += (_, _) =>
            {
                toBox.Text = c;
                toBox.Focus();
                toBox.SelectionStart = toBox.TextLength;
            };
            Controls.Add(quick);
        }

        AcceptButton = save;
        CancelButton = cancel;

        capture.KeyCaptured += () =>
        {
            error.Text = "";
            toBox.Focus();
            toBox.SelectAll();
        };
        capture.Rejected += () => error.Text = "Ctrl and Alt combinations aren't supported.";
        toBox.TextChanged += (_, _) => error.Text = "";

        save.Click += (_, _) =>
        {
            if (capture.Vk == 0)
            {
                error.Text = "Press the key you want to swap first.";
                capture.Focus();
                return;
            }
            if (toBox.Text.Length == 0)
            {
                error.Text = "Enter the character to type instead.";
                toBox.Focus();
                return;
            }
            if (existing.Any(r => r.Vk == capture.Vk && r.Shift == capture.Shift))
            {
                error.Text = "You already have a rule for that key.";
                capture.Focus();
                return;
            }

            Result = new Rule { Vk = capture.Vk, Shift = capture.Shift, From = capture.Display, To = toBox.Text };
            DialogResult = DialogResult.OK;
        };

        Shown += (_, _) => capture.Focus();
    }
}
