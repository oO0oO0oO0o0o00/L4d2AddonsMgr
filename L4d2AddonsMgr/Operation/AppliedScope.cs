using System.ComponentModel;

namespace L4d2AddonsMgr.OperationSpace {

    public class AppliedScope : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        public AppliedScope(Scope scope) {
            this.MyScope = scope;
            OnPropertyChanged(nameof(IsScopeSelectedItemsChecked));
            OnPropertyChanged(nameof(IsScopeAllChecked));
        }

        public Scope MyScope { get; private set; }

        public bool IsScopeSelectedItemsChecked {
            get => MyScope == Scope.SelectedItems;
            set {
                if (MyScope != Scope.SelectedItems) {
                    MyScope = Scope.SelectedItems;
                    OnPropertyChanged(nameof(IsScopeSelectedItemsChecked));
                    OnPropertyChanged(nameof(IsScopeAllChecked));
                }
            }
        }

        public bool IsScopeAllChecked {
            get => MyScope == Scope.All;
            set {
                if (MyScope != Scope.All) {
                    MyScope = Scope.All;
                    OnPropertyChanged(nameof(IsScopeSelectedItemsChecked));
                    OnPropertyChanged(nameof(IsScopeAllChecked));
                }
            }
        }

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public enum Scope {
            SelectedItems, All
        }
    }
}
