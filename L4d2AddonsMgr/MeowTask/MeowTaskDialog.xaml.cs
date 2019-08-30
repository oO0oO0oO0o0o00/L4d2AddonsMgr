using System;
using System.Threading.Tasks;
using System.Windows;

namespace L4d2AddonsMgr.MeowTaskSpace {
    /// <summary>
    /// AutoRenamingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MeowTaskDialog : Window {

        private bool done;
        private MeowTask _myTask;

        public MeowTask MyTask {
            get => _myTask; set {
                if (_myTask == null) {
                    _myTask = value;
                    DataContext = value;
                }
            }
        }

        public MeowTaskDialog() {
            InitializeComponent();
            Dispatcher.BeginInvoke(new Action(() => PartCancelButton.Focus()));
        }

        // Window events sequence:
        // https://wpf.2000things.com/2012/07/30/613-window-event-sequence/
        private async void Window_Loaded(object sender, RoutedEventArgs e) {
            await Task.Run(() => _myTask.Do());
            done = true;
            DialogResult = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            _myTask.mutex.WaitOne();
            if (!(_myTask.isCancelled || done)) {
                var dia = new CancelConfirmationDialog(_myTask.CancelPromptText) {
                    Owner = this
                };
                if (dia.ShowDialog() ?? false) _myTask.isCancelled = true;
                else e.Cancel = true;
            }
            _myTask.mutex.ReleaseMutex();
        }
    }
}
