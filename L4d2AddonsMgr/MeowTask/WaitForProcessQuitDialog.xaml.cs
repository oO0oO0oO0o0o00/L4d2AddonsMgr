using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace L4d2AddonsMgr.MeowTaskSpace {
    /// <summary>
    /// WaitForProcessQuitDialog.xaml 的交互逻辑
    /// </summary>
    public partial class WaitForProcessQuitDialog : Window {

        private Thread waitingThread;

        private readonly Process waitForThisGuy;

        public string Text { get; private set; }

        public WaitForProcessQuitDialog(string text, Process waitForThisGay) {
            InitializeComponent();
            Text = text;
            waitForThisGuy = waitForThisGay;
            Dispatcher.BeginInvoke(new Action(() => PartCancelButton.Focus()));
            Closing += Window_Closing;
            DataContext = this;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e) {
            var result = false;
            await Task.Run(() => {
                try {
                    waitingThread = Thread.CurrentThread;
                    waitForThisGuy.WaitForExit();
                    result = true;
                } catch (ThreadInterruptedException) { } finally { waitingThread = null; }
            });
            Closing -= Window_Closing;
            DialogResult = result;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            waitingThread?.Interrupt();
            e.Cancel = true;
        }
    }
}
