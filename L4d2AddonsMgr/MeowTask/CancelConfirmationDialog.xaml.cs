using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace L4d2AddonsMgr.MeowTaskSpace {
    /// <summary>
    /// CancelConfirmationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CancelConfirmationDialog : Window {

        public string ContentText { get; private set; }

        public CancelConfirmationDialog(string text) {
            InitializeComponent();
            ContentText = text;
            DataContext = this;
            Dispatcher.BeginInvoke(new Action(() => PartConfirmButton.Focus()));
            AddHandler(Window.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(OnKeyDownPreview));
        }

        private void OnKeyDownPreview(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.Enter && FocusManager.GetFocusedElement(this) is Button btn) {
                if (btn.IsCancel) Close();
                else btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }
    }
}
