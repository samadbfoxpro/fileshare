# FileShare

## فارسی

**FileShare** یک برنامه سبک ویندوزی برای اشتراک‌گذاری فایل و پیام در شبکه محلی است. برنامه یک پنجره مدیریتی تمیز دارد و هم‌زمان یک پنل وب داخلی اجرا می‌کند تا دستگاه‌های دیگر داخل همان شبکه، مثل موبایل یا لپ‌تاپ، بتوانند از طریق مرورگر به فایل‌ها و پیام‌ها دسترسی داشته باشند.

نسخه فعلی: **1.2**

### قابلیت‌های اصلی

- اجرای برنامه بدون نمایش ترمینال، با پنجره ویندوزی مستقل
- پنل وب واکنش‌گرا با تم تاریک و روشن
- نمایش آدرس‌های شبکه محلی برای اتصال دستگاه‌های دیگر
- آپلود فایل از مرورگر دستگاه‌های دیگر به سیستم میزبان
- دانلود فایل‌های آپلودشده از هر دستگاه داخل شبکه
- حذف فایل‌های آپلودشده از پنل وب
- تنظیم یک پوشه اشتراکی جدا از پوشه آپلود
- نمایش فایل‌های پوشه اشتراکی در پنل وب با برچسب «اشتراکی»
- دانلود فایل‌های پوشه اشتراکی بدون نیاز به آپلود دوباره
- جلوگیری از حذف فایل‌های پوشه اشتراکی از داخل پنل وب
- تبادل پیام متنی بین دستگاه‌ها
- کپی، ویرایش، حذف، انتخاب گروهی و دانلود پیام‌ها
- خروجی تک‌فایل `exe` به صورت self-contained، بدون نیاز به نصب .NET روی سیستم مقصد

### ساختار ذخیره‌سازی

به صورت پیش‌فرض داده‌های برنامه در مسیر زیر ذخیره می‌شوند:

```text
Downloads\FileShare
```

پوشه آپلود:

```text
Downloads\FileShare\uploads
```

فایل پیام‌ها:

```text
Downloads\FileShare\messages.jsonl
```

مسیر پوشه اشتراکی انتخاب‌شده:

```text
Downloads\FileShare\shared-folder.txt
```

### روش استفاده

1. برنامه `FileShare.exe` را اجرا کنید.
2. در پنجره برنامه، آدرس محلی و آدرس‌های شبکه را ببینید.
3. از دستگاه دیگر که به همان Wi-Fi یا شبکه محلی وصل است، یکی از آدرس‌های شبکه را در مرورگر باز کنید.
4. در تب «فایل‌ها» می‌توانید فایل آپلود یا دانلود کنید.
5. برای اشتراک‌گذاری فایل‌های یک پوشه موجود، در پنجره ویندوزی روی «تنظیم پوشه» بزنید و پوشه موردنظر را انتخاب کنید.
6. فایل‌های آن پوشه در پنل وب با برچسب «اشتراکی» نمایش داده می‌شوند.
7. در تب «تبادل پیام» می‌توانید متن را بین سیستم و دستگاه‌های دیگر جابه‌جا کنید.

### ساخت و اجرا از سورس

نیازمندی توسعه:

- Windows
- .NET SDK 9

ساخت پروژه:

```powershell
dotnet build
```

اجرای نسخه Debug:

```powershell
dotnet run
```

### ساخت خروجی تک‌فایل

برای ساخت یک فایل اجرایی مستقل برای ویندوز 64 بیت:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

خروجی در این مسیر ساخته می‌شود:

```text
bin\Release\net9.0-windows\win-x64\publish\FileShare.exe
```

این فایل روی سیستم مقصد به نصب جداگانه `.NET 9` نیاز ندارد.

### نکات مهم

- دستگاه‌ها باید داخل یک شبکه محلی باشند.
- اگر آدرس شبکه نمایش داده نشد، اتصال Wi-Fi یا تنظیمات شبکه را بررسی کنید.
- اگر فایروال ویندوز پرسید، دسترسی شبکه را برای برنامه مجاز کنید.
- برنامه روی پورت `8887` اجرا می‌شود.
- فایل‌های پوشه اشتراکی فقط برای دانلود نمایش داده می‌شوند و از پنل وب حذف نمی‌شوند.
- این برنامه برای شبکه‌های قابل اعتماد طراحی شده است؛ آن را روی شبکه عمومی یا ناشناس اجرا نکنید.

---

## English

**FileShare** is a lightweight Windows app for sharing files and text messages across a local network. It provides a clean desktop control window and runs an internal web panel so other devices on the same network, such as phones or laptops, can connect through a browser.

Current version: **1.2**

### Main Features

- Runs as a Windows desktop app without showing a terminal window
- Responsive web panel with dark and light themes
- Shows local network addresses for other devices
- Upload files from other devices to the host computer
- Download uploaded files from any device on the local network
- Delete uploaded files from the web panel
- Configure a separate shared folder
- Show shared-folder files in the web panel with a shared label
- Download shared-folder files without uploading them again
- Prevent deletion of shared-folder files from the web panel
- Exchange text messages between devices
- Copy, edit, delete, bulk-select, and download messages
- Publish as a self-contained single `exe`, with no .NET installation required on the target machine

### Storage Layout

By default, application data is stored under:

```text
Downloads\FileShare
```

Upload folder:

```text
Downloads\FileShare\uploads
```

Message log:

```text
Downloads\FileShare\messages.jsonl
```

Selected shared-folder path:

```text
Downloads\FileShare\shared-folder.txt
```

### How To Use

1. Run `FileShare.exe`.
2. Check the local and network addresses in the desktop window.
3. On another device connected to the same Wi-Fi or local network, open one of the network addresses in a browser.
4. Use the Files tab to upload and download files.
5. To share an existing folder, click the folder setup button in the desktop window and select a folder.
6. Files from that folder will appear in the web panel with a shared label.
7. Use the Messages tab to exchange text between the host computer and other devices.

### Build From Source

Development requirements:

- Windows
- .NET SDK 9

Build:

```powershell
dotnet build
```

Run the Debug version:

```powershell
dotnet run
```

### Publish A Single Executable

Create a self-contained single-file executable for 64-bit Windows:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

The output is created here:

```text
bin\Release\net9.0-windows\win-x64\publish\FileShare.exe
```

The generated executable does not require `.NET 9` to be installed on the target machine.

### Notes

- All devices must be on the same local network.
- If no network address appears, check Wi-Fi and network settings.
- If Windows Firewall prompts for access, allow the app on the local network.
- The app listens on port `8887`.
- Shared-folder files are download-only from the web panel and cannot be deleted there.
- This app is designed for trusted local networks. Do not run it on public or untrusted networks.
