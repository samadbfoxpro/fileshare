using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FileShareSetup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SetupWizardForm());
    }
}

internal sealed class SetupWizardForm : Form
{
    private const string AppName = "FileShare";
    private const string Version = "1.3";
    private static readonly Color Background = Color.FromArgb(18, 22, 28);
    private static readonly Color PanelBackground = Color.FromArgb(25, 31, 39);
    private static readonly Color FooterBackground = Color.FromArgb(14, 18, 24);
    private static readonly Color Accent = Color.FromArgb(30, 144, 255);
    private static readonly Color AccentHover = Color.FromArgb(58, 163, 255);
    private static readonly Color Foreground = Color.FromArgb(236, 241, 247);
    private static readonly Color MutedForeground = Color.FromArgb(169, 181, 195);
    private static readonly Color ControlBackground = Color.FromArgb(32, 39, 49);
    private static readonly Color Border = Color.FromArgb(59, 72, 88);

    private readonly Panel contentPanel = new() { BackColor = Background };
    private readonly Panel buttonsPanel = new() { BackColor = FooterBackground };
    private readonly Button backButton = new() { Text = "< Back", Width = 90, Height = 32, Enabled = false };
    private readonly Button nextButton = new() { Text = "Next >", Width = 90, Height = 32 };
    private readonly Button cancelButton = new() { Text = "Cancel", Width = 90, Height = 32 };
    private readonly TextBox installPathTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
    private readonly ProgressBar progressBar = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
    private readonly CheckBox desktopShortcutCheckBox = new() { Text = "Create a desktop shortcut", Checked = true, AutoSize = true };
    private readonly CheckBox launchCheckBox = new() { Text = "Launch FileShare after setup exits", Checked = true, AutoSize = true };

    private int pageIndex;
    private bool installed;

    public SetupWizardForm()
    {
        Text = "FileShare Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(700, 500);
        BackColor = Background;
        ForeColor = Foreground;
        Font = new Font("Segoe UI", 9F);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        installPathTextBox.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            AppName);
        StyleTextBox(installPathTextBox);
        StyleCheckBox(desktopShortcutCheckBox);
        StyleCheckBox(launchCheckBox);
        StyleButton(backButton, false);
        StyleButton(nextButton, true);
        StyleButton(cancelButton, false);

        buttonsPanel.SetBounds(0, ClientSize.Height - 82, ClientSize.Width, 82);
        buttonsPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

        contentPanel.SetBounds(0, 0, ClientSize.Width, buttonsPanel.Top);
        contentPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

        var separator = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Border };
        buttonsPanel.Controls.Add(separator);

        cancelButton.Left = buttonsPanel.Width - 124;
        nextButton.Left = cancelButton.Left - 104;
        backButton.Left = nextButton.Left - 104;
        backButton.Top = nextButton.Top = cancelButton.Top = 24;
        backButton.Anchor = nextButton.Anchor = cancelButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        buttonsPanel.Controls.AddRange(new Control[] { backButton, nextButton, cancelButton });

        Controls.Add(contentPanel);
        Controls.Add(buttonsPanel);
        buttonsPanel.BringToFront();

        backButton.Click += (_, _) => ShowPage(pageIndex - 1);
        nextButton.Click += async (_, _) => await NextAsync();
        cancelButton.Click += (_, _) => Close();

        ShowPage(0);
    }

    private async Task NextAsync()
    {
        if (pageIndex < 2)
        {
            ShowPage(pageIndex + 1);
            return;
        }

        if (pageIndex == 2)
        {
            await InstallAsync();
            return;
        }

        if (launchCheckBox.Checked)
        {
            var exePath = Path.Combine(installPathTextBox.Text.Trim(), "FileShare.exe");
            if (File.Exists(exePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                });
            }
        }

        Close();
    }

    private void ShowPage(int index)
    {
        pageIndex = index;
        contentPanel.Controls.Clear();
        backButton.Enabled = index > 0 && !installed;
        cancelButton.Enabled = !installed || index == 3;
        nextButton.Text = index switch
        {
            2 => "Install",
            3 => "Finish",
            _ => "Next >"
        };

        switch (index)
        {
            case 0:
                BuildWelcomePage();
                break;
            case 1:
                BuildDestinationPage();
                break;
            case 2:
                BuildReadyPage();
                break;
            case 3:
                BuildFinishPage();
                break;
        }
    }

    private void BuildWelcomePage()
    {
        var leftPanel = new Panel { Dock = DockStyle.Left, Width = 190, BackColor = PanelBackground };
        var accentPanel = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = Accent };
        var titleLabel = new Label
        {
            Text = "FileShare",
            ForeColor = Foreground,
            Font = new Font(Font.FontFamily, 24, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        leftPanel.Controls.Add(accentPanel);
        leftPanel.Controls.Add(titleLabel);

        var heading = NewHeading("Welcome to the FileShare Setup Wizard");
        heading.Top = 42;
        heading.Left = 225;

        var body = NewBody("This wizard will install FileShare on your computer.\r\n\r\nClick Next to continue, or Cancel to exit Setup.");
        body.Top = 108;
        body.Left = 225;
        body.Width = 390;
        body.Height = 160;

        contentPanel.Controls.AddRange(new Control[] { leftPanel, heading, body });
    }

    private void BuildDestinationPage()
    {
        var heading = NewHeading("Select Installation Folder");
        heading.Top = 30;
        heading.Left = 38;

        var body = NewBody("Setup will install FileShare in the following folder. To install in a different folder, click Browse.");
        body.Top = 84;
        body.Left = 38;
        body.Width = 590;
        body.Height = 48;

        installPathTextBox.Left = 38;
        installPathTextBox.Top = 150;
        installPathTextBox.Width = 470;
        installPathTextBox.Height = 30;

        var browseButton = new Button { Text = "Browse...", Left = 524, Top = 148, Width = 104, Height = 34 };
        StyleButton(browseButton, false);
        browseButton.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the folder where FileShare will be installed",
                SelectedPath = installPathTextBox.Text
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                installPathTextBox.Text = dialog.SelectedPath;
            }
        };

        desktopShortcutCheckBox.Left = 38;
        desktopShortcutCheckBox.Top = 214;

        contentPanel.Controls.AddRange(new Control[] { heading, body, installPathTextBox, browseButton, desktopShortcutCheckBox });
    }

    private void BuildReadyPage()
    {
        var heading = NewHeading("Ready to Install");
        heading.Top = 30;
        heading.Left = 38;

        var body = NewBody("Setup is ready to begin installing FileShare on your computer.");
        body.Top = 84;
        body.Left = 38;
        body.Width = 590;
        body.Height = 42;

        var pathLabel = NewBody("Destination folder:");
        pathLabel.Top = 145;
        pathLabel.Left = 38;
        pathLabel.Width = 590;
        pathLabel.Height = 24;

        var selectedPath = NewBody(installPathTextBox.Text.Trim());
        selectedPath.Top = 174;
        selectedPath.Left = 58;
        selectedPath.Width = 570;
        selectedPath.Height = 52;

        progressBar.Left = 38;
        progressBar.Top = 260;
        progressBar.Width = 590;
        progressBar.Height = 20;
        progressBar.Minimum = 0;
        progressBar.Maximum = 100;
        progressBar.Value = 0;
        progressBar.Visible = false;

        contentPanel.Controls.AddRange(new Control[] { heading, body, pathLabel, selectedPath, progressBar });
    }

    private void BuildFinishPage()
    {
        var heading = NewHeading("Completing the FileShare Setup Wizard");
        heading.Top = 42;
        heading.Left = 38;

        var body = NewBody("FileShare has been installed on your computer.\r\n\r\nClick Finish to exit Setup.");
        body.Top = 104;
        body.Left = 38;
        body.Width = 590;
        body.Height = 92;

        launchCheckBox.Left = 38;
        launchCheckBox.Top = 220;

        contentPanel.Controls.AddRange(new Control[] { heading, body, launchCheckBox });
    }

    private async Task InstallAsync()
    {
        var installDir = installPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(installDir))
        {
            MessageBox.Show(this, "Please select an installation folder.", "FileShare Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ShowPage(1);
            return;
        }

        backButton.Enabled = false;
        nextButton.Enabled = false;
        cancelButton.Enabled = false;
        progressBar.Visible = true;
        progressBar.Value = 10;

        try
        {
            await Task.Run(() => Install(installDir, desktopShortcutCheckBox.Checked));
            progressBar.Value = 100;
            installed = true;
            nextButton.Enabled = true;
            ShowPage(3);
        }
        catch (Exception ex)
        {
            nextButton.Enabled = true;
            cancelButton.Enabled = true;
            MessageBox.Show(this, ex.Message, "FileShare Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void Install(string installDir, bool createDesktopShortcut)
    {
        var startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "Windows",
            "Start Menu",
            "Programs",
            AppName);
        var desktopShortcut = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "FileShare.lnk");
        var startShortcut = Path.Combine(startMenuDir, "FileShare.lnk");
        var uninstallShortcut = Path.Combine(startMenuDir, "Uninstall FileShare.lnk");
        var exePath = Path.Combine(installDir, "FileShare.exe");
        var uninstallPath = Path.Combine(installDir, "uninstall.ps1");

        Directory.CreateDirectory(installDir);
        Directory.CreateDirectory(startMenuDir);

        ExtractResource("Payload.FileShare.exe", exePath);
        ExtractResource("Payload.WebView2Loader.dll", Path.Combine(installDir, "WebView2Loader.dll"));
        ExtractResource("Payload.uninstall.ps1", uninstallPath);

        if (createDesktopShortcut)
        {
            CreateShortcut(desktopShortcut, exePath, "", installDir, exePath);
        }
        else if (File.Exists(desktopShortcut))
        {
            File.Delete(desktopShortcut);
        }

        CreateShortcut(startShortcut, exePath, "", installDir, exePath);
        CreateShortcut(
            uninstallShortcut,
            "powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -File \"{uninstallPath}\"",
            installDir,
            exePath);

        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\FileShare");
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", Version);
        key.SetValue("Publisher", AppName);
        key.SetValue("InstallLocation", installDir);
        key.SetValue("DisplayIcon", exePath);
        key.SetValue("UninstallString", $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{uninstallPath}\"");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static Label NewHeading(string text) => new()
    {
        Text = text,
        ForeColor = Foreground,
        BackColor = Background,
        Font = new Font("Segoe UI", 15F, FontStyle.Bold),
        AutoSize = false,
        Width = 590,
        Height = 48
    };

    private static Label NewBody(string text) => new()
    {
        Text = text,
        ForeColor = MutedForeground,
        BackColor = Background,
        Font = new Font("Segoe UI", 9F),
        AutoSize = false
    };

    private static void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : Color.FromArgb(40, 49, 61);
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(18, 105, 196) : Color.FromArgb(29, 36, 45);
        button.BackColor = primary ? Accent : ControlBackground;
        button.ForeColor = Color.White;
        button.UseVisualStyleBackColor = false;
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = ControlBackground;
        textBox.ForeColor = Foreground;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.BackColor = Background;
        checkBox.ForeColor = Foreground;
        checkBox.FlatStyle = FlatStyle.Flat;
    }

    private static void ExtractResource(string resourceName, string destination)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var input = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Setup resource was not found: {resourceName}");
        using var output = File.Create(destination);
        input.CopyTo(output);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string arguments, string workingDirectory, string iconPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is not available.");
        var shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Could not create Windows shortcut helper.");

        try
        {
            dynamic shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath })!;
            shortcut.TargetPath = targetPath;
            shortcut.Arguments = arguments;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.IconLocation = iconPath;
            shortcut.Save();
            Marshal.FinalReleaseComObject(shortcut);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
