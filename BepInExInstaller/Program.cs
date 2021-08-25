using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BepInExInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            if(File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "UnityPlayer.dll")))
            {
                Console.WriteLine("Installer is in game folder already.");
                InstallTo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                return;
            }
            RegistryKey localMachine = Environment.Is64BitOperatingSystem ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64) : Registry.LocalMachine;
            RegistryKey openKey = localMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 1213740");
            if (openKey != null)
            {
                Console.WriteLine($"Found install location \"{(string)openKey.GetValue("InstallLocation")}\" from registry.");

                InstallTo((string)openKey.GetValue("InstallLocation"));
                return;
            }

            if (!File.Exists(@"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf"))
            {
                Console.WriteLine("Can't find Steam library!");
                return;
            }
            string[] lines = File.ReadAllLines(@"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf");
            bool folders = false;
            List<string> libs = new List<string>();
            foreach(string line in lines)
            {
                if (folders)
                {
                    if (line.StartsWith("}"))
                        break;
                    if (!line.StartsWith("\t"))
                        continue;
                    Match match = Regex.Match(line, "^\t\"([0-9]+)\"\t\t\"([^\"]+)\"");
                    if (match.Success)
                    {
                        libs.Add(match.Groups[2].Value);
                    }

                }
                else if(line == "\"LibraryFolders\"") 
                {
                    folders = true;
                }
            }
            if (!libs.Any())
            {
                Console.WriteLine("No Steam libraries found!");
                return;
            }
            foreach(string lib in libs)
            {
                string gamePath = Path.Combine(lib, @"steamapps\common\She Will Punish Them");
                //Console.WriteLine($"Checking for {gamePath}");
                if (Directory.Exists(gamePath))
                {
                    InstallTo(gamePath);
                    return;
                }
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
            return;
        }
    }
}
