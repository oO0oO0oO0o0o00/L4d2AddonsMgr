using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using L4d2AddonsMgr.OperationSpace;

namespace L4d2AddonsMgr.AutoRenameSpace {
    /// <summary>
    /// AutoRenameDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AutoRenameDialog : Window, INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        public AppliedScope MyAppliedScope { get; }

        private readonly AutoRenameConfig _model;
        private int _selectedItemsCount = -1;

        public int SelectedItemsCount {
            get => _selectedItemsCount;
            set {
                if (_selectedItemsCount != value) {

                    // If first update && single selected, or non selected => Prefer all.
                    if (_selectedItemsCount < 0 && value == 1 || value < 1)
                        MyAppliedScope.IsScopeAllChecked = true;
                    else MyAppliedScope.IsScopeSelectedItemsChecked = true;

                    if (value < 1) {
                        // In this way if user selects one or multiple items the next time we can apply preferred
                        // Selection differently (With non modal dialog).
                        value = -1;
                    }
                    PartAppliedScopeSelectedItemsRadio.IsEnabled = value >= 1;
                    _selectedItemsCount = value;
                }
            }
        }

        public bool IsRadioInternalToFileChecked {
            get => _model.Direction == AutoRenameConfig.RenameDirection.InternalToFile;
            set {
                if (value) _model.Direction = AutoRenameConfig.RenameDirection.InternalToFile;
            }
        }

        public bool IsRadioFileToInternalChecked {
            get => _model.Direction == AutoRenameConfig.RenameDirection.FileToInternal;
            set {
                if (value) _model.Direction = AutoRenameConfig.RenameDirection.FileToInternal;
            }
        }

        public bool IsRadioItfSourcePreferenceMissionChecked {
            get => _model.ItfMySourcePreference == AutoRenameConfig.ItfSourcePreference.Mission;
            set {
                if (value) _model.ItfMySourcePreference = AutoRenameConfig.ItfSourcePreference.Mission;
            }
        }

        public bool IsRadioItfSourcePreferenceAddonChecked {
            get => _model.ItfMySourcePreference == AutoRenameConfig.ItfSourcePreference.Addon;
            set {
                if (value) _model.ItfMySourcePreference = AutoRenameConfig.ItfSourcePreference.Addon;
            }
        }

        public AutoRenameConfig Model => _model;

        public AutoRenameDialog() {
            DataContext = this;

            MyAppliedScope = new AppliedScope(AppliedScope.Scope.SelectedItems);

            _model = new AutoRenameConfig();
            _model.PropertyChanged += Model_PropertyChanged;

            InitializeComponent();

            UpdateMajorOptionBoxesEnabledState();
            Dispatcher.BeginInvoke(new Action(() => PartConfirmButton.Focus()));
            AddHandler(Window.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(AutoRenameDialog_KeyDown));
        }

        private void AutoRenameDialog_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                var ele = FocusManager.GetFocusedElement(PartDlgButtons);
                if (ele is ButtonBase btn) {
                    if (btn.Command != null) {
                        if (btn.Command.CanExecute(null)) btn.Command.Execute(null);
                        // Raise event.
                        // https://stackoverflow.com/questions/728432/how-to-programmatically-click-a-button-in-wpf
                    } else if (btn is Button button && button.IsCancel) Close();
                    else btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    e.Handled = true;
                }
            }
        }

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
            case nameof(AutoRenameConfig.Direction):
                UpdateMajorOptionBoxesEnabledState();
                OnPropertyChanged(nameof(IsRadioInternalToFileChecked));
                OnPropertyChanged(nameof(IsRadioFileToInternalChecked));
                break;
            case nameof(AutoRenameConfig.ItfMySourcePreference):
                OnPropertyChanged(nameof(IsRadioItfSourcePreferenceMissionChecked));
                OnPropertyChanged(nameof(IsRadioItfSourcePreferenceAddonChecked));
                break;
            }
            PartConfirmButton.Focus();
        }

        private void UpdateMajorOptionBoxesEnabledState() {
            switch (_model.Direction) {
            case AutoRenameConfig.RenameDirection.Null:
                PartInternalToFileNamingOptions.IsEnabled = false;
                PartFileToInternalNamingOptions.IsEnabled = false;
                break;
            case AutoRenameConfig.RenameDirection.InternalToFile:
                PartInternalToFileNamingOptions.IsEnabled = true;
                PartFileToInternalNamingOptions.IsEnabled = false;
                break;
            case AutoRenameConfig.RenameDirection.FileToInternal:
                PartInternalToFileNamingOptions.IsEnabled = false;
                PartFileToInternalNamingOptions.IsEnabled = true;
                break;
            }
        }

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void ConfirmButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
