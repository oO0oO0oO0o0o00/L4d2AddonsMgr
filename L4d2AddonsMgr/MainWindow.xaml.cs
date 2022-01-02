using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Fluent;
using L4d2AddonsMgr.AddonsLibrarySpace;
using L4d2AddonsMgr.AutoRenameSpace;
using L4d2AddonsMgr.MeowTaskSpace;
using L4d2AddonsMgr.OperationSpace;
using L4d2AddonsMgr.RealFolderPicker;
using L4d2AddonsMgr.Utils;

namespace L4d2AddonsMgr {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    /*
     * Getting start...
     * String or string
     * https://stackoverflow.com/questions/7074/what-is-the-difference-between-string-and-string-in-c
     * Data types... https://www.tutorialspoint.com/csharp/csharp_data_types.htm
     * 
     * Naming convention?
     * https://github.com/ktaranov/naming-convention/blob/master/C%23%20Coding%20Standards%20and%20Naming%20Conventions.md
     * Good point however won't be followed.
     * 
     * Window for now.
     * https://stackoverflow.com/questions/5243910/page-vs-window-in-wpf
     * 
     * It seems not failing so renaming the workspace & project from WpfApp1 to the one being used.
     * https://stackoverflow.com/questions/43134850/rename-a-project-in-visual-studio-2017
     * 
     * Most of MS docs refs not listed as they're obvious to be used.
     */
    public partial class MainWindow : RibbonWindow {

        const int WmDwmColorizationColorChanged = 0x320;

        public static readonly RoutedCommand OpenExternalLibraryCommand = new RoutedCommand();

        public static readonly RoutedCommand ToggleAddonEnabledCommand = new RoutedCommand();
        public static readonly RoutedCommand TriggerSearchCommand = new RoutedCommand();

        public static readonly RoutedCommand ShowItemInExplorerCommand = new RoutedCommand();

        public static readonly RoutedCommand RefreshListCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleFilterCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleFilterDownloadSourceCommand = new RoutedCommand();

        public static readonly RoutedCommand OpAutoRenameCommand = new RoutedCommand();

        private Point dragPoint;

        private DataObject dragObj;

        public MainWindow() {
            InitializeComponent();
            ViewModel = new MainWindowViewModel();
            dragPoint = new Point(-1.0, 0.0);
        }

        public MainWindow(string libraryPath) {
            InitializeComponent();
            ViewModel = new MainWindowViewModel(libraryPath);
        }

        public MainWindowViewModel ViewModel {
            get;
        }

        // Stay identical with Windows accent color.
        // https://stackoverflow.com/questions/13660976/get-the-active-color-of-windows-8-automatic-color-theme
        protected override void OnSourceInitialized(EventArgs e) {
            IntPtr hwnd;
            if ((hwnd = new WindowInteropHelper(this).Handle) != IntPtr.Zero)
                HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
            UpdateAccentColor();
            base.OnSourceInitialized(e);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            switch (msg) {
            case WmDwmColorizationColorChanged:
                UpdateAccentColor();
                return IntPtr.Zero;
            default:
                return IntPtr.Zero;
            }
        }

        private async void AddonsGrid_Loaded(object sender, RoutedEventArgs e) {
            await Task.Run(() => ViewModel.Load());
            ViewModel.PropertyChanged +=
                (sender2, e2) => {
                    if (e2.PropertyName == nameof(MainWindowViewModel.IsLoading)) {
                        CommandManager.InvalidateRequerySuggested();
                    };
                };
            DataContext = ViewModel;
        }

        private void OpenExternalLibraryCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            var picker = new FolderPicker();
            var result = picker.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                new MainWindow(picker.DirectoryPath).Show();
        }

        private void OpenItemCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            // Open & Open As:
            // https://stackoverflow.com/questions/4726441/how-can-i-show-the-open-with-file-dialog
            if (e.Parameter is FileInfo fileInfo) {
                var procInfo = new ProcessStartInfo(fileInfo.FullName);
                const string verbOpen = "open";
                const string verbOpenAs = "openas";
                if (procInfo.Verbs.Contains(verbOpen))
                    procInfo.Verb = verbOpen;
                // The "open as" never works.
                else if (procInfo.Verbs.Contains(verbOpenAs))
                    procInfo.Verb = verbOpen;
                else {
                    // Open as. Space & comma allowed. Do not wrap with quotes. Cannot set default.
                    // https://social.msdn.microsoft.com/Forums/en-US/ba055db6-3d7e-4724-94cd-309e7f29e4ac/explicitly-open-choose-program-window-c-to-open-a-file?forum=netfxbcl
                    Process.Start("rundll32.exe", "shell32.dll, OpenAs_RunDLL " + fileInfo.FullName);
                    return;
                }
                Process.Start(procInfo);
            }
        }

        private void ShowItemInExplorerCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            // Show in explorer:
            // https://stackoverflow.com/questions/334630/opening-a-folder-in-explorer-and-selecting-a-file
            if (e.Parameter is FileInfo fileInfo)
                ExplorerInterop.OpenFolderAndSelectItem(fileInfo.DirectoryName, fileInfo.Name);
        }

        private async void RefreshListCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            // TODO: Reload addon list.
            //await Task.Run(() => ReloadAddonList());
            await Task.Run(() => ViewModel.ReloadFromLibrary());
        }

        private void ToggleFilterCommand_CanInvoke(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = (null != ViewModel) ? !ViewModel.IsLoading : false;

        private async void ToggleFilterCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            if (e.Parameter is MainWindowViewModel.VpkFilter filter)
                await Task.Run(() => ViewModel.ToggleFilter(filter));
        }

        private async void ToggleFilterDownloadSourceCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            if (e.Parameter is string name)
                await Task.Run(() => ViewModel.ToggleFilter(MainWindowViewModel.KnownSourceUrls.RegisteredFilters[name].Filter));
        }

        private void OpSelOrAll_CanInvoke(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void OpAutoRenameCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            if (!(ViewModel.Library is GameDirAddonsLibrary gameDirAddonsLibrary)) return;
            var dialog = new AutoRenameDialog() {
                Owner = this,
                SelectedItemsCount = AddonsListBox.SelectedItems?.Count ?? 0
            };
            bool? res = dialog.ShowDialog();
            if (!(res ?? false)) return;
            List<VpkHolder> list;
            switch (dialog.MyAppliedScope.MyScope) {
            case AppliedScope.Scope.All:
                list = new List<VpkHolder>(ViewModel.Files);
                break;
            case AppliedScope.Scope.SelectedItems:
                // synchronized and lock:
                // https://stackoverflow.com/questions/541194/c-sharp-version-of-javas-synchronized-keyword
                lock (AddonsListBox.SelectedItems) {
                    list = new List<VpkHolder>(AddonsListBox.SelectedItems.Count);
                    foreach (object o in AddonsListBox.SelectedItems)
                        list.Add((VpkHolder) o);
                }
                break;
            default:
                return;
            }
            var renameTask = new AutoRenameTask(list, dialog.Model, gameDirAddonsLibrary.gameDir, gameDirAddonsLibrary.listTxt);
            var dialog2 = new MeowTaskDialog() {
                Owner = this,
                MyTask = renameTask
            };
            bool? res2 = dialog2.ShowDialog();

            // How would it be known by just guessing.
            // https://www.wpf-tutorial.com/dialogs/the-messagebox/
            if (res2 ?? false)
                MessageBox.Show("已完成。", "自动重命名");
        }

        private void TriggerSearchCommand_Invoke(object sender, ExecutedRoutedEventArgs e) => SearchBox.ActivateSearch();

        private void CanTriggerSearchCommand(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private async void ToggleAddonEnabledCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            var holder = e.Parameter as VpkHolder;
            bool enabled = !holder.IsEnabled;
            holder.IsEnabled = enabled;
            await Task.Run(() => ViewModel.ToggleAddonEnabledState(holder, enabled));
        }

        private void CanToggleAddonEnabled(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void ExplorerSearchBox_SearchRequested(object sender, string e) => ViewModel.FilterText = e;

        private void UpdateAccentColor() {
            const string RibbonAccentBrushKey = "Fluent.Ribbon.Brushes.AccentBaseColorBrush";
            const string RibbonHighlightBrushKey = "Fluent.Ribbon.Brushes.HighlightBrush";
            //var color = Windowsifier.GetAccentColor();
            //color.A = 0;
            // Manual set resource.
            //https://www.dotnetcurry.com/wpf/1142/resources-wpf-static-dynamic-difference
            Resources.Remove(RibbonAccentBrushKey);
            Resources.Remove(RibbonHighlightBrushKey);
            Resources.Add(RibbonAccentBrushKey, SystemParameters.WindowGlassBrush);// new SolidColorBrush(color));
            Resources.Add(RibbonHighlightBrushKey, SystemParameters.WindowGlassBrush);
        }

        private void LogErrorAndQuit(string text, Exception exception) {
            MessageBox.Show(text ?? string.Format("应用程序发生错误：{0}", exception.Message));
            if (text != null)
                Debug.WriteLine(text);
            Debug.WriteLine(exception);
            Close();
        }

        private void AddonsListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            dragPoint = e.GetPosition(null);
        }

        private void AddonsListBox_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (dragPoint.X < 0.0)
                return;
            var pt = e.GetPosition(null);
            double a = pt.X - dragPoint.X;
            double b = pt.Y - dragPoint.Y;
            if (a * a + b * b > SystemParameters.MinimumHorizontalDragDistance * SystemParameters.MinimumHorizontalDragDistance) {
                dragPoint = new Point(-1.0, 0.0);
                var list = sender as ListBox;
                var li =
                    ((DependencyObject) e.OriginalSource).FindAnchestor<ListBoxItem>();
                if (li == null)
                    return;
                var holder = (VpkHolder) list.ItemContainerGenerator.ItemFromContainer(li);
                var dobj = dragObj = new DataObject();
                dobj.SetFileDropList(new System.Collections.Specialized.StringCollection() { holder.FileInf.FullName });
                var ret = DragDrop.DoDragDrop(li, dobj, DragDropEffects.Copy | DragDropEffects.Move);
                Console.WriteLine("ret = " + ret);
                dragObj = null;
                //if (ret.HasFlag(DragDropEffects.Move))
                //Addons.ReloadFromLibrary();
            }
        }

        private void AddonsListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            dragPoint = new Point(-1.0, 0.0);
        }

        private void AddonsListBox_DragValidate(object sender, DragEventArgs e) {
            // No dropping to self.
            if (dragObj != null) {
                e.Effects = DragDropEffects.None;
            } else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void AddonsListBox_Drop(object sender, DragEventArgs e) {
            // TODO support linking.
            // TODO differentiate copy/move.
            //if (e.Effects.HasFlag(DragDropEffects.Move)) {
            //   e.Data
            // }else 
            MessageBox.Show("123");
            if (e.Effects.HasFlag(DragDropEffects.Copy)) {
                string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files) {
                    Console.WriteLine(file);
                }
            }
        }
    }
}
