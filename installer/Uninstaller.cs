using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace OpenRouterChatboxInstaller
{
    internal static class UninstallerProgram
    {
        private const string AppName = "OpenRouter Chatbox";

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var result = MessageBox.Show(
                "Remove OpenRouter Chatbox from this Windows user account?",
                "Uninstall " + AppName,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (result != DialogResult.OK)
            {
                return;
            }

            try
            {
                RemoveShortcutsAndRegistry();
                ScheduleInstallDirectoryRemoval();
                MessageBox.Show(
                    AppName + " was uninstalled.",
                    "Uninstall " + AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception error)
            {
                MessageBox.Show(
                    "Uninstall failed:\n\n" + error.Message,
                    "Uninstall " + AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void RemoveShortcutsAndRegistry()
        {
            DeleteIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk"));

            var startMenuDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName);
            if (Directory.Exists(startMenuDirectory))
            {
                Directory.Delete(startMenuDirectory, true);
            }

            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRouterChatbox", false);
        }

        private static void ScheduleInstallDirectoryRemoval()
        {
            var installDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrWhiteSpace(installDirectory))
            {
                return;
            }

            var command = "/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"" + installDirectory + "\"";
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = command,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
