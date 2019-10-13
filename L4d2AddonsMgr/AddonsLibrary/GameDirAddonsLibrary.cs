using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace L4d2AddonsMgr.AddonsLibrarySpace {

    public class GameDirAddonsLibrary : AddonsLibrary {

        private readonly string gameDir;

        private readonly AddonsListTxt listTxt;

        public GameDirAddonsLibrary(string gameDir, AddonsListTxt listTxt) {
            this.gameDir = gameDir;
            this.listTxt = listTxt;
        }

        public override IEnumerator<VpkHolder> GetEnumerator() => new Enumerator(
            Path.Combine(gameDir, CommonConsts.L4d2MainSubdirName, CommonConsts.AddonsDirectoryName),
            listTxt);

        private class Enumerator : IEnumerator<VpkHolder> {

            private bool isEnumWorkshop;

            private readonly string basePath;

            private readonly AddonsListTxt listTxt;

            private IEnumerator<FileInfo> currentEnum;

            public Enumerator(string basePath, AddonsListTxt listTxt) {
                this.basePath = basePath;
                this.listTxt = listTxt;
                Reset();
            }

            public VpkHolder Current {
                get {
                    var item = new VpkHolder(currentEnum.Current,
                        isEnumWorkshop ? VpkHolder.VpkDirType.WorkShopDir : VpkHolder.VpkDirType.AddonsDir);
                    item.IsEnabled = listTxt != null ? listTxt.IsAddonEnabledInList(item.FileDispName) : true;
                    return item;
                }
            }

            object IEnumerator.Current => throw new NotImplementedException();

            public void Dispose() => currentEnum.Dispose();

            public bool MoveNext() {
                bool res = currentEnum.MoveNext();
                if (!res) {
                    if (isEnumWorkshop) return res;
                    currentEnum.Dispose();
                    // Enumerate than list.
                    // https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles?view=netframework-4.8
                    currentEnum = new DirectoryInfo(
                        Path.Combine(basePath, CommonConsts.AddonsWorkshopDirectoryName)
                    ).EnumerateFiles(CommonConsts.VpkFileSearchPattern).GetEnumerator();
                    isEnumWorkshop = true;
                    return currentEnum.MoveNext();
                }
                return res;
            }
            public void Reset() {
                isEnumWorkshop = false;
                if (currentEnum != null) currentEnum.Dispose();
                currentEnum = new DirectoryInfo(basePath).EnumerateFiles(CommonConsts.VpkFileSearchPattern).GetEnumerator();
            }
        }
    }
}
