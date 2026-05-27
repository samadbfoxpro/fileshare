راهنمای استفاده از پروژه FileShare در Visual Studio

1. کل پوشه FileShareVisualStudio را به هر سیستمی که می خواهی منتقل کن.
2. داخل پوشه، فایل FileShareVisualStudio.csproj را با Visual Studio باز کن.
3. برای اجرای تست، دکمه Start یا F5 را بزن.
4. برنامه روی پورت 8887 اجرا می شود و مرورگر را باز می کند.
5. فایل های آپلود و پیام ها در این مسیر ذخیره می شوند:
   Downloads\FileShare

فایل های مهم:

- Program.cs
  کد اصلی برنامه و سرور داخلی است.

- wwwroot\index.html
  ظاهر وب برنامه است.

- wwwroot\icon.ico
  هم لوگوی وب است، هم آیکون فایل exe.

- FileShareVisualStudio.csproj
  تنظیمات پروژه Visual Studio است.

ساخت exe تک فایلی:

1. در Visual Studio روی پروژه راست کلیک کن.
2. Publish را بزن.
3. اگر پروفایل FolderProfile را دیدی، همان را انتخاب کن.
4. Publish را بزن.
5. خروجی اینجا ساخته می شود:
   bin\Release\net9.0\win-x64\publish\FileShare.exe

اگر Publish گفت runtime لازم را باید دانلود کند، اینترنت را وصل کن و دوباره Publish بزن.
