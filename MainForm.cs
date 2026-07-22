using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace FileShare
{
    public class MainForm : Form
    {
        private readonly HttpServer _httpServer;
        private readonly FileService _fileService;

        private readonly Label _lblStatus;
        private readonly ComboBox _networkBox;
        private readonly PictureBox _pbQr;
        private readonly Label _lblFolderPath;

        public MainForm(HttpServer httpServer, FileService fileService)
        {
            _httpServer = httpServer;
            _fileService = fileService;

            Text = "FileShare - کنترل پنل شبکه";
            Width = 460; Height = 640;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            var cBg = Color.FromArgb(12, 14, 22);
            var cCard = Color.FromArgb(20, 24, 36);
            var cField = Color.FromArgb(14, 17, 27);
            var cLine = Color.FromArgb(35, 42, 62);
            var cText = Color.FromArgb(230, 235, 248);
            var cDim = Color.FromArgb(100, 110, 140);
            var cSub = Color.FromArgb(140, 152, 185);
            var cBlue = Color.FromArgb(110, 180, 255);
            var cBlueBg = Color.FromArgb(24, 55, 110);
            var cGreen = Color.FromArgb(60, 210, 110);
            var cGrayBg = Color.FromArgb(26, 30, 46);

            BackColor = cBg;
            Icon = LoadIconResource();

            Button Btn(string txt, Color bg, Color fg, int w, int h = 34)
            {
                var b = new Button
                {
                    Text = txt, FlatStyle = FlatStyle.Flat,
                    BackColor = bg, ForeColor = fg,
                    Width = w, Height = h, AutoSize = false,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand, Margin = new Padding(0)
                };
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bg, .25f);
                return b;
            }

            // ── هدر بالا ──
            var headerPanel = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = cCard, Padding = new Padding(16, 10, 16, 10) };
            headerPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(cLine);
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            var lblTitle = new Label { Text = "⚡ FileShare v1.5 Server", ForeColor = cText, Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true, Location = new Point(14, 10) };
            _lblStatus = new Label { Text = "● سرور فعال است", ForeColor = cGreen, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), AutoSize = true, Location = new Point(16, 32) };

            var btnOpen = Btn("🌐 باز کردن وب", cBlueBg, cBlue, 115, 34);
            btnOpen.Location = new Point(315, 11);

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(_lblStatus);
            headerPanel.Controls.Add(btnOpen);

            // ── محتوای اصلی پنل ──
            var mainContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(16),
                AutoScroll = true
            };

            // 1. کارت شبکه و QR Code
            var cardNet = new Panel { Width = 412, Height = 250, BackColor = cCard, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(14) };
            cardNet.Paint += (_, e) => { using var p = new Pen(cLine); e.Graphics.DrawRectangle(p, 0, 0, cardNet.Width - 1, cardNet.Height - 1); };

            var lblNetTitle = new Label { Text = "🌐 آدرس‌های اتصال شبکه", ForeColor = cText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), AutoSize = true, Location = new Point(14, 12) };

            _networkBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = cField, ForeColor = cText,
                Font = new Font("Consolas", 9.5F, FontStyle.Bold),
                Width = 260, Height = 30, FlatStyle = FlatStyle.Flat,
                Location = new Point(14, 40)
            };

            var btnCopy = Btn("📋 کپی", cGrayBg, cText, 60, 30);
            btnCopy.Location = new Point(280, 40);

            var btnRefresh = Btn("🔄", cGrayBg, cText, 38, 30);
            btnRefresh.Location = new Point(346, 40);

            _pbQr = new PictureBox { Size = new Size(140, 140), Location = new Point(136, 88), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
            var lblQrHint = new Label { Text = "اسکن QR جهت ورود سریع با گوشی", ForeColor = cDim, Font = new Font("Segoe UI", 8F), AutoSize = true, Location = new Point(118, 230) };

            cardNet.Controls.Add(lblNetTitle);
            cardNet.Controls.Add(_networkBox);
            cardNet.Controls.Add(btnCopy);
            cardNet.Controls.Add(btnRefresh);
            cardNet.Controls.Add(_pbQr);
            cardNet.Controls.Add(lblQrHint);

            // 2. کارت پوشه اشتراکی
            var cardFolder = new Panel { Width = 412, Height = 145, BackColor = cCard, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(14) };
            cardFolder.Paint += (_, e) => { using var p = new Pen(cLine); e.Graphics.DrawRectangle(p, 0, 0, cardFolder.Width - 1, cardFolder.Height - 1); };

            var lblFolderTitle = new Label { Text = "📂 پوشه اشتراکی سیستم", ForeColor = cText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), AutoSize = true, Location = new Point(14, 12) };
            _lblFolderPath = new Label
            {
                Text = string.IsNullOrWhiteSpace(_fileService.SharedFolder) ? "هیچ پوشه‌ای انتخاب نشده است" : _fileService.SharedFolder,
                ForeColor = cSub, Font = new Font("Segoe UI", 8.5F),
                Size = new Size(384, 38), Location = new Point(14, 38),
                AutoEllipsis = true
            };

            var btnSharedFolder = Btn("⚙ انتخاب / تغییر پوشه اشتراکی...", cGrayBg, cBlue, 220, 34);
            btnSharedFolder.Location = new Point(14, 94);

            var btnOpenSharedFolder = Btn("📂 باز کردن پوشه", cGrayBg, cText, 130, 34);
            btnOpenSharedFolder.Location = new Point(242, 94);

            cardFolder.Controls.Add(lblFolderTitle);
            cardFolder.Controls.Add(_lblFolderPath);
            cardFolder.Controls.Add(btnSharedFolder);
            cardFolder.Controls.Add(btnOpenSharedFolder);

            // 3. کارت راهنما و توضیحات
            var cardInfo = new Panel { Width = 412, Height = 80, BackColor = cCard, Margin = new Padding(0, 0, 0, 0), Padding = new Padding(14) };
            cardInfo.Paint += (_, e) => { using var p = new Pen(cLine); e.Graphics.DrawRectangle(p, 0, 0, cardInfo.Width - 1, cardInfo.Height - 1); };

            var lblInfo = new Label
            {
                Text = "💡 نکته: با انتخاب پوشه اشتراکی، تمام فایل‌ها و زیرپوشه‌های آن به همراه قابلیت دانلود در مرورگر قرار می‌گیرند.",
                ForeColor = cDim, Font = new Font("Segoe UI", 8.5F),
                Size = new Size(384, 52), Location = new Point(14, 14)
            };
            cardInfo.Controls.Add(lblInfo);

            mainContainer.Controls.Add(cardNet);
            mainContainer.Controls.Add(cardFolder);
            mainContainer.Controls.Add(cardInfo);

            Controls.Add(mainContainer);
            Controls.Add(headerPanel);

            // Event bindings
            btnOpen.Click += (_, _) => OpenBrowser(_networkBox.SelectedItem?.ToString() ?? _httpServer.LocalUrl);
            btnCopy.Click += (_, _) =>
            {
                try
                {
                    var value = _networkBox.SelectedItem?.ToString() ?? _httpServer.LocalUrl;
                    SetClipboardText(value);
                    _lblStatus.Text = "● آدرس کپی شد";
                }
                catch { }
            };

            btnRefresh.Click += (_, _) => { RefreshNetwork(); _lblStatus.Text = "● شبکه به‌روزرسانی شد"; };
            _networkBox.SelectedIndexChanged += (_, _) => RefreshNetworkQr();

            btnSharedFolder.Click += (_, _) =>
            {
                try
                {
                    var initialPath = Directory.Exists(_fileService.SharedFolder) ? _fileService.SharedFolder : _fileService.UploadDir;
                    var selectedPath = ShowFolderPicker(initialPath);
                    if (!string.IsNullOrWhiteSpace(selectedPath))
                    {
                        _fileService.UpdateSharedFolder(selectedPath);
                        RefreshSharedFolderUi();
                        _lblStatus.Text = "● پوشه اشتراکی به روز شد";
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(this, $"خطا در تغییر پوشه:\r\n{error.Message}", "FileShare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnOpenSharedFolder.Click += (_, _) =>
            {
                try
                {
                    var folderToOpen = Directory.Exists(_fileService.SharedFolder) ? _fileService.SharedFolder : _fileService.UploadDir;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folderToOpen,
                        UseShellExecute = true
                    });
                }
                catch { }
            };

            FormClosing += (_, _) => _httpServer.Stop();

            RefreshNetwork();
            RefreshSharedFolderUi();
        }

        private void RefreshNetwork()
        {
            _networkBox.Items.Clear();
            _networkBox.Items.Add(_httpServer.LocalUrl);
            foreach (var ip in HttpServer.GetLocalIps())
            {
                _networkBox.Items.Add($"http://{ip}:8887");
            }
            _networkBox.SelectedIndex = 0;
        }

        private void RefreshNetworkQr()
        {
            var url = _networkBox.SelectedItem?.ToString() ?? _httpServer.LocalUrl;
            var qrBytes = HttpServer.CreateQrPngBytes(url, 5);
            using var ms = new MemoryStream(qrBytes);
            _pbQr.Image = Image.FromStream(ms);
        }

        private void RefreshSharedFolderUi()
        {
            _lblFolderPath.Text = string.IsNullOrWhiteSpace(_fileService.SharedFolder) ? "هیچ پوشه‌ای انتخاب نشده است" : _fileService.SharedFolder;
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private static void SetClipboardText(string text)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try { Clipboard.SetText(text); }
                catch (Exception error) { failure = error; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure is not null) throw failure;
        }

        private static string? ShowFolderPicker(string initialPath)
        {
            string? selectedPath = null;
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var dialog = new FolderBrowserDialog
                    {
                        Description = "پوشه اشتراکی را انتخاب کنید",
                        UseDescriptionForTitle = true,
                        SelectedPath = initialPath
                    };

                    if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                    {
                        selectedPath = dialog.SelectedPath;
                    }
                }
                catch (Exception error) { failure = error; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure is not null) throw failure;
            return selectedPath;
        }

        private static Icon? LoadIconResource()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("wwwroot.icon.ico");
            return stream is null ? null : new Icon(stream);
        }
    }
}
