using System;
using System.Diagnostics;

namespace L4d2AddonsMgr.AcfFileSpace {

    internal partial class AcfFile {

        public abstract class ParserException : Exception {

            public int Line { get; }

            public int Col { get; }

            public string Expecting { get; }

            public ParserException(int line, int col, string expecting) {
                Line = line;
                Col = col;
                Expecting = expecting;
            }

            // https://stackoverflow.com/questions/3788605/if-debug-vs-conditionaldebug
            [Conditional("DEBUG")]
            public abstract void LogErrorString();

        }
    }
}
