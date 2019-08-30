using System;

namespace L4d2AddonsMgr.AcfFileSpace {

    internal partial class AcfFile {

        public class LeafNode : Node<String> {

            public bool IsValueNaked { get; set; }

            public bool IsComment { get; set; }

            public LeafNode(string key, string value, CompoundNode parent)
                : base(key, value, parent) {
            }
        }
    }
}
