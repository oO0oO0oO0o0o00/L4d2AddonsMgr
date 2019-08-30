using System.ComponentModel;
using System.Threading;

namespace L4d2AddonsMgr.MeowTaskSpace {

    public abstract class MeowTask : IMeowTask, INotifyPropertyChanged {

        private double _maxProgress;
        private double _currentProgress;
        private string _name;
        private string _text;
        private string _cancelPromptText;

        public bool isCancelled;
        public readonly Mutex mutex;

        protected MeowTask(string name) {
            Name = name;
            mutex = new Mutex();
        }

        public double MaxProgress {
            get => _maxProgress; protected set {
                _maxProgress = value;
                OnPropertyChanged(nameof(MaxProgress));
            }
        }

        public double CurrentProgress {
            get => _currentProgress; protected set {
                _currentProgress = value;
                OnPropertyChanged(nameof(CurrentProgress));
            }
        }

        public string Name {
            get => _name; protected set {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Text {
            get => _text; protected set {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public string CancelPromptText {
            get => _cancelPromptText; protected set {
                _cancelPromptText = value;
                OnPropertyChanged(nameof(CancelPromptText));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public abstract void Do();

        public bool RequestCancel() => true;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
