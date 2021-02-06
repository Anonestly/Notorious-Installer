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

        private protected string CurrentVersion = "1.0";
        private protected string VRChatInstallDir;
        private protected string AppdataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToString() + @"\Notorious";
        private protected static string AuthFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToString() + @"\Notorious\Auth.txt";



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
                    Stream stream = ReadReq.OpenRead("https://meap.gg/dl/Notorious_Installer.txt");
                    StreamReader reader = new StreamReader(stream);
                    string RemoteVersion = reader.ReadToEnd();

                    // If current version doesn't match remote version then we are out of date.
                    if (CurrentVersion != RemoteVersion)
                    {
                        await this.ShowMessageAsync("Update found!", "This installer is out of date.\nWe will now send you to the download of the new installer.");
                        Process.Start("https://meap.gg/dl/notorious");
                        Environment.Exit(0);
                    }
                    else // We are up to date.
                    {
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

                    BrowseForGameDirectory();
                }
                else // License key is valid.
                {
                    BrowseForGameDirectory();
                }
            }
            else // Auth file does not exist.
            {
                // Create Notorious appdata directory if it doesn't exist yet.
                if (!Directory.Exists(AppdataDir)) { Directory.CreateDirectory(AppdataDir); }

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

                BrowseForGameDirectory();
            }

        }



        private protected ProgressDialogController controller;
        private protected async void BrowseForGameDirectory()
        {

            await this.ShowMessageAsync("Please select VRChat.exe", "Please select VRChat.exe inside of the game install directory.", MessageDialogStyle.Affirmative);

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

                    // Start installation progress message.
                    controller = await this.ShowProgressAsync("Installing, please wait...", "Checking for existing loader files...");
                    controller.SetCancelable(false);
                    controller.SetIndeterminate();

                    // Remove 'VRChat.exe' from the filepath leaving us with just the game directory.
                    VRChatInstallDir = VRChatExecutablePath.Replace(@"\VRChat.exe", "");

                    // Check if MelonLoader is already installed and if so do a cleanup of previous install.
                    string MelonLoaderFolder = VRChatInstallDir + @"\MelonLoader";
                    if (Directory.Exists(MelonLoaderFolder)) { controller.SetMessage("Removing: 'MelonLoader' directory..."); await Task.Delay(500); Directory.Delete(MelonLoaderFolder, true); }

                    // Delete 'version.dll' file if present.
                    string VersionDLL = VRChatInstallDir + @"\version.dll";
                    if (File.Exists(VersionDLL)) { controller.SetMessage("Removing: version.dll"); await Task.Delay(500); File.Delete(VersionDLL); }

                    // Install latest custom MelonLoader files from zip.
                    controller.SetMessage("Preparing to install..."); await Task.Delay(500);
                    Install(VRChatInstallDir);

                }
                else // Selected file is not 'VRChat.exe'
                {
                    await this.ShowMessageAsync("Wrong file", "The file you selected is not VRChat.exe\nPlease try again.", MessageDialogStyle.Affirmative);
                    BrowseForGameDirectory(); // Start the openfiledialog again.
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
                    string DownloadURL = "https://meap.gg/dl/MelonLoader.zip";
                    string TemporaryExtractFile = path + @"\temp.zip";

                    // Download installer files into the game directory.
                    controller.SetMessage("Downloading latest loader files..."); await Task.Delay(500);
                    byte[] bytes = DL.DownloadData(DownloadURL);
                    File.WriteAllBytes(TemporaryExtractFile, bytes);

                    // Extract the installer files.
                    controller.SetMessage("Extracting loader files..."); await Task.Delay(500);
                    using (var strm = File.OpenRead(TemporaryExtractFile))
                    using (ZipArchive a = new ZipArchive(strm))
                    {
                        a.Entries.Where(o => o.Name == string.Empty && !Directory.Exists(System.IO.Path.Combine(VRChatInstallDir, o.FullName))).ToList().ForEach(o => Directory.CreateDirectory(System.IO.Path.Combine(VRChatInstallDir, o.FullName)));
                        a.Entries.Where(o => o.Name != string.Empty).ToList().ForEach(e => e.ExtractToFile(System.IO.Path.Combine(VRChatInstallDir, e.FullName), true));
                    }

                    // Cleanup, so we don't leave any unnecessary files behind.
                    controller.SetMessage("Cleaning up..."); await Task.Delay(500);
                    File.Delete(TemporaryExtractFile);

                    // Closes the progress message window.
                    await controller.CloseAsync();

                    // Ask the user if they want to start their game now or later.
                    var msgSettings = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "Yes",
                        NegativeButtonText = "No",
                    };
                    MessageDialogResult Result = await this.ShowMessageAsync("All done!", "Notorious has successfully been installed.\nWould you like to start VRChat now?", MessageDialogStyle.AffirmativeAndNegative, msgSettings);
                    if (Result == MessageDialogResult.Affirmative)
                    {
                        Process.Start(VRChatInstallDir + @"\VRChat.exe");
                        Environment.Exit(0);
                    }
                    Environment.Exit(0);
                }
            }
            catch { await this.ShowMessageAsync("Something went wrong", "Something went wrong while installing, please try again or contact support if the problem persists.\n\ndiscord.gg/NotoriousV2", MessageDialogStyle.Affirmative); Environment.Exit(0); }
        }

    }
}