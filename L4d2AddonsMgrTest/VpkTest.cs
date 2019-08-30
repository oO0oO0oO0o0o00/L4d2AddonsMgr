using System;
using System.IO;
using L4d2AddonsMgr.VpkSpace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace L4d2AddonsMgr.Test {

    [TestClass]
    public class VpkTest {

        [TestMethod]
        public void TestMethod1() {
            var vpk = Vpk.OpenFile(new FileInfo(@"E:\addons\blight_path_ls.vpk"));
            var arr = vpk.GetContainedFileBytes("", "addoninfo", "txt");
            File.WriteAllBytes(@"E:\fuck.txt", arr);
        }

        [TestMethod]
        public void TestMethod2() {
            var holder = new VpkHolder(new FileInfo(@"E:\addons\BlueFire.vpk"), VpkHolder.VpkDirType.AddonsDir);
            Console.WriteLine(holder.Name);
        }
    }
}
