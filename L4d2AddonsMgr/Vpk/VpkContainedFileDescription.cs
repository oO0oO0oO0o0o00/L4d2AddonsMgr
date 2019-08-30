namespace L4d2AddonsMgr.VpkSpace {

    internal partial class Vpk {

        /*
         * Struct or class.
         * https://stackoverflow.com/questions/945664/can-structs-contain-fields-of-reference-types
         * https://stackoverflow.com/questions/4224074/when-should-a-tree-structure-be-class-struct
         */
        public class VpkContainedFileDescription {

            public string Name { get; }

            public ContainedFileMeta meta;

            public byte[] preload;

            public VpkContainedFileDescription(string name) {
                Name = name;
            }
        }
    }
}
