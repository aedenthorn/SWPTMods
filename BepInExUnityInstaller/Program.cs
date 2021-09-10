using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BepInExInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            StartInstall();
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        static void StartInstall() {
            if(File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "UnityPlayer.dll")))
            {
                Console.WriteLine("Installer is in game folder already.");
                InstallTo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                return;
            }
            Console.WriteLine("Game folder not found!");
        }

        private static void InstallTo(string gamePath)
        {
            Console.WriteLine("Found game, looking for BepInEx archive...");

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string zipPath = null;
            foreach (string file in Directory.GetFiles(path, "*.zip"))
            {
                if ((Environment.Is64BitOperatingSystem && Path.GetFileName(file).StartsWith("BepInEx_x64")) || (!Environment.Is64BitOperatingSystem && Path.GetFileName(file).StartsWith("BepInEx_x86")))
                {
                    zipPath = file;
                    break;
                }
            }

            if (zipPath == null)
            {
                Console.WriteLine("BepInEx zip file not found, downloading from web...");
                var client = new WebClient();
                string source = client.DownloadString("https://github.com/BepInEx/BepInEx/releases/");
                Match match = Regex.Match(source, $"a href=.(/BepInEx/BepInEx/releases/download/v[^/]+/BepInEx_{(Environment.Is64BitOperatingSystem ? "x64" : "x86")}[^\"]+)\"");
                if (!match.Success)
                {
                    Console.WriteLine("Couldn't find latest BepInEx file, please visit https://github.com/BepInEx/BepInEx/releases/ to download the latest release.");
                }
                string latest = match.Groups[1].Value;
                Console.WriteLine($"Downloading https://github.com{latest}");
                string fileName = latest.Split('/')[latest.Split('/').Length - 1];
                client.DownloadFile("https://github.com" + latest, fileName);
                zipPath = Path.Combine(path, fileName);
                Console.WriteLine($"Downloaded https://github.com{latest}");
            }

            if (!File.Exists(zipPath))
            {
                Console.WriteLine($"Zip file {zipPath} does not exist!");
                return;
            }

            Console.WriteLine("Installing BepInEx...");

            var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                string f = Path.Combine(gamePath, entry.FullName);
                if (!Directory.Exists(Path.GetDirectoryName(f)))
                    Directory.CreateDirectory(Path.GetDirectoryName(f));
                entry.ExtractToFile(Path.Combine(gamePath, entry.FullName), true);
                Console.WriteLine($"Copying {entry.FullName}");
            }
            archive.Dispose();
            Console.WriteLine($"BepInEx installed to {gamePath}!");
            File.Delete(zipPath);
        }
    }
}
