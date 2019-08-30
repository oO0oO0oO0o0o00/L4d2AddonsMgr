using System;
using L4d2AddonsMgr.SearchSpace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace L4d2AddonsMgrTest {

    [TestClass]
    public class SearchNameTest {

        [TestMethod]
        public void TestMethod1() {
            //
        }

        public bool RunTestPair(string src, string exp) {
            Console.WriteLine("Src: ");
            Console.WriteLine(src);
            var got = new SearchName(src).ToString();
            Console.WriteLine("Got: ");
            Console.WriteLine(got);
            var ret = exp == got;
            Console.WriteLine(ret);
            return ret;
        }
    }
}
