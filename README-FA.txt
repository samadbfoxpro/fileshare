راهنمای استفاده و ساخت پروژه FileShare v1.5 در Visual Studio

1. پوشه پروژه FileShareVisualStudio را در Visual Studio باز کنید (فایل FileShareVisualStudio.csproj).
2. برای اجرای تست، کلید F5 یا دکمه Start را بزنید.
3. برنامه یک کنترل‌پنل شیک کوچک باز کرده و سرور محلی روی پورت 8887 قرار می‌گیرد.

امکانات جدید نسخه 1.5:
- امکان مرور زیرپوشه‌ها (Subdirectories) و پیمایش با Breadcrumbs در محیط وب.
- پخش و استریم آنلاین ویدیو و صوت (MP4, MKV, WebM, MP3...) پیش از دانلود.
- قابلیت دانلود چندپارتی (Multi-connection) و ادامه دانلود (Resume) سازگار با IDM.
- ساختار ماژولار و تمیز (Program.cs، FileService.cs، MessageService.cs، HttpServer.cs، MainForm.cs).

فایل‌های ساختاری اصلی:

- Program.cs
  نقطه شروع برنامه (Minimal EntryPoint).

- FileService.cs
  مدیریت فایل‌ها، زیرپوشه‌ها و امنیت مسیرها.

- MessageService.cs
  مدیریت چت‌ها و ذخیره‌سازی پیام‌ها.

- HttpServer.cs
  سرور داخلی HTTP، استریم ویدیو، دانلود IDM و APIها.

- MainForm.cs
  رابط کاربری دسکتاپ (WinForms Control Panel).

- wwwroot\index.html
  کلاینت وب مدرن و پلیر استریم آنلاین.

ساخت نسخه تک‌فایلی FileShare.exe:

1. در Visual Studio روی پروژه کلیک راست کنید و Publish را انتخاب نمایید.
2. گزینه Folder profile را انتخاب کرده و دکمه Publish را بزنید.
3. فایل اجرایی مستقل در مسیر زیر ایجاد می‌شود:
   bin\Release\net9.0-windows\win-x64\publish\FileShare.exe
