using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using L4d2AddonsMgr.AcfFileSpace;
using Microsoft.Win32;

namespace L4d2AddonsMgr {

    public partial class MainWindow {

        private void ShowMsgAboutAddonListProblem() {
            // How would it be known by just guessing.
            // https://www.wpf-tutorial.com/dialogs/the-messagebox/

            // `runOnUiThread` equivelant?
            // https://www.cnblogs.com/oiliu/p/5504424.html
            Dispatcher.Invoke(() => MessageBox.Show(
                "读取附加组件配置文件失败，为避免数据丢失，部分功能将不可用。" +
                "建议您检查并更正addonlist.txt中的格式错误，或在线寻求帮助。"));
        }

        private void FindGameDir() {

            const string FolderSteamAppsName = CommonConsts.SteamAppsDirectoryName;

            var gameDirFound = false;

            string path;

            // Find Steam install path from registry.
            try {
                path = LocateSteamDirFromRegistry();
            } catch (Exception e) {
                // Just use "+"! It would ALWAYS be optimized away.
                // https://stackoverflow.com/questions/288794/does-c-sharp-optimize-the-concatenation-of-string-literals

                // LMAO Forget about answers by
                // https://stackoverflow.com/questions/1100260/multiline-string-literal-in-c-sharp
                // https://stackoverflow.com/questions/31764898/long-string-interpolation-lines-in-c6/31766560#31766560
                // Performance art they are doing.
                Debug.WriteLine(
                    "ERROR: Failed locating installed Steam directory using registry.\n" +
                    "Did anything restricted me from accesing the registry or what?\n" +
                    "We were looking into 32-bit's view of the registry.\nDetails:");
                Debug.WriteLine(e.Message);
                throw new AddonsLoadingException(
                    AddonsLoadingException.ExceptionReason.RegistryAccessFailed);
            }
            if (path == null) {
                throw new AddonsLoadingException(
                    AddonsLoadingException.ExceptionReason.SteamPathNotFoundInRegistry);
            }

            // Look into steam apps directory.
            path = Path.Combine(path, FolderSteamAppsName);

            // Check the default library.
            var gamePath = FindGameInLibrary(path);
            if (gamePath == null) {
                var confPath = Path.Combine(path, CommonConsts.SteamLibraryConfigVdfFileName);
                var confTxt = File.ReadAllText(confPath);
                var conf = AcfFileSpace.AcfFile.ParseString(confTxt, true);
                var infoNode = conf.GetNodeByPath(CommonConsts.SteamLibraryConfigVdfFileRootNode);
                foreach (var node in (infoNode as AcfFile.CompoundNode).Value) {
                    if (!(node is AcfFile.LeafNode)) continue;
                    try {
                        int.Parse(node.Key);
                        path = FindGameInLibrary(
                            Path.Combine((node as AcfFile.LeafNode).Value, FolderSteamAppsName)
                        );
                        if (path != null) {
                            gameDirFound = true;
                            break;
                        }
                    } catch (Exception) { }
                }
            } else
                gameDirFound = true;

            if (!gameDirFound)
                throw new AddonsLoadingException(
                    AddonsLoadingException.ExceptionReason.GamePathNotFound);

            foreach (var testName in new string[] {
                CommonConsts.L4d2ExeFileName, CommonConsts.L4d2IcoFileName
            }) {
                var testPath = Path.Combine(path, testName);
                if (!File.Exists(testPath)) {
                    Debug.WriteLine(string.Format("ERROR: Test path {0} does not exist.", testPath));
                    throw new AddonsLoadingException(
                        AddonsLoadingException.ExceptionReason.GamePathBroken);
                }
            }
            gameDir = path;
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
            string path = null;
            const string KeyName = @"SOFTWARE\Valve\Steam";
            const string SubKeyName = "InstallPath";

            using (var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)) {
                using (var key = localMachine.OpenSubKey(KeyName, false)) {
                    path = (string)key.GetValue(SubKeyName);
                }
            }
            return path;
        }

        private static string FindGameInLibrary(string libraryPath) {
            const string FileAcfL4d2Name = "appmanifest_550.acf";
            var acfPath = Path.Combine(libraryPath, FileAcfL4d2Name);
            if (File.Exists(acfPath)) {
                var text = File.ReadAllText(acfPath);
                var file = AcfFileSpace.AcfFile.ParseString(text, true);
                return Path.Combine(
                    libraryPath, "common",
                    file.GetValueOfPath("AppState", "installdir"));
            }
            return null;
        }
    }
}
