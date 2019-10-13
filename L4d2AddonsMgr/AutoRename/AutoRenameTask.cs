using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using L4d2AddonsMgr.MeowTaskSpace;
using L4d2AddonsMgr.Utils;

namespace L4d2AddonsMgr.AutoRenameSpace {

    public class AutoRenameTask : MeowTask {

        // TODO: Move button size into style as default. Continue with error and cancel-confirm dialogs.

        private static Regex doRename_endWithUnderlineAndNumbersRegex = new Regex("_[0-9]+$");

        private static Regex doRename_NumbersRegex = new Regex("^[0-9]+$");

        private readonly string gameDir;

        private readonly AddonsListTxt listTxt;

        public List<VpkHolder> list;

        public AutoRenameConfig cfg;

        public AutoRenameTask(List<VpkHolder> list, AutoRenameConfig cfg, string gameDir, AddonsListTxt listTxt)
             : base("自动重命名") {
            this.list = list;
            this.cfg = cfg;
            this.gameDir = gameDir;
            this.listTxt = listTxt;
            CancelPromptText = "要中止操作吗？部分重命名可能已经进行，此时中止并不会撤销已发生的更改。";
            MaxProgress = 10;// list.Count;
            Text = "请稍等...";
        }

        public override void Do() {
            Action<VpkHolder, AutoRenameConfig> func;
            switch (cfg.Direction) {
            case AutoRenameConfig.RenameDirection.InternalToFile:
                func = DoRename_InternalNameToFileName;
                break;
            case AutoRenameConfig.RenameDirection.FileToInternal:
                MessageBox.Show("暂不支持。", "一个Boomer在你面前爆炸了");
                return;
            //func = DoRename_FileNameToInternalName;
            //break;
            default: return;
            }
            foreach (var item in list) {
retry:
                bool cancel;
                mutex.WaitOne();
                cancel = isCancelled;
                if (!cancel) {
                    bool gameNotRunning = true;
                    Application.Current.Dispatcher.Invoke(()
                        => gameNotRunning = ProcessQuitWaiter.WaitForProcessQuit(
                           Path.Combine(gameDir, CommonConsts.L4d2ExeFileName),
                         "正在等待Left 4 Dead 2退出。游戏运行期间进行此操作可能导致问题。" +
                         "当您结束游戏并退出时，此操作会自动继续。"));
                    if (!gameNotRunning) {
                        cancel = true;
                        isCancelled = true;
                    }
                }
                mutex.ReleaseMutex();
                if (cancel) break;
                try {
                    func(item, cfg);
                } catch (Exception e) {
                    Debug.WriteLine(e);
                    mutex.WaitOne();
                    var resFail = ItemFailureDialog.Result.Abort;
                    if (isCancelled) cancel = true;
                    else {
                        Application.Current.Dispatcher.Invoke(() => {
                            var diaFail = new ItemFailureDialog(string.Format(
                                "对附加组件{0}的操作失败了，这可能因为文件被其他程序占用" +
                                "（游戏本身或GCFScape等）、文件损坏" +
                                "或本应用程序存在的问题。您可以尝试排除问题，然后重试。", "xxx"));
                            bool? resBoolFail = diaFail.ShowDialog();
                            if (resBoolFail != true) resFail = ItemFailureDialog.Result.Abort;
                            else resFail = diaFail.GetResult();
                        });
                        if (resFail == ItemFailureDialog.Result.Abort) {
                            cancel = true;
                            isCancelled = true;
                        }
                    }
                    mutex.ReleaseMutex();
                    if (cancel || isCancelled) break;
                    if (resFail == ItemFailureDialog.Result.Retry) goto retry;
                }
                Application.Current.Dispatcher.Invoke(() => CurrentProgress++);
            }
            listTxt?.SaveToFile();
        }

        private void DoRename_InternalNameToFileName(VpkHolder item, AutoRenameConfig cfg) {
            string name;
            // Skip workshop items by default.
            if (item.vpkDir == VpkHolder.VpkDirType.WorkShopDir) return;
            // Retrieve old file name.
            string oldFn = Path.GetFileNameWithoutExtension(item.FileInf.Name);
            // Skip items having "those" prefises and suffises, or not.
            if (cfg.ItfSkipIfFnHasThosePrefises &&
                (oldFn.StartsWith(CommonConsts.RenameMapPrefix)
                || oldFn.StartsWith(CommonConsts.RenameModPrefix)
                || doRename_endWithUnderlineAndNumbersRegex.IsMatch(oldFn))) return;
            // Skip items having Chinese/Japanese/Korean characters in their file names, or not.
            if (cfg.ItfSkipIfFnContainsCjk && LocalizationUtil.HasCjkCharacter(oldFn)) return;
            // Attempt to read display name in the order defines by user.
            switch (cfg.ItfMySourcePreference) {
            case AutoRenameConfig.ItfSourcePreference.Addon:
                name = item.AddonTitle;
                if (DoRename_IsInvalidName(name)) name = item.MissionTitle;
                break;
            case AutoRenameConfig.ItfSourcePreference.Mission:
                name = item.MissionTitle;
                if (DoRename_IsInvalidName(name)) name = item.AddonTitle;
                break;
            default:
                return;
            }
            // Skip if both approaches to read these names have failed.
            if (DoRename_IsInvalidName(name)) return;
            // Replace invalid characters for file names and format string (it does not matter, right).
            foreach (char ch in Path.GetInvalidFileNameChars()) name = name.Replace(ch, '_');
            name = name.Replace('{', '(').Replace('}', ')');
            // Add prefises, or not.
            if (cfg.ItfFnAddMapOrModPrefix)
                name = item.HasMission ?
                    CommonConsts.RenameMapPrefix + name : CommonConsts.RenameModPrefix + name;
            // We will be adding numbers in case of name conflicts, e.g. xxx (1).vpk, xxx (3)_2333.vpk.
            string counterPattern;
            // For numeric file names we keep it as suffix (and counter num in the middle), or not.
            if (cfg.ItfKeepAsSuffixForNumericalFn && doRename_NumbersRegex.IsMatch(oldFn)) {
                name = name + "{0}" + '_' + oldFn;
                counterPattern = "({0})";
            } else {
                name += "{0}";
                counterPattern = " ({0})";
            }
            name += ".vpk";
            // Perform the actual rename.
            name = DoRename_MoveWithoutConflicts(item.FileInf, name, counterPattern);
            oldFn += ".vpk";
            // Anything changed.
            if (oldFn != string.Format(name, string.Empty)) {
                item.UpdateFileName();
                // Preserve disabled state. Enabled is default.
                if (item.vpkDir == VpkHolder.VpkDirType.AddonsDir && !item.IsEnabled) {
                    listTxt?.ToggleAddonEnabledState(name, false);
                    // Actually not good practice.
                    listTxt?.RemoveAddonEnabledState(oldFn);
                }
                Debug.WriteLine(string.Format("Renamed {0} to {1}", oldFn, name));
            }
        }

        private void DoRename_FileNameToInternalName(VpkHolder item, AutoRenameConfig cfg) {
        }

        private bool DoRename_IsInvalidName(string name) {
            return string.IsNullOrEmpty(name);
        }

        private string DoRename_MoveWithoutConflicts(FileInfo src, string namePattern, string counterPattern) {
            string dirName = src.DirectoryName;
            string resultedName;
            try {
                resultedName = string.Format(namePattern, string.Empty);
                src.MoveTo(Path.Combine(dirName, resultedName));
                return resultedName;
            } catch (IOException ioe) {
                if (!DoRename_IoExceptionIsFileExists(ioe)) throw ioe;
            }
            for (int i = 1; i < 21; i++) {
                try {
                    resultedName = string.Format(namePattern, string.Format(counterPattern, i));
                    src.MoveTo(Path.Combine(dirName, resultedName));
                    return resultedName;
                } catch (IOException ioe) {
                    if (!DoRename_IoExceptionIsFileExists(ioe)) throw ioe;
                }
            }
            var rand = new Random();
            for (int i = 0; i < 20; i++) {
                try {
                    resultedName = string.Format(namePattern, string.Format(
                        namePattern, string.Format(counterPattern, rand.Next())));
                    src.MoveTo(Path.Combine(dirName, resultedName));
                    return resultedName;
                } catch (IOException ioe) {
                    if (!DoRename_IoExceptionIsFileExists(ioe)) throw ioe;
                }
            }
            // Your extreme luck.
            throw new Exception("你是不是中了巫术，同名文件1到20号都已存在所以无法使用，又试了20个随机数也不行。");
        }

        // using only lower half of HResult:
        // https://stackoverflow.com/questions/425956/how-do-i-determine-if-an-ioexception-is-thrown-because-of-a-sharing-violation
        private bool DoRename_IoExceptionIsFileExists(IOException ioe) => (ioe.HResult & 0xffff) == 0xb7;
    }
}
