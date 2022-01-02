using System;
using System.Diagnostics;
using System.IO;
using L4d2AddonsMgr.AcfFileSpace;
using Microsoft.Win32;

namespace L4d2AddonsMgr.Service {

    public static class GameDirService {

        public static string LocateInstalledGame() {
            string path = null;
            // Find Steam install path from registry.
            try {
                path = LocateSteamDirFromRegistry();
            } catch (System.Exception ex) {
                Debug.WriteLine(ex);
            }
            if (path == null) {
                throw new Exception(Exception.Reason.SteamPathNotFoundInRegistry);
            }

            // Find game dir in all Steam libraries.
            try {
                path = FindGameInLibraries(path);
            } catch (System.Exception ex) {
                Debug.WriteLine(ex);
                path = null;
            }
            if (path == null) throw new Exception(Exception.Reason.GamePathNotFound);
            if (!ValidateGameDir(path)) throw new Exception(Exception.Reason.GamePathBroken);
            return path;
        }

        private static string LocateSteamDirFromRegistry() {

            // Using the registry is a good idea.
            // https://stackoverflow.com/questions/908850/get-installed-applications-in-a-system
            // Later given up finding L4d2 installation in reg
            // SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall directly, it's path is incorrect.
            // (remain the same after moved to another Steam library dir in Steam.)
            // Btw only this got how to make it work on both 32bit and 64bit Windows.
            // https://stackoverflow.com/questions/24909108/get-installed-software-list-using-c-sharp#comment38699255_24909354

            // Let's first find Steam itself..
            // It should have been searched for what was wanted DIRECTLY before plotting a whole
            // Road map and searching for its components.
            // https://stackoverflow.com/questions/34090258/find-steam-games-folder
            const string KeyName = @"SOFTWARE\Valve\Steam";
            const string SubKeyName = "InstallPath";

            using (var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)) {
                using (var key = localMachine.OpenSubKey(KeyName, false)) {
                    return (string) key.GetValue(SubKeyName);
                }
            }
        }

        private static string FindGameInLibraries(string baseDir) {

            // Look into steam apps directory.
            // Ahh combine.
            // https://stackoverflow.com/questions/961704/how-do-i-join-two-paths-in-c
            string steamAppsPath = Path.Combine(baseDir, CommonConsts.SteamAppsDirectoryName);

            // Check the default library.
            string gamePath = FindGameInLibrary(steamAppsPath);
            if (gamePath == null) {
                string confPath = Path.Combine(steamAppsPath, CommonConsts.SteamLibraryConfigVdfFileName);
                string confTxt = File.ReadAllText(confPath);
                var conf = AcfFile.ParseString(confTxt, true);
                var infoNode = conf.GetNodeByPath(CommonConsts.SteamLibraryConfigVdfFileRootNode);
                foreach (var node in (infoNode as AcfFile.CompoundNode).Value) {
                    //if (!(node is AcfFile.LeafNode)) continue;
                    try {
                        int.Parse(node.Key);
                        var libPath = (node as AcfFile.LeafNode)?.Value;
                        if (libPath == null) {
                            var comNode = node as AcfFile.CompoundNode;
                            if ((comNode.GetChild("apps") as AcfFile.CompoundNode)?.GetChild("550") == null)
                                continue;
                            libPath = (comNode.GetChild("path") as AcfFile.LeafNode).Value;
                        }
                        gamePath = FindGameInLibrary(
                            Path.Combine(libPath, CommonConsts.SteamAppsDirectoryName)
                        );
                        if (gamePath != null) {
                            break;
                        }
                    } catch (System.Exception) { }
                }
            }

            return gamePath;
        }

        private static string FindGameInLibrary(string libraryPath) {
            const string FileAcfL4d2Name = "appmanifest_550.acf";
            string acfPath = Path.Combine(libraryPath, FileAcfL4d2Name);
            if (File.Exists(acfPath)) {
                string text = File.ReadAllText(acfPath);
                var file = AcfFile.ParseString(text, true);
                return Path.Combine(
                    libraryPath, "common",
                    file.GetValueOfPath("AppState", "installdir"));
            }
            return null;
        }

        private static bool ValidateGameDir(string path) {
            try {
                foreach (string testName in new string[] {
                CommonConsts.L4d2ExeFileName, CommonConsts.L4d2IcoFileName
            }) {
                    string testPath = Path.Combine(path, testName);
                    if (!File.Exists(testPath)) {
                        Debug.WriteLine(string.Format("ERROR: Test path {0} does not exist.", testPath));
                        return false;
                    }
                }
            } catch (System.Exception) {
                return false;
            }
            return true;
        }

        public class Exception : System.Exception {

            public Reason TheReason {
                get;
            }

            public Exception(Reason reason) => TheReason = reason;

            public enum Reason {
                RegistryAccessFailed,
                SteamPathNotFoundInRegistry,
                SteamFileNotFound,
                SteamFileNotParsed,
                GamePathNotFound,
                GamePathBroken
            }

        }
    }
}
