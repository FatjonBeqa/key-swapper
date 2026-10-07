namespace KeySwapper;

/// <summary>Owns the app's lifetime: tray icon, keyboard hook and the (hideable) main window.</summary>
sealed class TrayContext : ApplicationContext
{
    readonly Settings settings;
    readonly KeyboardHook hook;
    readonly NotifyIcon tray;
    readonly ToolStripMenuItem enabledItem;
    readonly MainForm form;
    readonly Icon onIcon = IconFactory.Create(true);
    readonly Icon offIcon = IconFactory.Create(false);
    bool exiting, hintShown;

    public TrayContext(bool startHidden)
    {
        settings = Settings.Load();

        // On first run, start with Windows by default (the checkbox in the window turns it off),
        // and save right away so this only happens once.
        if (Settings.IsFirstRun)
        {
            try { Settings.StartWithWindows = true; } catch { }
            settings.Save();
        }
        // Keep the Run entry pointing at wherever the exe lives now.
        else if (Settings.StartWithWindows)
            try { Settings.StartWithWindows = true; } catch { }

        hook = new KeyboardHook(settings);

        form = new MainForm(settings, hook, onIcon);
        _ = form.Handle; // needed for BeginInvoke before the window is ever shown
        form.EnabledToggled += SetEnabled;
        // Minimize goes to the tray instead of the taskbar.
        form.Resize += (_, _) =>
        {
            if (form.WindowState != FormWindowState.Minimized) return;
            form.Hide();
            form.WindowState = FormWindowState.Normal;
            if (!hintShown)
            {
                hintShown = true;
                tray!.ShowBalloonTip(3000, "Key Swapper is still running",
                    "It keeps swapping keys from the tray. Click the icon to open it again.", ToolTipIcon.None);
            }
        };

        // X quits the app, after asking. Any other close (shutdown, Task Manager) quits right away.
        form.FormClosed += (_, _) =>
        {
            if (!exiting) ExitThread();
        };
        form.FormClosing += (_, e) =>
        {
            if (exiting || e.CloseReason != CloseReason.UserClosing) return;
            e.Cancel = true;
            bool quit = MessageDialog.Confirm(form, "Quit Key Swapper?",
                "Your keys will stop being swapped until you open the app again. " +
                "To keep it running in the background, minimize the window instead.",
                action: "Quit", safe: "Keep running");
            if (quit)
                form.BeginInvoke(ExitThread);
        };

        // The hook fires inside the keyboard callback, so do the real work afterwards.
        hook.ToggleRequested += () => form.BeginInvoke(() =>
        {
            SetEnabled(!settings.Enabled);
            if (!form.Visible)
                tray!.ShowBalloonTip(1500, settings.Enabled ? "Swapping on" : "Swapping off", " ", ToolTipIcon.None);
        });

        enabledItem = new ToolStripMenuItem("Enabled", null, (_, _) => SetEnabled(!settings.Enabled));
        var menu = new ContextMenuStrip();
        menu.Items.Add(enabledItem);
        menu.Items.Add("Open window", null, (_, _) => ShowWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        tray = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
        tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ShowWindow();
        };

        ApplyState();
        if (!startHidden || Settings.LoadProblem != null)
            ShowWindow();
        if (Settings.LoadProblem != null)
            form.BeginInvoke(() => MessageDialog.Notice(form, "Couldn't read your saved rules", Settings.LoadProblem));
    }

    void SetEnabled(bool on)
    {
        settings.Enabled = on;
        settings.Save();
        ApplyState();
    }

    void ApplyState()
    {
        tray.Icon = settings.Enabled ? onIcon : offIcon;
        tray.Text = settings.Enabled ? "Key Swapper (on)" : "Key Swapper (off)";
        enabledItem.Checked = settings.Enabled;
        form.RefreshState();
    }

    void ShowWindow()
    {
        form.Show();
        if (form.WindowState == FormWindowState.Minimized)
            form.WindowState = FormWindowState.Normal;
        form.Activate();
    }

    protected override void ExitThreadCore()
    {
        exiting = true;
        hook.Dispose();
        tray.Visible = false;
        tray.Dispose();
        if (!form.IsDisposed)
            form.Close();
        base.ExitThreadCore();
    }
}
