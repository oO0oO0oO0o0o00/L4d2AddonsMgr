using System;
using System.Collections.Generic;

namespace L4d2AddonsMgr.AcfFileSpace {

    internal partial class AcfFile {

        public class CompoundNode : Node<List<Node>> {

            public CompoundNode(string key, CompoundNode parent)
                : base(key, new List<Node>(), parent) {
            }

            public void Add(Node node) {
                Value.Add(node);
            }

            public Node GetChild(string key) {
                foreach (var child in Value) {
                    if (String.Equals(child.Key, key)) return child;
                }
                return null;
            }
        }
    }
}
