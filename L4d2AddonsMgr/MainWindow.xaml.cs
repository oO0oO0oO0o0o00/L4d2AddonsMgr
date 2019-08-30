using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Fluent;
using L4d2AddonsMgr.AutoRenameSpace;
using L4d2AddonsMgr.MeowTaskSpace;
using L4d2AddonsMgr.OperationSpace;

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

        public static readonly RoutedCommand ToggleAddonEnabledCommand = new RoutedCommand();
        public static readonly RoutedCommand TriggerSearchCommand = new RoutedCommand();

        public static readonly RoutedCommand ShowItemInExplorerCommand = new RoutedCommand();

        public static readonly RoutedCommand RefreshListCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleFilterCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleFilterDownloadSourceCommand = new RoutedCommand();

        public static readonly RoutedCommand OpAutoRenameCommand = new RoutedCommand();

        private bool listReady;
        private string gameDir;

        private bool haventToggleEnabled;

        public AddonsCollection Addons { get; private set; }

        private AddonListTxt addonListTxt;

        public MainWindow() {
            InitializeComponent();
            Addons = new AddonsCollection();
            Addons.PropertyChanged +=
                (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
                    if (e.PropertyName == nameof(AddonsCollection.IsLoading)) {
                        CommandManager.InvalidateRequerySuggested();
                    };
                };
            DataContext = this;
            listReady = false;

            haventToggleEnabled = true;
        }

        // Stay identical with Windows accent color.
        // https://stackoverflow.com/questions/13660976/get-the-active-color-of-windows-8-automatic-color-theme
        protected override void OnSourceInitialized(EventArgs e) {
            IntPtr hwnd;
            if ((hwnd = new WindowInteropHelper(this).Handle) != IntPtr.Zero)
                HwndSource.FromHwnd(hwnd).AddHook(WndProc);
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

        /*
         * AsyncTask?
         * https://stackoverflow.com/questions/27089263/how-to-run-and-interact-with-an-async-task-from-a-wpf-gui
         */
        private async void AddonsGrid_Loaded(object sender, RoutedEventArgs e) {
            if (!listReady) {
                try {
                    await Task.Run(() => FindGameDir());
                } catch (AddonsLoadingException ale) {
                    Dispatcher.Invoke(() => {
                        LogErrorAndQuit(string.Format("尝试获取L4d2文件夹失败。\n{0}", ale.Reason), ale);
                    });
                    return;
                } catch (Exception exc) {
                    Dispatcher.Invoke(() => {
                        LogErrorAndQuit("。。。", exc);
                    });
                    return;
                }
                Addons.IsLoading = true;
                await Task.Run(() => ReloadAddonList());
                await Task.Run(() => RefreshAddonsFromFile());
                Addons.IsLoading = false;
                listReady = true;
            }
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
                Process.Start("explorer.exe", "/select, \"" + fileInfo.FullName + "\"");
        }
        private async void RefreshListCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            Addons.IsLoading = true;
            await Task.Run(() => ReloadAddonList());
            await Task.Run(() => RefreshAddonsFromFile());
            Addons.IsLoading = false;
        }

        private void ToggleFilterCommand_CanInvoke(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = (null != Addons) ? !Addons.IsLoading : false;

        private void ToggleFilterCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            if (e.Parameter is AddonsCollection.VpkFilter filter) Addons.ToggleFilter(filter);
        }

        private void ToggleFilterDownloadSourceCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            if (e.Parameter is string name)
                Addons.ToggleFilter(AddonsCollection.KnownSourceUrls.RegisteredFilters[name].Filter);
        }

        private void OpSelOrAll_CanInvoke(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void OpAutoRenameCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            var dialog = new AutoRenameDialog() {
                Owner = this,
                SelectedItemsCount = AddonsListBox.SelectedItems?.Count ?? 0
            };
            var res = dialog.ShowDialog();
            if (res ?? false) {
                List<VpkHolder> list;
                switch (dialog.MyAppliedScope.MyScope) {
                case AppliedScope.Scope.All:
                    list = new List<VpkHolder>(Addons.Files);
                    break;
                case AppliedScope.Scope.SelectedItems:
                    // synchronized and lock:
                    // https://stackoverflow.com/questions/541194/c-sharp-version-of-javas-synchronized-keyword
                    lock (AddonsListBox.SelectedItems) {
                        list = new List<VpkHolder>(AddonsListBox.SelectedItems.Count);
                        foreach (var o in AddonsListBox.SelectedItems) list.Add((VpkHolder)o);
                    }
                    break;
                default: return;
                }
                Addons.IsLoading = true;
                var renameTask = new AutoRenameTask(list, dialog.Model, gameDir, addonListTxt);
                var dialog2 = new MeowTaskDialog() {
                    Owner = this,
                    MyTask = renameTask
                };
                var res2 = dialog2.ShowDialog();
                if (res2 ?? false) MessageBox.Show("已完成。", "自动重命名");
                Addons.IsLoading = false;
            }
        }

        private void TriggerSearchCommand_Invoke(object sender, ExecutedRoutedEventArgs e) => SearchBox.ActivateSearch();

        private void CanTriggerSearchCommand(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private async void ToggleAddonEnabledCommand_Invoke(object sender, ExecutedRoutedEventArgs e) {
            var holder = e.Parameter as VpkHolder;
            var enabled = !holder.IsEnabled;
            holder.IsEnabled = enabled;
            await Task.Run(() => addonListTxt.ToggleAddonEnabledStateAndWriteBack(holder.FileDispName, enabled));
            if (haventToggleEnabled) {
                var procL4d2 = ProcessQuitWaiter.GetRunningProcessOfPath(Path.Combine(gameDir, CommonConsts.L4d2ExeFileName));
                if (procL4d2 != null) {
                    procL4d2.Dispose();
                    MessageBox.Show("检测到L4D2正在运行。\n要使本软件中启用或禁用的操作" +
                        "立即生效，您【不需要】重新启动游戏，但是需要以下额外操作：\n" +
                        "1. 回到游戏主界面；\n" +
                        "2. 点击“附加内容”选项，有两个的话应该是第一个，也可能显示Addons；\n" +
                        "3. 如果有弹窗，点击“确定”；\n" +
                        "4. 点击“完成”。", "需要额外操作");
                    haventToggleEnabled = false;
                }
            }
        }

        private void CanToggleAddonEnabled(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void ExplorerSearchBox_SearchRequested(object sender, string e) => Addons.FilterText = e;

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
            if (text != null) Debug.WriteLine(text);
            Debug.WriteLine(exception);
            Close();
        }

    }
}
