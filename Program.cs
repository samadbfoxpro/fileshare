using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace FileShare
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            const int Port = 8887;
            var appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "FileShare");
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var fileService = new FileService(appDir);
            var messageService = new MessageService(appDir, jsonOptions);
            var httpServer = new HttpServer(Port, fileService, messageService, jsonOptions);

            httpServer.StartAcceptLoop();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => { Console.WriteLine($"[ThreadException] {e.Exception}"); };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { Console.WriteLine($"[UnhandledException] {e.ExceptionObject}"); };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(httpServer, fileService));
        }
    }
}
