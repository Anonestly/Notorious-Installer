using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.IO;
using Microsoft.Win32;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;

namespace Notorious_Installer
{
    public partial class MainWindow : MetroWindow
    {

        private protected static string CurrentVersion = "1.0";
        private protected static string VRChatInstallDir;
        private protected static string AppdataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Notorious";
        private protected static string AuthFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Notorious\Auth.txt";
        private protected static string SteamVRChatInstallDir32 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null)?.ToString() + @"\steamapps\common\VRChat\VRChat.exe";
        private protected static string SteamVRChatInstallDir64 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", null)?.ToString() + @"\steamapps\common\VRChat\VRChat.exe";
        private protected static string SteamVRChatInstallDirAlternative = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 438100", "InstallLocation", null)?.ToString();



        public MainWindow()
        {
            InitializeComponent();

            CheckForUpdates(); // Start the application.
        }



        private protected async void CheckForUpdates()
        {

            try
            {
                using (WebClient ReadReq = new WebClient())
                {
                    string RemoteVersion = ReadReq.DownloadString("https://meap.gg/dl/Notorious_Installer.txt");

                    // If current version doesn't match remote version then we are out of date.
                    if (CurrentVersion != RemoteVersion)
                    {
                        await this.ShowMessageAsync("Update found!", "This installer is out of date.\nWe will now send you to the download of the new installer.");
                        Process.Start("https://meap.gg/dl/notorious");
                        Environment.Exit(0);
                    }
                    else // We are up to date.
                    {

                        // Check if VRChat is currently running.
                        Process[] VRChat = Process.GetProcessesByName("VRChat");
                        if (VRChat.Length != 0)
                        {
                            // VRChat is running, the installer cannot continue.
                            var ExitOnly = new MetroDialogSettings()
                            {
                                AffirmativeButtonText = "Exit",
                            };
                            await this.ShowMessageAsync("Please close VRChat", "The installer cannot continue while VRChat is running.", MessageDialogStyle.Affirmative, ExitOnly);
                            Environment.Exit(0);
                        }

                        // Start the rest of the application.
                        ValidateLicense();

                    }
                }
            }
            catch { await this.ShowMessageAsync("Could not connect", "Could not connect to server.\nPlease try again later or contact support if the problem persists.\n\ndiscord.gg/NotoriousV2"); Environment.Exit(0); }

        }



        private protected async void ValidateLicense()
        {

            string LicenseKey;
            if (File.Exists(AuthFile))
            {

                // Read all content from the auth file and turn it into a string.
                string content = File.ReadAllText(AuthFile);

                // Check if the Auth file is empty or invalid.
                if (content.Length != 256)
                {
                    // Auth file is empty or invalid, write user entered license key into the Auth file.
                    LicenseKey = await this.ShowInputAsync("Welcome", "Please enter your license key.\n\nDon't own a license key?\nGet one here: meap.gg/buy");

                    // If user presses 'Cancel' the popup will instantly come back.
                    if (LicenseKey == null || LicenseKey.Length != 172)
                    {
                        var msgSettings = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = "Ok",
                            NegativeButtonText = "Exit",
                        };
                        MessageDialogResult Result = await this.ShowMessageAsync("Invalid license key", "Please try again.", MessageDialogStyle.AffirmativeAndNegative, msgSettings);
                        if (Result == MessageDialogResult.Affirmative)
                        {
                            // User clicked Ok.
                            ValidateLicense();
                            return;
                        }
                        else // User pressed Exit.
                        {
                            Environment.Exit(0); // Closes the application.
                        }
                    }

                    // Write the license key that the user entered into the Auth file.
                    File.WriteAllText(AuthFile, LicenseKey);

                    FindInstallDir();
                }
                else // License key is valid.
                {
                    FindInstallDir();
                }

            }
            else // Auth file does not exist.
            {

                // Make sure that the Notorious directory exists inside of appdata directory.
                Directory.CreateDirectory(AppdataDir);

                // Ask user for license key and write it into Auth file.
                LicenseKey = await this.ShowInputAsync("Welcome", "Please enter your license key.\n\nDon't own a license key?\nGet one here: meap.gg/buy");

                // If user presses 'Cancel' the popup will instantly come back.
                if (LicenseKey == null || LicenseKey.Length != 172)
                {
                    var msgSettings = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "Ok",
                        NegativeButtonText = "Exit",
                    };
                    MessageDialogResult Result = await this.ShowMessageAsync("Invalid license key", "Please try again.", MessageDialogStyle.AffirmativeAndNegative, msgSettings);
                    if (Result == MessageDialogResult.Affirmative)
                    {
                        // User clicked Ok.
                        ValidateLicense();
                        return;
                    }
                    else // User pressed Exit.
                    {
                        Environment.Exit(0); // Closes the application.
                    }
                }

                // Write the license key that the user entered into the Auth file.
                File.WriteAllText(AuthFile, LicenseKey);

                FindInstallDir();

            }

        }



        private protected ProgressDialogController controller;
        private protected static bool RanBefore = false;
        private protected async void FindInstallDir()
        {

            if (!RanBefore)
            {
                RanBefore = true; // Makes sure this won't run again.
                if (File.Exists(SteamVRChatInstallDir32))
                    VRChatInstallDir = SteamVRChatInstallDir32;
                else if (File.Exists(SteamVRChatInstallDir64))
                    VRChatInstallDir = SteamVRChatInstallDir64;
                else if (File.Exists(SteamVRChatInstallDirAlternative))
                    VRChatInstallDir = SteamVRChatInstallDirAlternative;
            }



            if (VRChatInstallDir != null)
            {
                // Ask the user if the found path is correct or not.
                var YesNo = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "Yes",
                    NegativeButtonText = "No",
                };
                // Remove 'VRChat.exe' from the filepath leaving us with just the directory.
                string PathWithoutExecutable = VRChatInstallDir.Replace(@"\VRChat.exe", "");
                MessageDialogResult Result = await this.ShowMessageAsync("Game directory found", PathWithoutExecutable + "\n\nDo you want to install Notorious here?", MessageDialogStyle.AffirmativeAndNegative, YesNo);
                if (Result == MessageDialogResult.Affirmative)
                {
                    Install(PathWithoutExecutable);
                }
                else // User clicked no.
                {
                    ShowFileDialog();
                }
            }
            else // No VRChat install directory was found.
            {
                ShowFileDialog();
            }

        }



        private protected async void ShowFileDialog()
        {

            VRChatInstallDir = null;
            await this.ShowMessageAsync("Please select VRChat executable", "Please navigate to the game install directory and select VRChat.exe", MessageDialogStyle.Affirmative);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Please navigate to the game install directory and select VRChat.exe";
            openFileDialog.Filter = "VRChat (*.exe)|*.exe";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == true)
            {
                // Turn selected filepath into a string.
                string VRChatExecutablePath = openFileDialog.FileName;

                // If the path is not empty and contains 'VRChat.exe' then continue.
                if (!string.IsNullOrEmpty(VRChatExecutablePath) && VRChatExecutablePath.Contains("VRChat.exe"))
                {

                    // Remove 'VRChat.exe' from the filepath leaving us with just the game directory.
                    VRChatInstallDir = VRChatExecutablePath.Replace(@"\VRChat.exe", "");

                    Install(VRChatInstallDir);

                }
                else // Selected file is not 'VRChat.exe'
                {
                    await this.ShowMessageAsync("Wrong file", "The file you selected is not VRChat.exe\nPlease try again.", MessageDialogStyle.Affirmative);
                    FindInstallDir(); // Start the openfiledialog again.
                }
            }
            else // User closed the window or clicked cancel.
            {
                await this.ShowMessageAsync("Goodbye!", "The installer will now close.", MessageDialogStyle.Affirmative);
                Environment.Exit(0);
            }

        }



        private protected async void Install(string path)
        {

            try
            {
                using (WebClient DL = new WebClient())
                {

                    // Start installation progress message.
                    controller = await this.ShowProgressAsync("Installing, please wait...", "Checking for existing loader files...");
                    controller.SetCancelable(false);
                    controller.SetIndeterminate();

                    // Check if MelonLoader is already installed and if so do a cleanup of previous install.
                    string MelonLoaderFolder = path + @"\MelonLoader";
                    if (Directory.Exists(MelonLoaderFolder)) { controller.SetMessage("Removing: 'MelonLoader' directory..."); await Task.Delay(500); Directory.Delete(MelonLoaderFolder, true); }

                    // Delete 'version.dll' file if present.
                    string VersionDLL = path + @"\version.dll";
                    if (File.Exists(VersionDLL)) { controller.SetMessage("Removing: version.dll"); await Task.Delay(500); File.Delete(VersionDLL); }

                    // Install latest custom MelonLoader files from zip.
                    controller.SetMessage("Preparing to install..."); await Task.Delay(500);



                    string DownloadURL = "https://meap.gg/dl/MelonLoader.zip";
                    string TemporaryExtractFile = path + @"\temp.zip";

                    // Download installer files into the game directory.
                    controller.SetMessage("Downloading latest loader files..."); await Task.Delay(500);
                    DL.DownloadFile(DownloadURL, TemporaryExtractFile);

                    // Extract the installer files.
                    controller.SetMessage("Extracting loader files..."); await Task.Delay(500);
                    using (var strm = File.OpenRead(TemporaryExtractFile))
                    using (ZipArchive a = new ZipArchive(strm))
                    {
                        a.Entries.Where(o => o.Name == string.Empty && !Directory.Exists(System.IO.Path.Combine(path, o.FullName))).ToList().ForEach(o => Directory.CreateDirectory(System.IO.Path.Combine(path, o.FullName)));
                        a.Entries.Where(o => o.Name != string.Empty).ToList().ForEach(e => e.ExtractToFile(System.IO.Path.Combine(path, e.FullName), true));
                    }

                    // Cleanup, so we don't leave any unnecessary files behind.
                    controller.SetMessage("Cleaning up..."); await Task.Delay(500);
                    File.Delete(TemporaryExtractFile);

                    // Closes the progress message window.
                    await controller.CloseAsync();



                    // Check mod compatibility and give warning if needed.
                    string ModsFolder = path + @"\Mods";
                    DirectoryInfo d = new DirectoryInfo(ModsFolder);
                    FileInfo[] Files = d.GetFiles("*.dll");
                    if (Files.Length > 1)
                        await this.ShowMessageAsync("Compatibility warning", "Mulitple mods were found in your mods folder.\nPlease note that some mods may not be compatible with Notorious.\n\nIf you run into any issues, please try removing other mods first before contacting support.", MessageDialogStyle.Affirmative);



                    // Ask the user if they want to start their game now or later.
                    var YesNo = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "Yes",
                        NegativeButtonText = "No",
                    };
                    MessageDialogResult Result = await this.ShowMessageAsync("All done!", "Notorious has successfully been installed.\nWould you like to start VRChat now?", MessageDialogStyle.AffirmativeAndNegative, YesNo);
                    if (Result == MessageDialogResult.Affirmative)
                    {
                        Process.Start(path + @"\VRChat.exe");
                        Environment.Exit(0);
                    }
                    Environment.Exit(0);

                }
            }
            catch (Exception ex) // An error occurred during the installation.
            {

                // Closes the progress message window since the installation encountered an error.
                await controller.CloseAsync();

                var YesNo = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "Yes",
                    NegativeButtonText = "No",
                };

                await this.ShowMessageAsync("Something went wrong", "Something went wrong while installing.", MessageDialogStyle.Affirmative);

                // Ask the user if they want to create a dump file.
                MessageDialogResult Result = await this.ShowMessageAsync("Report issue?", "Do you want to dump a log file?\n\nYou can send us this log file in our Discord.\nThis way we can track down and fix your issue.\n\ndiscord.gg/NotoriousV2", MessageDialogStyle.AffirmativeAndNegative, YesNo);
                if (Result == MessageDialogResult.Affirmative)
                {
                    File.WriteAllText("Installer-Error.log", "Error occurred during installation process.\n\n" + ex.ToString());
                    await this.ShowMessageAsync("Error log created", "'Installer-Error.log' successfully created.\nPlease send this file to a developer in our Discord.\n\ndiscord.gg/NotoriousV2", MessageDialogStyle.Affirmative);
                    Process.Start(Directory.GetCurrentDirectory());
                }
                Environment.Exit(0); // Closes the application.

            }

        }

    }
}