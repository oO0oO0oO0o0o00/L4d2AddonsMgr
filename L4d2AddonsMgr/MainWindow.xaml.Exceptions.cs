using System;

namespace L4d2AddonsMgr {

    public partial class MainWindow {

        private class AddonsLoadingException : Exception {

            public ExceptionReason Reason { get; }

            public AddonsLoadingException(ExceptionReason reason) {
                Reason = reason;
            }

            public enum ExceptionReason {
                RegistryAccessFailed,
                SteamPathNotFoundInRegistry,
                SteamFileNotFound,
                SteamFileNotParsed,
                GamePathNotFound,
                GamePathBroken
            }

        }
    }
}
