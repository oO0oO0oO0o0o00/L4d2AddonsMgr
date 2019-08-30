using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using L4d2AddonsMgr.AcfFileSpace;

namespace L4d2AddonsMgr.Test {

    [TestClass]
    public class AcfFile_LexerTest {

        [TestMethod]
        public void TestMethod1() {
            Assert.IsTrue(RunTestPair("", "[EOF]"));
        }

        [TestMethod]
        public void TestMethod2() {
            Assert.IsTrue(RunTestPair("{\"Meow\"", "\"{\"\"Meow\"[EOF]"));
        }

        [TestMethod]
        public void TestMethod3() {
            Assert.IsTrue(RunTestPair("{\"Meow\" \"hitler\"", "\"{\"\"Meow\"\"hitler\"[EOF]"));
        }

        [TestMethod]
        public void TestMethod4() {
            Assert.IsTrue(RunTestPair("{\r\n\"Meow\"\r\n}", "\"{\"\"Meow\"\"}\"[EOF]"));
        }

        [TestMethod]
        public void TestMethod5() {
            Assert.IsTrue(RunTestPair(
                "{\r\n" +
                "\"Meow\"\r\n" +
                "Shit // Y o u\n" +
                "Meow\n" +
                "}",
                "\"{\"" +
                "\"Meow\"" +
                "'Shit'" +
                "\"// Y o u\"" +
                "'Meow'" +
                "\"}\"" +
                "[EOF]"));
        }

        public static bool RunTestPair(string input, string expected) {
            var got = AcfFile.TestLexer(input);
            bool ret = String.Equals(expected, got);
            Console.WriteLine(ret ? "Test passed." : "Test failed.");
            Console.WriteLine(input);
            Console.WriteLine("======");
            Console.WriteLine(got);
            Console.WriteLine("======");
            return ret;
        }
    }
}
