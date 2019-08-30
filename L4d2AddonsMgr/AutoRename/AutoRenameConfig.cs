using System.ComponentModel;

namespace L4d2AddonsMgr.AutoRenameSpace {

    public class AutoRenameConfig : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        private RenameDirection _direction;
        private ItfSourcePreference _myItfSourcePreference;
        private bool _itfSkipIfFnContainsCjk;
        private bool _itfFnAddMapOrModPrefix;
        private bool _itfSkipIfFnHasThosePrefises;
        private bool _itfKeepAsSuffixForNumericalFn;
        private bool _ftiInclAddonName;
        private bool _ftiInclMissionName;
        private bool _ftiOnlyForFailedNames;
        private bool _ftiTryRecoverFailedNameInsteadFirst;

        public AutoRenameConfig() {
            Direction = RenameDirection.InternalToFile;
            ItfMySourcePreference = ItfSourcePreference.Mission;
            ItfSkipIfFnContainsCjk = true;
            ItfSkipIfFnHasThosePrefises = true;
            ItfKeepAsSuffixForNumericalFn = true;
            FtiInclAddonName = true;
            FtiOnlyForFailedNames = true;
            FtiTryRecoverFailedNameInsteadFirst = true;
        }

        public RenameDirection Direction {
            get => _direction;
            set {
                if (_direction != value) {
                    _direction = value;
                    OnPropertyChanged(nameof(Direction));
                }
            }
        }

        public ItfSourcePreference ItfMySourcePreference {
            get => _myItfSourcePreference; set {
                if (_myItfSourcePreference != value) {
                    _myItfSourcePreference = value;
                    OnPropertyChanged(nameof(ItfMySourcePreference));
                }
            }
        }

        public bool ItfSkipIfFnContainsCjk {
            get => _itfSkipIfFnContainsCjk; set {
                if (_itfSkipIfFnContainsCjk != value) {
                    _itfSkipIfFnContainsCjk = value;
                    OnPropertyChanged(nameof(ItfSkipIfFnContainsCjk));
                }
            }
        }

        public bool ItfFnAddMapOrModPrefix {
            get => _itfFnAddMapOrModPrefix; set {
                if (_itfFnAddMapOrModPrefix != value) {
                    _itfFnAddMapOrModPrefix = value;
                    OnPropertyChanged(nameof(ItfFnAddMapOrModPrefix));
                }
            }
        }

        public bool ItfSkipIfFnHasThosePrefises {
            get => _itfSkipIfFnHasThosePrefises; set {
                if (_itfSkipIfFnHasThosePrefises != value) {
                    _itfSkipIfFnHasThosePrefises = value;
                    OnPropertyChanged(nameof(ItfSkipIfFnHasThosePrefises));
                }
            }
        }

        public bool ItfKeepAsSuffixForNumericalFn {
            get => _itfKeepAsSuffixForNumericalFn; set {
                if (_itfKeepAsSuffixForNumericalFn != value) {
                    _itfKeepAsSuffixForNumericalFn = value;
                    OnPropertyChanged(nameof(ItfKeepAsSuffixForNumericalFn));
                }
            }
        }

        public bool FtiInclAddonName {
            get => _ftiInclAddonName; set {
                if (_ftiInclAddonName != value) {
                    _ftiInclAddonName = value;
                    OnPropertyChanged(nameof(FtiInclAddonName));
                }
            }
        }

        public bool FtiInclMissionName {
            get => _ftiInclMissionName; set {
                if (_ftiInclMissionName != value) {
                    _ftiInclMissionName = value;
                    OnPropertyChanged(nameof(FtiInclMissionName));
                }
            }
        }

        public bool FtiOnlyForFailedNames {
            get => _ftiOnlyForFailedNames; set {
                if (_ftiOnlyForFailedNames != value) {
                    _ftiOnlyForFailedNames = value;
                    OnPropertyChanged(nameof(FtiOnlyForFailedNames));
                }
            }
        }

        public bool FtiTryRecoverFailedNameInsteadFirst {
            get => _ftiTryRecoverFailedNameInsteadFirst; set {
                if (_ftiTryRecoverFailedNameInsteadFirst != value) {
                    _ftiTryRecoverFailedNameInsteadFirst = value;
                    OnPropertyChanged(nameof(FtiTryRecoverFailedNameInsteadFirst));
                }
            }
        }

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public enum RenameDirection {
            Null, InternalToFile, FileToInternal
        }

        public enum ItfSourcePreference {
            Null, Addon, Mission
        }

    }
}
