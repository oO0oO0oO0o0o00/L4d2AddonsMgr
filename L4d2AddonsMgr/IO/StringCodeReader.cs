using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace L4d2AddonsMgr {

    /*
     * Character stream designed for Acf file parsing.
     * 
     * Use string reader.
     * https://stackoverflow.com/questions/24713509/character-stream-processing-in-c-sharp
     * 
     * Eof?
     * https://stackoverflow.com/questions/2425863/what-is-character-for-end-of-file-of-filestream
     * https://social.msdn.microsoft.com/Forums/en-US/48b6ee62-f238-4b7e-92cc-11739795d76b/stringreader-eof?forum=vbgeneral 
     */
    public class StringCodeReader : StringReader {

        private const int EOF = -1;

        private const int NewLine = '\n';

        public int LineNo { get; private set; }

        public int Col { get; private set; }

        private bool shallAdjustPos;

        private StringReader lineReader;

        public StringCodeReader(string s) : base(s) {
            LineNo = 0;
            ResetCol();
            ReadALine();
        }

        public override int Read() {

            // If no more lines available just return EOF.
            if (lineReader == null) return EOF;

            // If now reading the first character of a line.
            // Adjust position according to previously set flag.
            if (shallAdjustPos) AdjustPos();

            // Read from current line.
            int ret = lineReader.Read();

            //  No more characters in this line.
            if (ret < 0) {

                ReadALine();

                // If there's no more lines, return EOF.
                if (lineReader == null) return EOF;

                // in order to correctly report new line position,
                // reset positions (lineNo++, col=1) in the next read.
                shallAdjustPos = true;
                return NewLine;
            }

            Col++;
            return ret;
        }

        public override int Peek() {
            if (lineReader == null) return EOF;
            int ch = lineReader.Peek();
            if (ch < 0) return base.Peek() == -1 ? EOF : NewLine;
            return ch;
        }

        private void ResetCol() {
            Col = 1;
            shallAdjustPos = false;
        }

        private void AdjustPos() {
            LineNo++;
            ResetCol();
        }

        private void ReadALine() {
            // Would base.ReadLine call base.Read which is obligated
            // or this.Read according to object's actual type which would bring trouble?
            // How it works.
            // https://www.geeksforgeeks.org/virtual-function-cpp/
            // disassemble a java test program by javap. Same would apply to all managed code languages and cpp late binding.
            // https://stackoverflow.com/questions/13764238/why-invokespecial-is-needed-when-invokevirtual-exists
            var line = base.ReadLine();
            if (line == null) lineReader = null;
            else lineReader = new StringReader(line);
        }
    }
}
