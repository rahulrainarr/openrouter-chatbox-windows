using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace OpenRouterChatboxInstaller
{
    internal static class InstallerProgram
    {
        private const string AppName = "OpenRouter Chatbox";
        private const string AppExe = "OpenRouter-Chatbox-Windows.exe";
        private const string UninstallerExe = "Uninstall-OpenRouter-Chatbox.exe";
        private const string AppResourceName = "OpenRouterChatboxApp.exe";
        private const string UninstallerResourceName = "OpenRouterChatboxUninstaller.exe";

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var result = MessageBox.Show(
                "Install OpenRouter Chatbox for the current Windows user?\n\n" +
                "The installer will create Desktop and Start Menu shortcuts.",
                AppName + " Setup",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK)
            {
                return;
            }

            try
            {
                Install();
                var launch = MessageBox.Show(
                    AppName + " was installed successfully.\n\nLaunch it now?",
                    AppName + " Setup",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (launch == DialogResult.Yes)
                {
                    Process.Start(InstalledAppPath());
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(
                    "Installation failed:\n\n" + error.Message,
                    AppName + " Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void Install()
        {
            Directory.CreateDirectory(InstallDirectory());
            ExtractResource(AppResourceName, InstalledAppPath());
            ExtractResource(UninstallerResourceName, InstalledUninstallerPath());

            CreateShortcut(DesktopShortcutPath(), InstalledAppPath(), "OpenRouter desktop AI chat client", InstalledAppPath());
            Directory.CreateDirectory(StartMenuDirectory());
            CreateShortcut(StartMenuShortcutPath(), InstalledAppPath(), "OpenRouter desktop AI chat client", InstalledAppPath());
            CreateShortcut(StartMenuUninstallShortcutPath(), InstalledUninstallerPath(), "Uninstall OpenRouter Chatbox", InstalledUninstallerPath(), "/uninstall");
            WriteUninstallRegistry();
        }

        private static void ExtractResource(string resourceName, string destination)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var input = assembly.GetManifestResourceStream(resourceName))
            {
                if (input == null)
                {
                    throw new InvalidOperationException("An embedded installation file could not be found.");
                }

                using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write))
                {
                    input.CopyTo(output);
                }
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string description, string iconPath, string arguments = "")
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("Windows shortcut service is unavailable.");
            }

            dynamic shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = InstallDirectory();
            shortcut.Description = description;
            shortcut.IconLocation = iconPath;
            shortcut.Arguments = arguments;
            shortcut.Save();
        }

        private static void WriteUninstallRegistry()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRouterChatbox"))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("Could not create the Windows uninstall entry.");
                }
                key.SetValue("DisplayName", AppName);
                key.SetValue("DisplayVersion", "1.0.0");
                key.SetValue("Publisher", "Rahul Raina");
                key.SetValue("InstallLocation", InstallDirectory());
                key.SetValue("DisplayIcon", InstalledAppPath());
                key.SetValue("UninstallString", "\"" + InstalledUninstallerPath() + "\" /uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        private static string InstallDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "OpenRouter Chatbox");
        }

        private static string InstalledAppPath()
        {
            return Path.Combine(InstallDirectory(), AppExe);
        }

        private static string InstalledUninstallerPath()
        {
            return Path.Combine(InstallDirectory(), UninstallerExe);
        }

        private static string DesktopShortcutPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk");
        }

        private static string StartMenuDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName);
        }

        private static string StartMenuShortcutPath()
        {
            return Path.Combine(StartMenuDirectory(), AppName + ".lnk");
        }

        private static string StartMenuUninstallShortcutPath()
        {
            return Path.Combine(StartMenuDirectory(), "Uninstall " + AppName + ".lnk");
        }
    }
}
