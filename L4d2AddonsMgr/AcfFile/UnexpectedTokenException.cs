using System;
using System.Diagnostics;

namespace L4d2AddonsMgr.AcfFileSpace {

    internal partial class AcfFile {

        class UnexpectedTokenException : ParserException {

            public string Got { get; }

            public UnexpectedTokenException(int line, int col, string expecting, string got)
                : base(line, col, expecting) {
                Got = got;
            }

            public override void LogErrorString() {
                Debug.WriteLine(String.Format(
                    "ERROR: Unexpected token at line {0} column {1}, expecting {2}, got {3}",
                    Line, Col, Expecting, Got.ToString()
                    ));
            }
        }
    }
}
