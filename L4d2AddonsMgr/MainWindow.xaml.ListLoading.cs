using System;
using System.IO;

namespace L4d2AddonsMgr {
    public partial class MainWindow {

        private void ReloadAddonList() {
            try {
                addonListTxt = new AddonListTxt(gameDir);
            } catch (AddonListTxt.LoadingException) {
                ShowMsgAboutAddonListProblem();
            }
        }

        private void RefreshAddonsFromFile() {
            Dispatcher.Invoke(() => Addons.Clear());
            // Enumerate than list.
            // https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles?view=netframework-4.8
            var files = new DirectoryInfo(
                // Ahh combine.
                // https://stackoverflow.com/questions/961704/how-do-i-join-two-paths-in-c
                Path.Combine(gameDir, CommonConsts.L4d2MainSubdirName, CommonConsts.AddonsDirectoryName)).
                EnumerateFiles(CommonConsts.VpkFileSearchPattern);
            foreach (var item in files) {
                var holder = new VpkHolder(item, VpkHolder.VpkDirType.AddonsDir);
                holder.IsEnabled = addonListTxt != null ? addonListTxt.IsAddonEnabledInList(holder.FileDispName) : true;
                Dispatcher.Invoke(new Action(() => Addons.Add(holder)));
            }
            var ws = new DirectoryInfo(Path.Combine(gameDir, CommonConsts.L4d2MainSubdirName, CommonConsts.AddonsDirectoryName, "workshop"));
            if (ws.Exists) foreach (var item in ws.EnumerateFiles(CommonConsts.VpkFileSearchPattern)) {
                    var holder = new VpkHolder(item, VpkHolder.VpkDirType.WorkShopDir);
                    holder.IsEnabled = addonListTxt != null ? addonListTxt.IsAddonEnabledInList(holder.FileDispName) : true;
                    Dispatcher.Invoke(new Action(() => Addons.Add(holder)));
                }
        }
    }
}
