using System.Diagnostics;
using System.Reflection;
using static KeySwapper.Theme;

namespace KeySwapper;

sealed class AboutDialog : Form
{
    const string Website = "https://beqaindustries.com";

    public AboutDialog()
    {
        Text = "About Key Swapper";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font(UiFont, 9.75f);
        BackColor = Theme.Card;
        ForeColor = Theme.Text;

        int pad = D(24), logoSize = D(48), textX = pad + logoSize + D(16);
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        var logo = new PictureBox
        {
            Image = IconFactory.CreateBitmap(logoSize, true), Size = new Size(logoSize, logoSize),
            Location = new Point(pad, D(24)),
        };
        var name = new Label
        {
            Text = "Key Swapper", AutoSize = true, Location = new Point(textX, D(24)),
            Font = new Font("Segoe UI Semibold", 13f),
        };
        var versionLabel = new Label
        {
            Text = "Version " + version, AutoSize = true, ForeColor = SubText, Location = new Point(textX, D(54)),
        };
        var by = new Label
        {
            Text = "Developed by Beqa Industries", AutoSize = true, Location = new Point(pad, D(96)),
        };
        var link = new LinkLabel
        {
            Text = "beqaindustries.com", AutoSize = true, Location = new Point(pad, D(120)),
            LinkColor = Accent, ActiveLinkColor = Accent, VisitedLinkColor = Accent,
            LinkBehavior = LinkBehavior.HoverUnderline,
        };
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(Website) { UseShellExecute = true });

        var footer = new Panel { Dock = DockStyle.Bottom, Height = D(64), BackColor = Back };
        var ok = new Button { Text = "OK", Size = new Size(D(100), D(32)), DialogResult = DialogResult.OK };
        footer.Controls.Add(ok);

        Controls.AddRange(new Control[] { logo, name, versionLabel, by, link, footer });
        AcceptButton = ok;
        CancelButton = ok;

        ClientSize = new Size(D(340), link.Bottom + D(24) + footer.Height);
        ok.Location = new Point(ClientSize.Width - pad - ok.Width, D(16));
        Shown += (_, _) => ok.Focus();
    }
}
