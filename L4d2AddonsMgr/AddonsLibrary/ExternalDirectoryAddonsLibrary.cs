using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace L4d2AddonsMgr.AddonsLibrarySpace {

    public class ExternalDirectoryAddonsLibrary : AddonsLibrary {

        private readonly string path;

        public ExternalDirectoryAddonsLibrary(string path) => this.path = path;

        public override IEnumerator<VpkHolder> GetEnumerator() => new Enumerator(path);

        private class Enumerator : IEnumerator<VpkHolder> {

            private readonly IEnumerator<FileInfo> inner;

            public Enumerator(string path)
                => inner = new DirectoryInfo(path).EnumerateFiles(CommonConsts.VpkFileSearchPattern).GetEnumerator();

            public VpkHolder Current => new VpkHolder(inner.Current, VpkHolder.VpkDirType.External);

            object IEnumerator.Current => throw new System.NotImplementedException();

            public void Dispose() => inner.Dispose();
            public bool MoveNext() => inner.MoveNext();
            public void Reset() => inner.Reset();
        }
    }
}
