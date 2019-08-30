namespace L4d2AddonsMgr.VpkSpace {

    internal partial class Vpk {

        struct VpkHeader {
            public const uint HeaderMarker = 0x55aa1234;
            public int version;
            public int directorySize;
            public int embeddedChunkSize;
            public int chunkHashesSize;
            public int selfHashesSize;
            public int signatureSize;
            public int headerSize;
        }
    }
}
