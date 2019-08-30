using System;

namespace L4d2AddonsMgr.VpkSpace {

    internal partial class Vpk {

        /* Well just Exception not RuntimeException.
         * https://docs.microsoft.com/en-us/dotnet/standard/exceptions/how-to-create-user-defined-exceptions
         * 
         * vs poor autocomplete.
         * https://docs.microsoft.com/en-us/visualstudio/ide/reference/generate-constructor?view=vs-2019 
         */
        class VpkException : Exception {

            public ExceptionReason Reason { get; }

            public VpkException(ExceptionReason reason) : base() {
                Reason = reason;
            }

            public enum ExceptionReason {
                WrongHeaderMarker,
                UnsupportedVersion,
                UnknownVersion,
                WrongHash,
                Unknown
            }
        }
    }
}
