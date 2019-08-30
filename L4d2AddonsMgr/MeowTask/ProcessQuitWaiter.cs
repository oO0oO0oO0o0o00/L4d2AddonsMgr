using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using L4d2AddonsMgr.Utils;

namespace L4d2AddonsMgr.MeowTaskSpace {

    internal class ProcessQuitWaiter {

        public static Process GetRunningProcessOfPath(string path)
            => GetRunningProcessOfPathAndName(path, Path.GetFileNameWithoutExtension(path));

        public static Process GetRunningProcessOfPathAndName(string path, string name) {
            var list = Process.GetProcessesByName(name);
            if (list == null) return null;
            Process target = null;
            Process ambiTarget = null;
            foreach (var proc in list) {
                var dispose = true;
                if (target == null)
                    try {
                        var fname = proc.GetMainModuleFileName();
                        if (path == fname
                            || FileInodeComparer.IsSameFile(path, fname)) {
                            target = proc;
                            dispose = false;
                        }
                    } catch (Win32Exception e) {
                        Debug.WriteLine(e);
                        if (ambiTarget == null) ambiTarget = proc;
                    }
                if (dispose) proc.Dispose();
            }
            return target ?? ambiTarget;
        }

        // https://stackoverflow.com/questions/262280/how-can-i-know-if-a-process-is-running
        public static bool WaitForProcessQuit(string path, string waitText)
            => WaitForProcessQuit(path, Path.GetFileNameWithoutExtension(path), waitText);

        public static bool WaitForProcessQuit(string path, string name, string waitText) {
            var target = GetRunningProcessOfPathAndName(path, name);
            try {
                if (target == null || target.HasExited) return true;
            } catch (Win32Exception e) { Debug.WriteLine(e); }
            var dia = new WaitForProcessQuitDialog(waitText, target);
            target.Dispose();
            return dia.ShowDialog() ?? false;
        }
    }
}
