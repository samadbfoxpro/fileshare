# FileShare v1.5

## فارسی

**FileShare** یک نرم‌افزار فوق‌العاده سبک، مدرن و پرسرعت برای ویندوز است که امکان اشتراک‌گذاری فایل، مرور پوشه‌ها، استریم آنلاین ویدیو (حتی پسوندهای خاص و ساختار MPEG-TS) و تبادل پیام متنی در شبکه محلی (LAN / Wi-Fi) را فراهم می‌کند.

نسخه جدید: **1.5**

---

### 🌟 قابلیت‌های جدید و کلیدی در v1.5

- **📂 آپلود پیشرفته فایل و پوشه کامل (Advanced File & Folder Upload):** 
  - امکان آپلود تک‌فایل یا انتخاب ده‌ها فایل به صورت همزمان (Batch & Queue Upload).
  - کلید مجزا جهت انتخاب و آپلود یک پوشه کامل به همراه حفظ کامل ساختار درختی و زیرپوشه‌ها.
  - پشتیبانی کامل از Drag & Drop فایل و پوشه روی مرورگر.
- **🎬 پخش و استریم هوشمند آنلاین انواع ویدیو (MPEG-TS & Web Player):**
  - پخش مستقیم ویدیوهای `MP4`, `MKV`, `WebM`, `MOV`, `AVI`, `TS`.
  - ادغام داخلی کتابخانه `mpegts.js` (بدون نیاز به اینترنت) جهت رمزگشایی و پخش مستقیم فایل‌های ویدیویی با ساختار MPEG-TS و پسوندهای متنوع در مرورگر.
- **📁 مرور هوشمند پوشه‌ها و زیرپوشه‌ها (Subdirectory Explorer):** پشتیبانی از اسکن کامل زیرپوشه‌های داخل پوشه اشتراکی و آپلودها به همراه نوار مسیر (Breadcrumb Navigation).
- **🚀 دانلود چندپارچه و قابلیت Resume (پشتیبانی کامل از IDM و ADM):**
  - پشتیبانی از درخواست‌های `Range Header` (`bytes=start-end`) و وضعیت HTTP `206 Partial Content`.
  - پشتیبانی از درخواست‌های `HEAD` و کلیدهای `ETag` و `Last-Modified` جهت دانلود با حداکثر سرعت و قابلیت ادامه (Pause / Resume).
- **📱 وب‌پنل کاملاً واکنش‌گرا (Responsive Web UI):** کادربندی هوشمند و سازگار با ابعاد نمایشگر انواع گوشی‌ها، تبلت‌ها و کامپیوترها.
- **🎛 کنترل‌پنل دسکتاپ سفارشی و هماهنگ با تم تاریک:**
  - منوی انتخاب آی‌پی (ComboBox) هماهنگ با تم تاریک برنامه.
  - نمایش بارکد QR جهت اتصال سریع موبایل با اسکن دوربین.
  - دکمه کپی آدرس و باز کردن مستقیم در مرورگر پیش‌فرض سیستم.
- **🛡 پایداری فوق‌العاده سرور (Crash Protection):** عدم کرش یا بسته شدن ناگهانی سرور هنگام قطع اتصال یا لغو استریم توسط کاربر.

---

### 📋 سایر امکانات

- اجرای مستقل ویندوزی بدون نمایش پنجره CMD
- تم تاریک و روشن شیک (Dark / Light Mode)
- عدم امکان حذف فایل‌های پوشه اشتراکی از وب‌پنل جهت امنیت فایل‌های سیستم
- سیستم چت و تبادل متن زنده بین دستگاه‌ها با قابلیت ویرایش، کپی، حذف و دانلود خروجی متنی

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

### 🚀 اسکریپت‌های بیلد پروژه

پروژه دارای اسکریپت‌های سریع برای بیلد است:

- **`00_build-release.bat`**: بیلد نسخه نهایی (Release) در مسیر `bin\Release\net9.0-windows\FileShare.exe`
- **`build-debug.bat`**: بیلد نسخه دیباگ (Debug) در مسیر `bin\Debug\net9.0-windows\FileShare.exe`

---

## English

**FileShare** is a lightweight, modern, and high-performance Windows desktop application designed for seamless file sharing, directory browsing, online video streaming, and real-time text messaging over a local network (LAN / Wi-Fi).

Current Version: **1.5**

---

### 🌟 What's New in v1.5

- **📂 Advanced File & Folder Uploads:**
  - Multi-file batch upload support.
  - Full directory/folder upload support preserving complete nested directory trees.
  - Full Drag & Drop support for both files and entire folders.
- **🎬 Smart Media Streaming & MPEG-TS Support:**
  - Native and embedded `mpegts.js` (offline bundled) video player support for various video codecs and MPEG-TS containers.
- **📁 Subdirectory Explorer & Breadcrumb Navigation:** Browse nested folders and subdirectories inside shared folders effortlessly.
- **🚀 IDM Multi-Connection & Resume Support:** Full support for segmented multi-part downloading, `HEAD` requests, `ETag`, and `Last-Modified` headers for pause/resume compatibility with download managers like IDM and ADM.
- **🛡 Server Crash Protection:** Handles abrupt socket disconnects safely during stream seeking/cancellation.
- **🎛 Modern Desktop Control Panel:** Sleek WinForms GUI with dark-themed IP dropdown, instant QR code generator for mobile connections, and default system browser launch.

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

### 💻 Easy Build Scripts

- **`00_build-release.bat`**: Builds the Release binary output (`bin\Release\net9.0-windows\FileShare.exe`).
- **`build-debug.bat`**: Builds the Debug binary output (`bin\Debug\net9.0-windows\FileShare.exe`).
