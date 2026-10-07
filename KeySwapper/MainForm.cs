using static KeySwapper.Theme;

namespace KeySwapper;

sealed class MainForm : Form
{
    const int FromX = 44, ToX = 184;

    readonly Settings settings;
    readonly KeyboardHook hook;
    readonly Label title;
    readonly ToggleSwitch toggle;
    readonly FlowLayoutPanel list;
    readonly Label empty;
    readonly (int On, int From, int To) centers;
    bool syncing;

    public event Action<bool>? EnabledToggled;

    public MainForm(Settings settings, KeyboardHook hook, Icon icon)
    {
        this.settings = settings;
        this.hook = hook;

        Text = "Key Swapper";
        Icon = icon;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font(UiFont, 9.75f);
        BackColor = Back;
        ForeColor = Theme.Text;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        ClientSize = new Size(D(380), D(400));
        MinimumSize = new Size(D(370), D(340));
        Padding = new Padding(D(16));

        // Header: on/off switch
        var header = new Card { Dock = DockStyle.Top, Height = D(72) };
        title = new Label
        {
            AutoSize = true, Location = new Point(D(16), D(14)), BackColor = Theme.Card, ForeColor = Theme.Text,
            Font = new Font("Segoe UI Semibold", 11f),
        };
        var subtitle = new Label
        {
            AutoSize = true, Location = new Point(D(16), D(40)), BackColor = Theme.Card, ForeColor = SubText,
            Text = "Toggle anytime with Ctrl+Alt+K",
        };
        toggle = new ToggleSwitch { BackColor = Theme.Card };
        header.Controls.AddRange(new Control[] { title, subtitle, toggle });
        header.Resize += (_, _) =>
            toggle.Location = new Point(header.Width - D(18) - toggle.Width, (header.Height - toggle.Height) / 2);
        toggle.CheckedChanged += (_, _) =>
        {
            if (!syncing) EnabledToggled?.Invoke(toggle.Checked);
        };

        var gap = new Panel { Dock = DockStyle.Top, Height = D(12) };

        // Rules list
        var rulesCard = new Card { Dock = DockStyle.Fill, Padding = new Padding(D(1), D(4), D(1), D(8)) };
        var columns = new Panel { Dock = DockStyle.Top, Height = D(28), BackColor = Theme.Card };
        var onLabel = ColumnLabel("On", D(12));
        var fromLabel = ColumnLabel("When I press", D(FromX));
        var toLabel = ColumnLabel("Type instead", D(ToX));
        columns.Controls.AddRange(new Control[] { onLabel, fromLabel, toLabel });
        // Each row centers its checkbox and keys under these headings.
        centers = (Center(onLabel), Center(fromLabel), Center(toLabel));

        // Small "i" above the trash icons: opens the About box.
        var about = new Label
        {
            Text = Icons.Info, Font = Icons.Font(9f), AutoSize = true, ForeColor = Muted, BackColor = Theme.Card,
            Cursor = Cursors.Hand, Padding = new Padding(D(3)),
        };
        columns.Controls.Add(about);
        new ToolTip().SetToolTip(about, "About Key Swapper");
        about.MouseEnter += (_, _) => about.ForeColor = Accent;
        about.MouseLeave += (_, _) => about.ForeColor = Muted;
        about.Click += (_, _) =>
        {
            using var dialog = new AboutDialog();
            dialog.ShowDialog(this);
        };
        int trashWidth;
        using (var trashFont = Icons.Font(11f))
            trashWidth = TextRenderer.MeasureText(Icons.Delete, trashFont).Width + D(8);
        columns.Resize += (_, _) => about.Location = new Point(
            columns.Width - D(12) - trashWidth / 2 - about.Width / 2, (columns.Height - about.Height) / 2);
        columns.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawLine(pen, 0, columns.Height - 1, columns.Width, columns.Height - 1);
        };

        list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = Theme.Card,
        };
        list.Resize += (_, _) => SizeRows();
        empty = new Label
        {
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, BackColor = Theme.Card, ForeColor = Muted,
            Text = "No rules yet.\nAdd one to start swapping keys.", Visible = false,
        };
        // Dock order: fill controls first, the top strip last.
        rulesCard.Controls.Add(list);
        rulesCard.Controls.Add(empty);
        rulesCard.Controls.Add(columns);

        // Footer
        var footer = new Panel { Dock = DockStyle.Bottom, Height = D(48) };
        var add = new Button { Text = "+  Add rule", AutoSize = true, Padding = new Padding(D(6), D(2), D(6), D(2)) };
        var startup = new CheckBox { Text = "Start with Windows", AutoSize = true, Checked = Settings.StartWithWindows };
        footer.Controls.Add(add);
        footer.Controls.Add(startup);
        footer.Resize += (_, _) =>
        {
            add.Location = new Point(0, footer.Height - add.Height);
            startup.Location = new Point(footer.Width - startup.Width, add.Top + (add.Height - startup.Height) / 2);
        };
        add.Click += (_, _) => AddRule();
        startup.CheckedChanged += (_, _) =>
        {
            try
            {
                Settings.StartWithWindows = startup.Checked;
            }
            catch (Exception ex)
            {
                MessageDialog.Notice(this, "Couldn't change the startup setting", ex.Message);
            }
        };

        Controls.Add(rulesCard);
        Controls.Add(gap);
        Controls.Add(header);
        Controls.Add(footer);

        RefreshState();
        RebuildRules();
    }

    static int Center(Label label) => label.Left + label.PreferredSize.Width / 2;

    static Label ColumnLabel(string text, int x) => new()
    {
        Text = text, AutoSize = true, Location = new Point(x, D(6)), ForeColor = Muted, BackColor = Theme.Card,
        Font = new Font(UiFont, 9f),
    };

    public void RefreshState()
    {
        syncing = true;
        toggle.Checked = settings.Enabled;
        syncing = false;
        title.Text = settings.Enabled ? "Swapping is on" : "Swapping is off";
    }

    void RebuildRules()
    {
        list.SuspendLayout();
        var old = list.Controls.Cast<Control>().ToList();
        list.Controls.Clear();
        old.ForEach(c => c.Dispose());

        foreach (var rule in settings.Rules)
            list.Controls.Add(new RuleRow(rule, centers, settings.Save, DeleteRule));

        empty.Visible = settings.Rules.Count == 0;
        list.Visible = !empty.Visible;
        SizeRows();
        list.ResumeLayout();
    }

    void SizeRows()
    {
        foreach (Control row in list.Controls)
            row.Width = list.ClientSize.Width;
    }

    void DeleteRule(Rule rule)
    {
        settings.Rules.Remove(rule);
        settings.Save();
        // The row being clicked is about to be disposed, so rebuild after its click handler returns.
        BeginInvoke(RebuildRules);
    }

    void AddRule()
    {
        hook.Suspended = true; // so the key being recorded isn't swapped by an existing rule
        try
        {
            using var dialog = new AddRuleDialog(settings.Rules);
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Result != null)
            {
                settings.Rules.Add(dialog.Result);
                settings.Save();
                RebuildRules();
            }
        }
        finally
        {
            hook.Suspended = false;
        }
    }

    sealed class RuleRow : Panel
    {
        public RuleRow(Rule rule, (int On, int From, int To) centers, Action changed, Action<Rule> delete)
        {
            Height = D(46);
            Margin = Padding.Empty;
            BackColor = Theme.Card;

            var check = new CheckBox { Checked = rule.Enabled, AutoSize = true, BackColor = Theme.Card };
            var from = new KeyCap { Text = rule.From, Top = D(8), Dimmed = !rule.Enabled };
            var arrow = new Label
            {
                Text = Icons.Forward, Font = Icons.Font(10f), AutoSize = true, ForeColor = Muted, BackColor = Theme.Card,
                Top = D(15),
            };
            var to = new KeyCap { Text = rule.To, Top = D(8), Dimmed = !rule.Enabled };
            var remove = new Label
            {
                Text = Icons.Delete, Font = Icons.Font(11f), AutoSize = true, ForeColor = Muted, BackColor = Theme.Card,
                Cursor = Cursors.Hand, Padding = new Padding(D(4)),
            };

            Controls.AddRange(new Control[] { check, from, arrow, to, remove });
            // Center the checkbox and keys under their column headings, and the arrow between the keys.
            var checkSize = check.PreferredSize;
            check.Location = new Point(centers.On - checkSize.Width / 2, (Height - checkSize.Height) / 2);
            from.Left = centers.From - from.Width / 2;
            to.Left = centers.To - to.Width / 2;
            arrow.Left = (from.Right + to.Left - arrow.PreferredSize.Width) / 2;
            new ToolTip().SetToolTip(remove, "Remove rule");

            check.CheckedChanged += (_, _) =>
            {
                rule.Enabled = check.Checked;
                from.Dimmed = to.Dimmed = !check.Checked;
                changed();
            };
            remove.MouseEnter += (_, _) => remove.ForeColor = Danger;
            remove.MouseLeave += (_, _) => remove.ForeColor = Muted;
            remove.Click += (_, _) => delete(rule);
            Resize += (_, _) =>
                remove.Location = new Point(Width - D(12) - remove.Width, (Height - remove.Height) / 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Border);
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }
    }
}
