using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

const int Port = 8887;
var appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "FileShare");
var uploadDir = Path.Combine(appDir, "uploads");
var sharedFolderConfig = Path.Combine(appDir, "shared-folder.txt");
var sharedFolder = File.Exists(sharedFolderConfig) ? File.ReadAllText(sharedFolderConfig, Encoding.UTF8).Trim() : string.Empty;
var messageLog = Path.Combine(appDir, "messages.jsonl");
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};

Directory.CreateDirectory(uploadDir);
if (!string.IsNullOrWhiteSpace(sharedFolder))
{
    Directory.CreateDirectory(sharedFolder);
}
File.AppendAllText(messageLog, string.Empty, Encoding.UTF8);

var listener = StartHttpListener(out var networkSharingEnabled);

var localUrl = $"http://localhost:{Port}";

// Start accept loop on a background task so the UI thread can run the window.
_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            var context = await listener.GetContextAsync();
            _ = Task.Run(() => HandleRequestAsync(context));
        }
        catch (HttpListenerException) // listener stopped
        {
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
});

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

// ═══════════════════════════════════════════════════════════
//  رنگ‌پالت
// ═══════════════════════════════════════════════════════════
var cBg      = Color.FromArgb(10,  12,  18);
var cCard    = Color.FromArgb(16,  18,  28);
var cField   = Color.FromArgb(11,  13,  22);
var cLine    = Color.FromArgb(28,  32,  48);
var cText    = Color.FromArgb(220, 225, 240);
var cDim     = Color.FromArgb(90,  98, 125);
var cSub     = Color.FromArgb(130, 140, 168);
var cBlue    = Color.FromArgb(100, 170, 255);
var cBlueBg  = Color.FromArgb(18,  45,  90);
var cGreen   = Color.FromArgb(60,  210, 110);
var cGreenBg = Color.FromArgb(10,  36,  22);
var cRed     = Color.FromArgb(255, 100, 100);
var cRedBg   = Color.FromArgb(40,  12,  18);
var cGrayBg  = Color.FromArgb(20,  23,  36);

// ═══════════════════════════════════════════════════════════
//  کمک‌کننده‌ها
// ═══════════════════════════════════════════════════════════

Button Btn(string txt, Color bg, Color fg, int w, int h = 32)
{
    var b = new Button
    {
        Text = txt, FlatStyle = FlatStyle.Flat,
        BackColor = bg, ForeColor = fg,
        Width = w, Height = h, AutoSize = false,
        Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
        Cursor = Cursors.Hand, Margin = new Padding(0)
    };
    b.FlatAppearance.BorderSize = 0;
    b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bg, .2f);
    return b;
}

Label Lbl(string txt, Font f, Color fg, int mb = 0) => new Label
{
    Text = txt, AutoSize = true, ForeColor = fg, Font = f,
    Margin = new Padding(0, 0, 0, mb), BackColor = Color.Transparent
};

// فیلد یک‌خطی (URL / مسیر)
Panel Field(string txt, Font f, Color fg)
{
    var p = new Panel
    {
        Dock = DockStyle.Top, Height = 36,
        BackColor = cField, Margin = new Padding(0, 0, 0, 10),
        Padding = new Padding(12, 0, 12, 0)
    };
    p.Paint += (_, e) =>
    {
        using var pen = new Pen(cLine);
        e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
    };
    var lbl = new Label
    {
        Text = txt, AutoSize = false, Dock = DockStyle.Fill,
        ForeColor = fg, BackColor = Color.Transparent,
        Font = f, TextAlign = ContentAlignment.MiddleLeft
    };
    p.Controls.Add(lbl);
    return p;
}

// بخش‌نما (خط افقی + برچسب)
Panel Divider(string label)
{
    var p = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = Color.Transparent, Margin = new Padding(0, 4, 0, 8) };
    p.Paint += (_, e) =>
    {
        using var pen = new Pen(cLine);
        int y = p.Height / 2;
        e.Graphics.DrawLine(pen, 0, y, p.Width, y);
        var sz = e.Graphics.MeasureString(label, p.Font);
        using var bg = new SolidBrush(cCard);
        e.Graphics.FillRectangle(bg, 0, y - (int)(sz.Height / 2) - 1, (int)sz.Width + 4, (int)sz.Height + 2);
        using var fg = new SolidBrush(cDim);
        e.Graphics.DrawString(label, p.Font, fg, 0, y - sz.Height / 2);
    };
    return p;
}

// ═══════════════════════════════════════════════════════════
//  فرم
// ═══════════════════════════════════════════════════════════
var form = new Form
{
    Text = "FileShare v1.2",
    Size = new Size(580, 545),
    MinimumSize = new Size(560, 520),
    FormBorderStyle = FormBorderStyle.Sizable,
    MaximizeBox = true,
    AutoScroll = true,
    StartPosition = FormStartPosition.CenterScreen,
    BackColor = cBg,
    ForeColor = cText,
    Font = new Font("Segoe UI", 9F),
    Icon = LoadIconResource()
};
UseImmersiveDarkTitleBar(form);

// ── نوار وضعیت پایین ─────────────────────────────────────
var statusBar = new Label
{
    Dock = DockStyle.Bottom, Height = 24,
    TextAlign = ContentAlignment.MiddleLeft,
    Text = $"  سرور روی پورت {Port} فعال است",
    BackColor = Color.FromArgb(8, 10, 16),
    ForeColor = cDim,
    Font = new Font("Segoe UI", 8F),
    Padding = new Padding(4, 0, 0, 0)
};
form.Controls.Add(statusBar);

// ── کانتینر اصلی ─────────────────────────────────────────
var canvas = new Panel
{
    Dock = DockStyle.Fill,
    BackColor = cBg,
    Padding = new Padding(20, 18, 20, 28)
};
form.Controls.Add(canvas);

// ── هدر ──────────────────────────────────────────────────
var lblTitle = new Label
{
    Text = "FileShare v1.2",
    Location = new Point(0, 0),
    AutoSize = true,
    ForeColor = cText,
    BackColor = Color.Transparent,
    Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold)
};
var lblSub = new Label
{
    Text = "اشتراک فایل و پیام در شبکه محلی",
    AutoSize = true,
    ForeColor = cDim,
    BackColor = Color.Transparent,
    Font = new Font("Segoe UI", 9F)
};
var badgeLive = new Label
{
    Text = "  ● LIVE  ",
    AutoSize = true,
    BackColor = cGreenBg,
    ForeColor = cGreen,
    Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold),
    Padding = new Padding(8, 5, 8, 5)
};
canvas.Controls.Add(lblTitle);
canvas.Controls.Add(lblSub);
canvas.Controls.Add(badgeLive);

// ── گرید دو‌ستونه ─────────────────────────────────────────
// از TableLayoutPanel فقط یک‌بار برای تقسیم دو ستون استفاده می‌کنیم
var grid = new TableLayoutPanel
{
    Dock = DockStyle.Top,
    ColumnCount = 2, RowCount = 1,
    BackColor = Color.Transparent,
    Margin = new Padding(0),
    Padding = new Padding(0),
    Height = 330
};
grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

// ── کارت چپ (اتصال) ──────────────────────────────────────
var leftCard = new Panel
{
    Dock = DockStyle.Fill,
    BackColor = cCard,
    Padding = new Padding(16, 14, 16, 14),
    Margin = new Padding(0, 0, 8, 0)
};
leftCard.Paint += (_, e) =>
{
    using var pen = new Pen(cLine);
    e.Graphics.DrawRectangle(pen, 0, 0, leftCard.Width - 1, leftCard.Height - 1);
};

var leftInner = new Panel { Dock = DockStyle.Fill, BackColor = cCard };

// عنوان کارت
var lcTitle = Lbl("اتصال", new Font("Segoe UI Semibold", 8F, FontStyle.Bold), cDim, 10);
lcTitle.Dock = DockStyle.Top;

// URL محلی
var lcLocalHead = Lbl("آدرس محلی", new Font("Segoe UI", 7.5F), cDim, 4);
lcLocalHead.Dock = DockStyle.Top;
var lcLocalChip = Field(localUrl, new Font("Consolas", 9F), cBlue);

// شبکه
var lcNetHead = Lbl("شبکه", new Font("Segoe UI", 7.5F), cDim, 4);
lcNetHead.Dock = DockStyle.Top;

var networkBox = new ListBox
{
    Dock = DockStyle.Fill,
    BackColor = cField,
    ForeColor = cBlue,
    BorderStyle = BorderStyle.None,
    Font = new Font("Consolas", 9F),
    IntegralHeight = false,
    Margin = new Padding(0, 0, 0, 10)
};

// دکمه‌ها - کارت چپ
var btnOpen    = Btn("باز کردن", cBlueBg, cBlue, 86);
var btnCopy    = Btn("کپی", cGrayBg, cSub, 62);
var btnRefresh = Btn("↺  تازه‌سازی", cGrayBg, cSub, 88);

var lcBtnRow = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 32, BackColor = cCard, ColumnCount = 3, RowCount = 1, Padding = new Padding(0), Margin = new Padding(0) };
lcBtnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
lcBtnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
lcBtnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
btnOpen.Location    = new Point(0, 0);
btnCopy.Location    = new Point(90, 0);
btnRefresh.Location = new Point(156, 0);
foreach (var button in new[] { btnOpen, btnCopy, btnRefresh })
{
    button.Dock = DockStyle.Fill;
    button.Width = 0;
    button.Height = 28;
    button.Margin = new Padding(0, 0, 4, 0);
}
btnRefresh.Margin = new Padding(0);
btnOpen.Text = "Open";
btnCopy.Text = "Copy";
btnRefresh.Text = "Refresh";
lcBtnRow.Controls.Add(btnOpen, 0, 0);
lcBtnRow.Controls.Add(btnCopy, 1, 0);
lcBtnRow.Controls.Add(btnRefresh, 2, 0);

// چیدمان کارت چپ (از پایین به بالا برای Dock)
leftInner.Controls.Add(networkBox);     // Fill - وسط
leftInner.Controls.Add(lcLocalChip);    // Top
leftInner.Controls.Add(lcLocalHead);    // Top
leftInner.Controls.Add(lcTitle);        // Top
leftCard.Controls.Add(leftInner);
leftCard.Controls.Add(lcBtnRow);       // Bottom
// برچسب شبکه بعد از networkBox قرار داره — باید بعد از leftInner اضافه بشه
var lcNetHeadWrap = new Panel { Dock = DockStyle.Top, Height = 20, BackColor = cCard };
lcNetHead.Dock = DockStyle.Fill;
lcNetHeadWrap.Controls.Add(lcNetHead);
leftInner.Controls.Add(lcNetHeadWrap);

// ── کارت راست (وضعیت) ────────────────────────────────────
var rightCard = new Panel
{
    Dock = DockStyle.Fill,
    BackColor = cCard,
    Padding = new Padding(16, 14, 16, 14),
    Margin = new Padding(0)
};
rightCard.Paint += (_, e) =>
{
    using var pen = new Pen(cLine);
    e.Graphics.DrawRectangle(pen, 0, 0, rightCard.Width - 1, rightCard.Height - 1);
};

var rightInner = new Panel { Dock = DockStyle.Fill, BackColor = cCard };
var sharedCard = new Panel { Dock = DockStyle.Bottom, Height = 78, BackColor = cCard, Padding = new Padding(0, 10, 0, 0) };

var rcTitle = Lbl("وضعیت", new Font("Segoe UI Semibold", 8F, FontStyle.Bold), cDim, 10);
rcTitle.Dock = DockStyle.Top;

// پورت
var rcPortHead = Lbl("پورت", new Font("Segoe UI", 7.5F), cDim, 4);
rcPortHead.Dock = DockStyle.Top;
var rcPortVal  = Lbl(Port.ToString(), new Font("Consolas", 15F, FontStyle.Bold), cBlue, 10);
rcPortVal.Dock = DockStyle.Top;

// وضعیت سرور
var rcStatusHead = Lbl("سرور", new Font("Segoe UI", 7.5F), cDim, 4);
rcStatusHead.Dock = DockStyle.Top;
var rcStatusVal  = Lbl("فعال  ●", new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), cGreen, 10);
rcStatusVal.Dock = DockStyle.Top;

// مسیر
var rcPathHead = Lbl("ذخیره فایل‌ها", new Font("Segoe UI", 7.5F), cDim, 4);
rcPathHead.Dock = DockStyle.Top;
var shortPath  = uploadDir.Length > 35 ? "…" + uploadDir[^32..] : uploadDir;
var rcPathVal  = Lbl(shortPath, new Font("Consolas", 8F), cSub, 10);
rcPathVal.Dock = DockStyle.Top;
rcPathVal.AutoSize = false;
rcPathVal.Height = 16;
rcPathVal.AutoEllipsis = true;
var sharedPathText = ShortPath(sharedFolder, 35);
var rcSharedHead = Lbl("Ù¾ÙˆØ´Ù‡ Ø§Ø´ØªØ±Ø§Ú©ÛŒ", new Font("Segoe UI", 7.5F), cDim, 4);
rcSharedHead.Dock = DockStyle.Top;
var rcSharedVal = Lbl(string.IsNullOrWhiteSpace(sharedPathText) ? "ØªÙ†Ø¸ÛŒÙ… Ù†Ø´Ø¯Ù‡" : sharedPathText, new Font("Consolas", 8F), cSub, 8);
rcSharedVal.Dock = DockStyle.Top;
rcSharedVal.AutoSize = false;
rcSharedVal.Height = 16;
rcSharedVal.AutoEllipsis = true;
var btnSharedFolder = Btn("ØªÙ†Ø¸ÛŒÙ… Ù¾ÙˆØ´Ù‡", cGrayBg, cSub, 0, 28);
btnSharedFolder.Dock = DockStyle.Top;
rcSharedHead.Text = "پوشه اشتراکی";
rcSharedVal.Text = string.IsNullOrWhiteSpace(sharedPathText) ? "تنظیم نشده" : sharedPathText;
btnSharedFolder.Text = "تنظیم پوشه";

// امکانات
var rcFeatHead = Lbl("امکانات", new Font("Segoe UI", 7.5F), cDim, 6);
rcFeatHead.Dock = DockStyle.Top;
string[] feats = { "آپلود / دانلود فایل", "پیام بین دستگاه‌ها", "حذف فایل‌ها" };
var rcFeatBox = new Panel { Dock = DockStyle.Fill, BackColor = cCard };
int fy = 0;
foreach (var feat in feats)
{
    var row = new Label
    {
        Text = "–  " + feat,
        AutoSize = false, Dock = DockStyle.Top,
        Height = 19, ForeColor = cSub,
        BackColor = Color.Transparent,
        Font = new Font("Segoe UI", 8.5F),
        Margin = new Padding(0)
    };
    rcFeatBox.Controls.Add(row);
}

// دکمه خروج
var btnExit = Btn("بستن برنامه", cRedBg, cRed, 0, 32);
btnExit.Dock = DockStyle.Bottom;

// چیدمان کارت راست
rightInner.Controls.Add(rcFeatBox);     // Fill
rightInner.Controls.Add(rcFeatHead);
rightInner.Controls.Add(rcPathVal);
rightInner.Controls.Add(rcPathHead);
rightInner.Controls.Add(rcStatusVal);
rightInner.Controls.Add(rcStatusHead);
rightInner.Controls.Add(rcPortVal);
rightInner.Controls.Add(rcPortHead);
rightInner.Controls.Add(rcTitle);
rightCard.Controls.Add(rightInner);
sharedCard.Controls.Add(btnSharedFolder);
sharedCard.Controls.Add(rcSharedVal);
sharedCard.Controls.Add(rcSharedHead);
rightCard.Controls.Add(sharedCard);
rightCard.Controls.Add(btnExit);

grid.Controls.Add(leftCard, 0, 0);
grid.Controls.Add(rightCard, 1, 0);

// ── هدر + grid را به canvas اضافه می‌کنیم ───────────────
// هدر با Panel محاسبه‌شده به بالای canvas می‌رود
var headerWrap = new Panel
{
    Dock = DockStyle.Top,
    Height = 56,
    BackColor = Color.Transparent,
    Margin = new Padding(0, 0, 0, 12)
};
lblTitle.Location = new Point(0, 0);
lblSub.Location   = new Point(2, 32);
badgeLive.Location = new Point(0, 0); // در headerWrap تنظیم می‌شود
headerWrap.Controls.Add(lblTitle);
headerWrap.Controls.Add(lblSub);

// badge سمت راست headerWrap
headerWrap.Paint += (_, e) =>
{
    var sz = TextRenderer.MeasureText("  ● LIVE  ", badgeLive.Font);
    badgeLive.Location = new Point(headerWrap.Width - sz.Width - 4, 8);
};
headerWrap.Controls.Add(badgeLive);

canvas.Controls.Add(grid);        // Fill — باید اول اضافه بشه
canvas.Controls.Add(headerWrap);  // Top — بعد، روی grid می‌نشینه

void RefreshNetwork()
{
    networkBox.Items.Clear();
    if (!networkSharingEnabled)
    {
        networkBox.Items.Add("Network sharing needs Windows permission. Restart app after allowing access.");
        return;
    }

    var urls = GetLocalIps().Select(ip => $"http://{ip}:{Port}").ToList();
    if (urls.Count == 0)
    {
        networkBox.Items.Add("آدرس شبکه‌ای پیدا نشد");
        return;
    }
    foreach (var url in urls)
        networkBox.Items.Add(url);
}

void RefreshSharedFolderUi()
{
    var value = ShortPath(sharedFolder, 35);
    if (string.IsNullOrWhiteSpace(value))
    {
        rcSharedVal.Text = "تنظیم نشده";
        return;
    }

    rcSharedVal.Text = value;
    return;
    rcSharedVal.Text = string.IsNullOrWhiteSpace(value) ? "ØªÙ†Ø¸ÛŒÙ… Ù†Ø´Ø¯Ù‡" : value;
}

string ShortPath(string value, int maxLength)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    return value.Length > maxLength ? "..." + value[^Math.Max(0, maxLength - 3)..] : value;
}

void Shutdown()
{
    try { listener.Stop(); } catch { }
    try { listener.Close(); } catch { }
}

HttpListener StartHttpListener(out bool networkEnabled)
{
    if (TryStartListener($"http://+:{Port}/", out var publicListener))
    {
        networkEnabled = true;
        return publicListener;
    }

    TryAddUrlAcl();

    if (TryStartListener($"http://+:{Port}/", out publicListener))
    {
        networkEnabled = true;
        return publicListener;
    }

    networkEnabled = false;
    if (TryStartListener($"http://localhost:{Port}/", out var localListener))
    {
        return localListener;
    }

    throw new InvalidOperationException($"Could not start FileShare on port {Port}.");
}

bool TryStartListener(string prefix, out HttpListener result)
{
    result = new HttpListener();
    try
    {
        result.Prefixes.Add(prefix);
        result.Start();
        return true;
    }
    catch
    {
        try { result.Close(); } catch { }
        return false;
    }
}

void TryAddUrlAcl()
{
    try
    {
        var args = $"/c netsh http add urlacl url=http://+:{Port}/ user=Everyone";
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = args,
            Verb = "runas",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
        process?.WaitForExit(15000);
    }
    catch
    {
        // If the user cancels UAC, FileShare still works locally.
    }
}

void UseImmersiveDarkTitleBar(Form target)
{
    if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
    {
        return;
    }

    const int DwmwaUseImmersiveDarkMode = 20;
    var enabled = 1;
    DwmSetWindowAttribute(target.Handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
}

[DllImport("dwmapi.dll")]
static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

void SetClipboardText(string value)
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            Clipboard.SetText(value);
        }
        catch (Exception error)
        {
            failure = error;
        }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
    {
        throw failure;
    }
}

string? ShowFolderPicker(string initialPath)
{
    string? selectedPath = null;
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select shared folder",
                UseDescriptionForTitle = true,
                SelectedPath = initialPath
            };

            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                selectedPath = dialog.SelectedPath;
            }
        }
        catch (Exception error)
        {
            failure = error;
        }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
    {
        throw failure;
    }

    return selectedPath;
}

btnOpen.Click += (_, _) =>
{
    try
    {
        OpenBrowser(localUrl);
    }
    catch
    {
        MessageBox.Show(form, "Could not open the browser. Copy the address and open it manually.", "FileShare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
};
btnCopy.Click += (_, _) =>
{
    try
    {
        var value = networkBox.SelectedItem?.ToString();
    if (string.IsNullOrWhiteSpace(value) || value.StartsWith("آدرس شبکه‌ای", StringComparison.Ordinal))
    {
        value = localUrl;
    }
        SetClipboardText(value);
        statusBar.Text = $"کپی شد: {value}";
    }
    catch (Exception error)
    {
        MessageBox.Show(form, $"Could not copy the address:\r\n{error.Message}", "FileShare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
};
btnRefresh.Click += (_, _) =>
{
    try
    {
        RefreshNetwork();
        statusBar.Text = "آدرس‌های شبکه به‌روز شد.";
    }
    catch (Exception error)
    {
        MessageBox.Show(form, $"Could not refresh network addresses:\r\n{error.Message}", "FileShare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
};
btnSharedFolder.Click += (_, _) =>
{
    try
    {
        var initialPath = Directory.Exists(sharedFolder)
            ? sharedFolder
            : Directory.Exists(uploadDir)
                ? uploadDir
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var selectedPath = ShowFolderPicker(initialPath);
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        sharedFolder = selectedPath;
        Directory.CreateDirectory(sharedFolder);
        File.WriteAllText(sharedFolderConfig, sharedFolder, Encoding.UTF8);
        RefreshSharedFolderUi();
        statusBar.Text = "Shared folder updated.";
    }
    catch (Exception error)
    {
        MessageBox.Show(form, $"Could not set shared folder:\r\n{error.Message}", "FileShare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
};
btnExit.Click += (_, _) =>
{
    try
    {
        form.Close();
    }
    catch
    {
        Shutdown();
        Application.Exit();
    }
};
form.FormClosing += (_, _) => Shutdown();

RefreshNetwork();

Application.Run(form);

async Task HandleRequestAsync(HttpListenerContext context)
{
    var req = context.Request;
    var res = context.Response;
    var path = req.Url?.AbsolutePath ?? "/";

    try
    {
        if (req.HttpMethod == "GET" && path == "/")
        {
            await SendResourceAsync(res, "wwwroot.index.html", "text/html; charset=utf-8");
            return;
        }

        if (req.HttpMethod == "GET" && path == "/icon.ico")
        {
            await SendResourceAsync(res, "wwwroot.icon.ico", "image/x-icon");
            return;
        }

        if (req.HttpMethod == "GET" && path == "/api/network")
        {
            await SendJsonAsync(res, new
            {
                port = Port,
                local = localUrl,
                links = networkSharingEnabled
                    ? GetLocalIps().Select(ip => new { ip, url = $"http://{ip}:{Port}" })
                    : []
            });
            return;
        }

        if (req.HttpMethod == "GET" && path == "/api/files")
        {
            var files = GetVisibleFiles()
                .OrderByDescending(file => file.Modified);
            await SendJsonAsync(res, files);
            return;
        }

        if (req.HttpMethod == "GET" && path.StartsWith("/download/", StringComparison.Ordinal))
        {
            var request = ParseFileRequest(path["/download/".Length..]);
            var filePath = GetFilePath(request.Name, request.Source);
            if (!File.Exists(filePath))
            {
                await SendTextAsync(res, 404, "File not found");
                return;
            }

            res.StatusCode = 200;
            res.ContentType = "application/octet-stream";
            res.AddHeader("Content-Disposition", $"attachment; filename*=UTF-8''{Uri.EscapeDataString(request.Name)}");
            await using var stream = File.OpenRead(filePath);
            res.ContentLength64 = stream.Length;
            await stream.CopyToAsync(res.OutputStream);
            return;
        }

        if (req.HttpMethod == "DELETE" && path.StartsWith("/api/files/", StringComparison.Ordinal))
        {
            var request = ParseFileRequest(path["/api/files/".Length..]);
            if (request.Source != "upload")
            {
                await SendTextAsync(res, 403, "Shared files cannot be deleted here");
                return;
            }

            var filePath = GetFilePath(request.Name, request.Source);
            if (!File.Exists(filePath))
            {
                await SendTextAsync(res, 404, "File not found");
                return;
            }

            File.Delete(filePath);
            await SendTextAsync(res, 200, "OK");
            return;
        }

        if (req.HttpMethod == "POST" && path == "/upload")
        {
            await SaveMultipartUploadAsync(req);
            await SendTextAsync(res, 200, "OK");
            return;
        }

        if (req.HttpMethod == "GET" && path == "/api/messages")
        {
            var after = long.TryParse(req.QueryString["after"], out var value) ? value : 0;
            await SendJsonAsync(res, ReadMessages().Where(message => message.Id > after));
            return;
        }

        if (req.HttpMethod == "GET" && path == "/api/messages/download")
        {
            var body = string.Join("\n\n---\n\n", ReadMessages().Select(message =>
            {
                var from = string.IsNullOrWhiteSpace(message.From) ? string.Empty : $" ({message.From})";
                return $"[{message.Created:yyyy-MM-dd HH:mm:ss}{from}]\n{message.Text}";
            }));
            res.AddHeader("Content-Disposition", $"attachment; filename*=UTF-8''{Uri.EscapeDataString("fileshare-messages.txt")}");
            await SendTextAsync(res, 200, body, "text/plain; charset=utf-8");
            return;
        }

        if (req.HttpMethod == "POST" && path == "/api/messages")
        {
            var data = await JsonSerializer.DeserializeAsync<MessageInput>(req.InputStream, jsonOptions);
            if (string.IsNullOrWhiteSpace(data?.Text))
            {
                await SendTextAsync(res, 400, "No message text");
                return;
            }

            var message = new MessageItem(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(1000), (data.From ?? string.Empty)[..Math.Min(data.From?.Length ?? 0, 80)], data.Text, DateTimeOffset.UtcNow, null);
            await File.AppendAllTextAsync(messageLog, JsonSerializer.Serialize(message, jsonOptions) + "\n", Encoding.UTF8);
            await SendJsonAsync(res, message);
            return;
        }

        if (req.HttpMethod == "DELETE" && path == "/api/messages")
        {
            await File.WriteAllTextAsync(messageLog, string.Empty, Encoding.UTF8);
            await SendTextAsync(res, 200, "OK");
            return;
        }

        if (req.HttpMethod == "POST" && path == "/api/messages/delete")
        {
            var input = await JsonSerializer.DeserializeAsync<DeleteInput>(req.InputStream, jsonOptions);
            var ids = (input?.Ids ?? []).ToHashSet();
            var messages = ReadMessages();
            var next = messages.Where(message => !ids.Contains(message.Id)).ToList();
            await WriteMessagesAsync(next);
            await SendJsonAsync(res, new { deleted = messages.Count - next.Count });
            return;
        }

        if ((req.HttpMethod == "PUT" || req.HttpMethod == "DELETE") && path.StartsWith("/api/messages/", StringComparison.Ordinal))
        {
            if (!long.TryParse(path["/api/messages/".Length..], out var id))
            {
                await SendTextAsync(res, 400, "Bad message id");
                return;
            }

            var messages = ReadMessages();
            var index = messages.FindIndex(message => message.Id == id);
            if (index < 0)
            {
                await SendTextAsync(res, 404, "Message not found");
                return;
            }

            if (req.HttpMethod == "DELETE")
            {
                messages.RemoveAt(index);
                await WriteMessagesAsync(messages);
                await SendTextAsync(res, 200, "OK");
                return;
            }

            var data = await JsonSerializer.DeserializeAsync<MessageInput>(req.InputStream, jsonOptions);
            if (string.IsNullOrWhiteSpace(data?.Text))
            {
                await SendTextAsync(res, 400, "No message text");
                return;
            }

            messages[index] = messages[index] with { Text = data.Text, Edited = DateTimeOffset.UtcNow };
            await WriteMessagesAsync(messages);
            await SendJsonAsync(res, messages[index]);
            return;
        }

        await SendTextAsync(res, 404, "Not found");
    }
    catch (Exception error)
    {
        Console.WriteLine(error);
        if (res.OutputStream.CanWrite)
        {
            await SendTextAsync(res, 500, "Server error");
        }
    }
    finally
    {
        res.OutputStream.Close();
    }
}

async Task SendFileAsync(HttpListenerResponse res, string filePath, string contentType)
{
    if (!File.Exists(filePath))
    {
        await SendTextAsync(res, 404, "File not found");
        return;
    }

    res.StatusCode = 200;
    res.ContentType = contentType;
    await using var stream = File.OpenRead(filePath);
    res.ContentLength64 = stream.Length;
    await stream.CopyToAsync(res.OutputStream);
}

Icon? LoadIconResource()
{
    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("wwwroot.icon.ico");
    return stream is null ? null : new Icon(stream);
}

async Task SendResourceAsync(HttpListenerResponse res, string resourceName, string contentType)
{
    await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
    if (stream is null)
    {
        await SendTextAsync(res, 404, "Resource not found");
        return;
    }

    res.StatusCode = 200;
    res.ContentType = contentType;
    res.ContentLength64 = stream.Length;
    await stream.CopyToAsync(res.OutputStream);
}

async Task SendTextAsync(HttpListenerResponse res, int status, string body, string contentType = "text/plain; charset=utf-8")
{
    var bytes = Encoding.UTF8.GetBytes(body);
    res.StatusCode = status;
    res.ContentType = contentType;
    res.ContentLength64 = bytes.Length;
    await res.OutputStream.WriteAsync(bytes);
}

async Task SendJsonAsync(HttpListenerResponse res, object value)
{
    var bytes = JsonSerializer.SerializeToUtf8Bytes(value, jsonOptions);
    res.StatusCode = 200;
    res.ContentType = "application/json; charset=utf-8";
    res.ContentLength64 = bytes.Length;
    await res.OutputStream.WriteAsync(bytes);
}

async Task SaveMultipartUploadAsync(HttpListenerRequest req)
{
    var contentType = req.ContentType ?? string.Empty;
    var match = Regex.Match(contentType, "boundary=(.+)$");
    if (!match.Success)
    {
        throw new InvalidOperationException("No boundary");
    }

    using var memory = new MemoryStream();
    await req.InputStream.CopyToAsync(memory);
    var body = memory.ToArray();
    var headerEnd = IndexOf(body, Encoding.UTF8.GetBytes("\r\n\r\n"));
    if (headerEnd < 0)
    {
        throw new InvalidOperationException("Bad headers");
    }

    var headers = Encoding.UTF8.GetString(body, 0, headerEnd);
    var fileMatch = Regex.Match(headers, "filename=\"([^\"]*)\"");
    if (!fileMatch.Success)
    {
        throw new InvalidOperationException("No file");
    }

    var start = headerEnd + 4;
    var ending = Encoding.UTF8.GetBytes($"\r\n--{match.Groups[1].Value}");
    var end = LastIndexOf(body, ending);
    if (end < start)
    {
        throw new InvalidOperationException("Bad ending");
    }

    var targetPath = UniquePath(SafeName(fileMatch.Groups[1].Value));
    await File.WriteAllBytesAsync(targetPath, body[start..end]);
}

List<MessageItem> ReadMessages()
{
    return File.ReadLines(messageLog, Encoding.UTF8)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .Select(line =>
        {
            try { return JsonSerializer.Deserialize<MessageItem>(line, jsonOptions); }
            catch { return null; }
        })
        .Where(message => message is not null)
        .Cast<MessageItem>()
        .ToList();
}

async Task WriteMessagesAsync(IEnumerable<MessageItem> messages)
{
    var body = string.Join("\n", messages.Select(message => JsonSerializer.Serialize(message, jsonOptions)));
    await File.WriteAllTextAsync(messageLog, string.IsNullOrEmpty(body) ? string.Empty : body + "\n", Encoding.UTF8);
}

IEnumerable<FileItem> GetVisibleFiles()
{
    foreach (var file in Directory.GetFiles(uploadDir))
    {
        var info = new FileInfo(file);
        yield return new FileItem(info.Name, info.Length, info.LastWriteTimeUtc, "upload", true);
    }

    if (string.IsNullOrWhiteSpace(sharedFolder) || !Directory.Exists(sharedFolder))
    {
        yield break;
    }

    foreach (var file in Directory.GetFiles(sharedFolder))
    {
        var info = new FileInfo(file);
        yield return new FileItem(info.Name, info.Length, info.LastWriteTimeUtc, "shared", false);
    }
}

(string Name, string Source) ParseFileRequest(string value)
{
    var raw = Uri.UnescapeDataString(value);
    var separator = raw.IndexOf('/', StringComparison.Ordinal);
    if (separator < 0)
    {
        return (SafeName(raw), "upload");
    }

    var source = raw[..separator] == "shared" ? "shared" : "upload";
    return (SafeName(raw[(separator + 1)..]), source);
}

string GetFilePath(string name, string source)
{
    var root = source == "shared" ? sharedFolder : uploadDir;
    return Path.Combine(root, SafeName(name));
}

string UniquePath(string fileName)
{
    var target = Path.Combine(uploadDir, fileName);
    var name = Path.GetFileNameWithoutExtension(fileName);
    var ext = Path.GetExtension(fileName);
    var count = 1;

    while (File.Exists(target))
    {
        target = Path.Combine(uploadDir, $"{name}-{count}{ext}");
        count++;
    }

    return target;
}

string SafeName(string? name)
{
    var baseName = Path.GetFileName(string.IsNullOrWhiteSpace(name) ? "file" : name);
    var invalid = Path.GetInvalidFileNameChars();
    var cleaned = new string(baseName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned;
}

IEnumerable<string> GetLocalIps()
{
    return NetworkInterface.GetAllNetworkInterfaces()
        .Where(item => item.OperationalStatus == OperationalStatus.Up)
        .SelectMany(item => item.GetIPProperties().UnicastAddresses)
        .Where(item => item.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(item.Address))
        .Select(item => item.Address.ToString())
        .Distinct();
}

void OpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
        // Opening the browser is a convenience; the server still works without it.
    }
}

int IndexOf(byte[] source, byte[] pattern)
{
    for (var i = 0; i <= source.Length - pattern.Length; i++)
    {
        if (pattern.SequenceEqual(source.AsSpan(i, pattern.Length).ToArray()))
        {
            return i;
        }
    }
    return -1;
}

int LastIndexOf(byte[] source, byte[] pattern)
{
    for (var i = source.Length - pattern.Length; i >= 0; i--)
    {
        if (pattern.SequenceEqual(source.AsSpan(i, pattern.Length).ToArray()))
        {
            return i;
        }
    }
    return -1;
}

record MessageInput(string? Text, string? From);
record DeleteInput(long[] Ids);
record FileItem(string Name, long Size, DateTime Modified, string Source, bool CanDelete);
record MessageItem(long Id, string From, string Text, DateTimeOffset Created, DateTimeOffset? Edited);
