using System;
using System.Diagnostics;

namespace L4d2AddonsMgr.AcfFileSpace {

    internal partial class AcfFile {

        class UnexpectedCharacterException : ParserException {

            public char Got { get; }

            public UnexpectedCharacterException(int line, int col, string expecting, char got)
                : base(line, col, expecting) {
                Got = got;
            }

            public override void LogErrorString() {
                // If the charcater is a control symbol we cannot just print it in this way.
                // https://stackoverflow.com/questions/34328680/print-ascii-char-on-the-console-in-c-sharp
                // https://stackoverflow.com/questions/323640/can-i-convert-a-c-sharp-string-value-to-an-escaped-string-literal
                // Well no neat approach. Forget about it.
                Debug.WriteLine(String.Format(
                    "ERROR: Syntax error at line {0} column {1}, expecting {2}, got {3}",
                    Line, Col, Expecting, Got == 0 ? "EOF" : Got.ToString()
                    ));
            }
        }
    }
}
