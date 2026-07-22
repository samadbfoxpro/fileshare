# FileShare v1.5

## فارسی

**FileShare** یک نرم‌افزار فوق‌العاده سبک، مدرن و پرسرعت برای ویندوز است که امکان اشتراک‌گذاری فایل، مرور پوشه‌ها، استریم آنلاین ویدیو و تبادل پیام متنی در شبکه محلی (LAN / Wi-Fi) را فراهم می‌کند.

نسخه جدید: **1.5**

---

### 🌟 قابلیت‌های جدید و کلیدی

- **📁 مرور هوشمند پوشه‌ها و زیرپوشه‌ها (Subdirectory Explorer):** پشتیبانی از اسکن کامل زیرپوشه‌های داخل پوشه اشتراکی و آپلودها به همراه نوار مسیر (Breadcrumb Navigation).
- **⏯ پخش و استریم آنلاین ویدیو و صوت (Online Media Streaming):** مشاهده و پخش مستقیم فایل‌های ویدیویی (`MP4`, `MKV`, `WebM`, `MOV`, `AVI`) و صوتی بدون نیاز به دانلود کامل.
- **🚀 دانلود چندپارچه و قابلیت Resume (پشتیبانی کامل از IDM و ADM):**
  - پشتیبانی از درخواست‌های `Range Header` (`bytes=start-end`) و وضعیت HTTP `206 Partial Content`.
  - پشتیبانی از درخواست‌های `HEAD` و کلیدهای `ETag` و `Last-Modified` جهت دانلود با حداکثر سرعت و قابلیت ادامه (Pause / Resume).
- **📱 پلیر و وب‌پنل کاملاً واکنش‌گرا (Responsive Media Player):** کادربندی هوشمند و سازگار با ابعاد نمایشگر انواع گوشی‌ها، تبلت‌ها و کامپیوترها.
- **🎛 کنترل‌پنل دسکتاپ مدرن و جمع‌وجور:**
  - فرم ویندوزی شیک و کوچک جهت مدیریت سرور.
  - نمایش بارکد QR جهت اتصال سریع موبایل با اسکن دوربین.
  - دکمه کپی آدرس و باز کردن مستقیم در مرورگر پیش‌فرض سیستم.
- **🏗 معماری کد جدید و تفکیک‌شده (Modular Clean Architecture):** کد برنامه به ماژول‌های مجزای `FileService.cs` ، `MessageService.cs` ، `HttpServer.cs` و `MainForm.cs` تفکیک شده است.

---

### 📋 سایر امکانات

- اجرای مستقل ویندوزی بدون نمایش پنجره CMD
- تم تاریک و روشن شیک (Dark / Light Mode)
- آپلود مستقیم فایل از مرورگر سایر دستگاه‌ها به سیستم میزبان
- عدم امکان حذف فایل‌های پوشه اشتراکی از وب‌پنل جهت امنیت فایل‌های سیستم
- سیستم چت و تبادل متن زنده بین دستگاه‌ها با قابلیت ویرایش، کپی، حذف و دانلود خروجی متنی
- امکان خروجی تک‌فایل `FileShare.exe` (Self-Contained) بدون نیاز به نصب بودن .NET روی سیستم مقصد

---

### 📂 ساختار ذخیره‌سازی

داده‌های برنامه در مسیر زیر نگهداری می‌شوند:

```text
Downloads\FileShare
├── uploads\            # فایل‌های آپلود شده از وب
├── messages.jsonl       # تاریخچه پیام‌های متنی
└── shared-folder.txt   # مسیر پوشه اشتراکی انتخاب‌شده
```

---

### 🚀 روش استفاده

1. برنامه **`FileShare.exe`** را اجرا کنید.
2. آدرس‌های شبکه یا کد QR را در کنترل‌پنل مشاهده کنید.
3. با مرورگر گوشی یا سیستم دیگر (که به همان Wi-Fi یا LAN وصل است) آدرس را باز یا QR کد را اسکن کنید.
4. فایل‌ها و پوشه‌ها را مرور کنید، ویدیوها را به صورت آنلاین تماشا کنید یا با دانلود منجرهایی مانند IDM با سرعت بالا دانلود کنید.

---

### 💻 ساخت و اجرا از سورس کد

نیازمندی‌ها:
- Windows
- .NET SDK 9 یا بالاتر

ساخت پروژه:
```powershell
dotnet build
```

ایجاد خروجی تک‌فایل مستقل (Single Executable):
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

خروجی در مسیر زیر ساخته می‌شود:
```text
bin\Release\net9.0-windows\win-x64\publish\FileShare.exe
```

---

## English

**FileShare** is a lightweight, modern, and high-performance Windows desktop application designed for seamless file sharing, directory browsing, online video streaming, and real-time text messaging over a local network (LAN / Wi-Fi).

Current Version: **1.5**

---

### 🌟 What's New in v1.5

- **📁 Subdirectory Explorer & Breadcrumb Navigation:** Browse nested folders and subdirectories inside shared folders effortlessly.
- **⏯ Online Video & Audio Streaming:** Stream media files (`MP4`, `MKV`, `WebM`, `MOV`, `AVI`, `MP3`, etc.) directly in the browser with HTTP 206 Range Request support.
- **🚀 IDM Multi-Connection & Resume Support:** Full support for segmented multi-part downloading, `HEAD` requests, `ETag`, and `Last-Modified` headers for pause/resume compatibility with download managers like IDM and ADM.
- **📱 Responsive Media Player Modal:** Clean, auto-scaling media modal adapted for both desktop monitors and smartphone screens.
- **🎛 Compact Desktop Control Panel:** Sleek WinForms GUI with network address list, instant QR code generator for mobile connections, and default system browser launch.
- **🏗 Modular Clean Codebase:** Cleanly refactored into `FileService.cs`, `MessageService.cs`, `HttpServer.cs`, `MainForm.cs`, and a minimal `Program.cs`.

---

### 📋 Core Features

- Standalone Windows GUI app (no terminal window required)
- Sleek Dark and Light themes for the web interface
- Upload files directly from any connected phone or laptop to the host PC
- Read-only protection for host shared folders (prevents accidental deletion via web)
- Live text messaging with edit, copy, bulk delete, and export capabilities
- Publishable as a single-file `exe` with no .NET runtime required on target machines

---

### 📂 File Storage Layout

Application configuration and uploads are stored under:

```text
Downloads\FileShare
├── uploads\            # Uploaded files
├── messages.jsonl       # Text message logs
└── shared-folder.txt   # Selected shared folder path
```

---

### 💻 Build & Publish

Build Debug version:
```powershell
dotnet build
```

Publish as a self-contained single file:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Executable output path:
```text
bin\Release\net9.0-windows\win-x64\publish\FileShare.exe
```
