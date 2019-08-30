namespace L4d2AddonsMgr.VpkSpace {

    internal partial class Vpk {

        public struct ContainedFileMeta {
            public const ushort Suffix = 0xffff;
            public uint crc32;
            public short preloadLength;
            public short archieveIndex;
            public int archieveOffset;
            public int fileLength;
        }
    }
}
