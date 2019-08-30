using L4d2AddonsMgr.AcfFileSpace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace L4d2AddonsMgr.Test {

    [TestClass]
    public class AcfFileTest {

        [TestMethod]
        public void TestMethod1() {
            Assert.IsTrue(RunTestPair("\"nmsl\" \"xjp\"", "[nmsl = xjp 00]\n"));
        }

        [TestMethod]
        public void TestMethod2() {
            Assert.IsTrue(RunTestPair("\"a1k\"\n{\n\"b1k\" \"b1v\"\n}\n", "a1k = {\n[b1k = b1v 00]\n}\n"));
        }

        [TestMethod]
        public void TestMethod3() {
            Assert.IsTrue(RunTestPair(
                "\"a1k\"\n" +
                "{\n" +
                "\"b1k\" \"b1v\"\n" +
                "\"b2k\"\t\"b2v\"\n" +
                "}\n",
                "a1k = {\n" +
                "[b1k = b1v 00]\n" +
                "[b2k = b2v 00]\n" +
                "}\n"));
        }

        [TestMethod]
        public void TestMethod4() {
            Assert.IsTrue(RunTestPair(
                "\"a1k\"\n" +
                "{\n" +
                "b1k \"b1v\"\n" +
                "\"b2k\"\t{\n" +
                "  \"c1k\"      c1v\n" +
                "}\n" +
                "}\n",
                "a1k = {\n" +
                "[b1k = b1v 10]\n" +
                "b2k = {\n" +
                "[c1k = c1v 01]\n" +
                "}\n" +
                "}\n"));
        }

        [TestMethod]
        public void TestMethod5() {
            Assert.IsTrue(RunTestPair(
                "\"a1k\" // Ignore this\n" +
                "{\n" +
                "\"b1k\" \"b1v\"\n" +
                "\"b2k\"\t\"b2v\"\n" +
                "// 123 Jump\n" +
                "}\n",
                "[// Ignore this]\n" +
                "a1k = {\n" +
                "[b1k = b1v 00]\n" +
                "[b2k = b2v 00]\n" +
                "[// 123 Jump]\n" +
                "}\n"));
        }

        [TestMethod]
        public void TestMethod6() {

            RunTestPair(
                File.ReadAllText(@"E:\Program Files (x86)\Steam\steamapps\appmanifest_243730.acf"),
                "");
        }

        [TestMethod]
        public void TestMethod7() {
            Console.WriteLine(
                AcfFile.ParseString(File.ReadAllText(@"E:\Program Files (x86)\Steam\steamapps\appmanifest_243730.acf"), false).ToString()
                );
        }

        private static bool RunTestPair(string input, string expected) {
            var file = AcfFile.ParseString(input, false);
            // Use LF.
            // https://stackoverflow.com/questions/4855576/streamwriter-end-line-with-lf-rather-than-crlf
            var writer = new StringWriter {
                NewLine = "\n"
            };
            foreach (var node in file.Root.Value) PrintNodeRecursive(node, writer);
            var got = writer.ToString();
            Console.WriteLine("==========");
            Console.WriteLine(input);
            Console.WriteLine("----------");
            Console.WriteLine(got);
            Console.WriteLine();
            return String.Equals(expected, got);
        }

        private static void PrintNodeRecursive(AcfFile.Node node, StringWriter writer) {
            if (node is AcfFile.LeafNode) {
                var lnode = node as AcfFile.LeafNode;
                if (lnode.IsComment) writer.WriteLine("[//{0}]", lnode.Value);
                else writer.WriteLine("[{0} = {1} {2}{3}]", lnode.Key, lnode.Value, lnode.IsKeyNaked ? 1 : 0, lnode.IsValueNaked ? 1 : 0);
            } else {
                var cnode = node as AcfFile.CompoundNode;
                // How to escape "{".
                // https://stackoverflow.com/questions/91362/how-to-escape-braces-curly-brackets-in-a-format-string-in-net
                writer.WriteLine("{0} = {{", cnode.Key);
                foreach (var child in cnode.Value) {
                    PrintNodeRecursive(child, writer);
                }
                writer.WriteLine("}");
            }
        }

    }
}
