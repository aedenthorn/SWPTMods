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
            try
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "UnityPlayer.dll")))
                {
                    Console.WriteLine("Installer is in game folder.");
                    if (Directory.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BepInEx")))
                    {
                        Console.WriteLine("BepInEx folder already exists!");
                        Console.WriteLine("Press U to uninstall or Y to install anyway:");
                        ConsoleKeyInfo key = Console.ReadKey();
                        if (key.Key == ConsoleKey.U)
                        {
                            Console.WriteLine("This will remove all existing BepInEx data and any plugins already installed! Press Y if you're sure:");
                            key = Console.ReadKey();
                            if (key.Key == ConsoleKey.Y)
                            {
                                Console.WriteLine("Deleting BepInEx folder");
                                Directory.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BepInEx"), true);
                                Console.WriteLine("Deleting winhttp.dll");
                                File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "winhttp.dll"));
                                Console.WriteLine("Deleting doorstop_config.ini");
                                File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "doorstop_config.ini"));
                                Console.WriteLine("Deleting changelog.txt");
                                File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "changelog.txt"));
                                Console.WriteLine("\nBepInEx uninstalled! Press any key to exit...");
                            }
                            else
                            {
                                Console.WriteLine("Uninstall aborted! Press any key to exit...");
                            }
                        }
                        else if (key.Key == ConsoleKey.Y)
                        {
                            InstallTo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                            Console.WriteLine("\nPress any key to exit...");
                        }
                        Console.ReadKey();
                        return;
                    }

                    InstallTo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }
                Console.WriteLine("Game folder not found! Install here anyway? (Y to confirm)");
                var keyinfo = Console.ReadKey();
                if (keyinfo.Key == ConsoleKey.Y)
                {
                    InstallTo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("\n\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        private static void InstallTo(string gamePath)
        {
            Console.WriteLine("Looking for BepInEx archive...");

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            bool x64 = true;
            foreach(string file in Directory.GetFiles(path, "*.exe"))
            {
                if (!file.StartsWith("BepInEx") && Directory.Exists(Path.Combine(path, Path.GetFileNameWithoutExtension(file)+"_Data")))
                {
                    Console.WriteLine($"Basing architecture on {file}: {(GetAppCompiledMachineType(file) == MachineType.x86 ? "32-bit" : "64-bit")}");
                    x64 = GetAppCompiledMachineType(file) != MachineType.x86;
                }
            }

            Console.WriteLine($"Game appears to be {(x64 ? "64-bit":"32-bit")}...");


            string zipPath = null;
            foreach (string file in Directory.GetFiles(path, "*.zip"))
            {
                if ((x64 && Path.GetFileName(file).StartsWith("BepInEx_x64")) || (!x64 && Path.GetFileName(file).StartsWith("BepInEx_x86")))
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
                Match match = Regex.Match(source, "src=.(https://github.com/BepInEx/BepInEx/releases/expanded_assets/v5[^\"]+)\"");
                if (!match.Success)
                {
                    Console.WriteLine("Couldn't find latest BepInEx release, please visit https://github.com/BepInEx/BepInEx/releases/ to download the latest release.");
                    File.WriteAllText("test.txt", source);
                    Console.ReadKey();
                    return;
                }
                source = client.DownloadString(match.Groups[1].Value);

                match = Regex.Match(source, "href=.(/BepInEx/BepInEx/releases/download/v[^/]+/BepInEx_" + (x64 ? "x64" : "x86") + "[^\"]+)\"");
                if (!match.Success)
                {
                    Console.WriteLine("Couldn't find latest BepInEx file, please visit https://github.com/BepInEx/BepInEx/releases/ to download the latest release.");
                    File.WriteAllText("test.txt", source);
                    Console.ReadKey();
                    return;
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
                Console.ReadKey();
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

            if (!Directory.Exists(Path.Combine(gamePath, "BepInEx", "plugins")))
                Directory.CreateDirectory(Path.Combine(gamePath, "BepInEx","plugins"));

            Console.WriteLine($"BepInEx installed to {gamePath}!");
            File.Delete(zipPath);
        }

        public enum MachineType { Native = 0, x86 = 0x014c, Itanium = 0x0200, x64 = 0x8664 }

        public static MachineType GetAppCompiledMachineType(string fileName)
        {
            const int PE_POINTER_OFFSET = 60;
            const int MACHINE_OFFSET = 4;
            byte[] data = new byte[4096];
            using (Stream s = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                s.Read(data, 0, 4096);
            }
            // dos header is 64 bytes, last element, long (4 bytes) is the address of the PE header
            int PE_HEADER_ADDR = BitConverter.ToInt32(data, PE_POINTER_OFFSET);
            int machineUint = BitConverter.ToUInt16(data, PE_HEADER_ADDR + MACHINE_OFFSET);
            return (MachineType) machineUint;
        }
    }
}
