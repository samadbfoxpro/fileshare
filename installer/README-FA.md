# ساخت فایل نصب FileShare

برای ساخت فایل نصب قابل ارائه به کاربران، این دستور را در ریشه پروژه اجرا کنید:

```powershell
powershell -ExecutionPolicy Bypass -File .\installer\build-installer.ps1
```

خروجی اینجا ساخته می‌شود:

```text
dist\FileShareSetup.exe
```

این setup یک ویزارد نصب دارد و هنگام نصب می‌توانید مسیر نصب را انتخاب کنید. مسیر پیش‌فرض این است و نیاز به دسترسی Admin ندارد:

```text
%LOCALAPPDATA%\Programs\FileShare
```

بعد از نصب، شورتکات‌های Desktop و Start Menu ساخته می‌شوند. حذف نصب هم از Start Menu یا بخش Apps ویندوز قابل انجام است.

نکته: خود برنامه به صورت self-contained منتشر می‌شود و روی سیستم مقصد به نصب جداگانه .NET نیاز ندارد. برای پنجره داخلی برنامه، WebView2 Runtime باید روی ویندوز مقصد موجود باشد که روی بیشتر نسخه‌های جدید Windows 10 و Windows 11 از قبل نصب است.
