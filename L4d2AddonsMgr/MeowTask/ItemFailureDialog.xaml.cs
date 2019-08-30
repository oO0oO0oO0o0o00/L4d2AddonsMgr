using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace L4d2AddonsMgr.MeowTaskSpace {
    /// <summary>
    /// ItemFailureDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ItemFailureDialog : Window {

        private Result myResult;

        public string Text { get; private set; }

        public ItemFailureDialog(string text) {
            InitializeComponent();
            Text = text;
            DataContext = this;
            Dispatcher.BeginInvoke(new Action(() => PartRetryButton.Focus()));
            AddHandler(Window.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(OnKeyDownPreview));
        }

        private void OnKeyDownPreview(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.Enter && FocusManager.GetFocusedElement(this) is Button btn) {
                btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
            }
        }

        private void ButtonRetry_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            myResult = Result.Retry;
            Close();
        }

        private void ButtonSkip_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            myResult = Result.Skip;
            Close();
        }

        public Result GetResult() => myResult;

        public enum Result { Retry, Skip, Abort }
    }
}
