using System;
using System.Collections;
using System.Collections.Generic;

namespace L4d2AddonsMgr.AddonsLibrarySpace {

    public abstract class AddonsLibrary : IEnumerable<VpkHolder> {

        public abstract IEnumerator<VpkHolder> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

    }
}
