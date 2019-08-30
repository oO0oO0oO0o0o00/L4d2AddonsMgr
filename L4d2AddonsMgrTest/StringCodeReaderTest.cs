using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Text;

namespace L4d2AddonsMgr.Test {

    /*
     * Unit test.
     * https://docs.microsoft.com/en-us/visualstudio/test/getting-started-with-unit-testing?view=vs-2019
     */
    [TestClass]
    public class StringCodeReaderTest {

        [TestMethod]
        public void TestMethod1() {
            Assert.IsTrue(RunTestPair("", ""));
        }

        [TestMethod]
        public void TestMethod2() {
            Assert.IsTrue(RunTestPair("123\n\n", "123\n"));
        }

        [TestMethod]
        public void TestMethod3() {
            Assert.IsTrue(RunTestPair("123\n\n  456", "123\n\n  456"));
        }

        [TestMethod]
        public void TestMethod4() {
            Assert.IsTrue(RunTestPair("123\n\n  456\n", "123\n\n  456"));
        }

        [TestMethod]
        public void TestMethod5() {
            using (var reader = new StringCodeReader("123\n\n 456")) {
                while (true) {
                    int ch = reader.Peek();
                    int chr = reader.Read();
                    Console.Write((ch == chr) ? "1" : String.Format("||{0}|{1}||", ch, chr));
                    Assert.IsTrue(ch == chr);
                    if (ch < 0 || chr < 0) break;
                }
                // Getting output?
                // https://www.codeproject.com/Articles/501610/Getting-Console-Output-Within-A-Unit-Test
                // No, this.
                // https://stackoverflow.com/questions/4786884/how-to-write-output-from-a-unit-test#comment100863043_13532856
                Console.WriteLine("");
            }
        }

        private static bool RunTestPair(string input, string expected) {
            var sb = new StringBuilder();
            using (var reader = new StringCodeReader(input)) {
                int ch;
                while (true) {
                    ch = reader.Read();
                    if (ch < 0) break;
                    sb.Append((char)ch);
                }
            }

            string got = sb.ToString();
            bool ok = String.Equals(expected, got);

            Trace.WriteLine(ok ? "Test passed:" : "Test failed:\n");
            Console.WriteLine("Expecting");
            Console.WriteLine(expected);
            Console.WriteLine("\nGot");
            Console.WriteLine(got.Replace("\n", "[\\n]\n"));
            Console.WriteLine("\n\n");

            return ok;
        }
    }
}
