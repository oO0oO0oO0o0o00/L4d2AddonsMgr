using System;

namespace L4d2AddonsMgr.AcfFileSpace {

    internal partial class AcfFile {

        /*
         * Not really relavant.
         * https://stackoverflow.com/questions/2542297/what-would-be-the-use-of-accepting-itself-as-type-arguments-in-generics
         * 
         * Can it...?
         * https://stackoverflow.com/questions/116830/is-there-an-anonymous-generic-tag-in-c-like-in-java
         * Seems not. Java's generic is just edit-time sugar.
         * C#'s seems more "real" so something cannot be done.
         * https://stackoverflow.com/questions/9515128/is-there-a-way-to-deal-with-unknown-generic-types
         */
        public abstract class Node<T> : Node {

            public T Value { get; set; }

            private WeakReference<CompoundNode> parent;

            public CompoundNode Parent {
                get {
                    if (parent.TryGetTarget(out CompoundNode node)) return node;
                    return null;
                }
            }

            public Node(string key, T value, CompoundNode parent) : base(key) {
                Value = value;
                this.parent = new WeakReference<CompoundNode>(parent);
            }

        }

        public abstract class Node {

            public bool IsKeyNaked { get; set; }

            public string Key { get; }

            public Node(string key) {
                Key = key;
            }
        }
    }
}
