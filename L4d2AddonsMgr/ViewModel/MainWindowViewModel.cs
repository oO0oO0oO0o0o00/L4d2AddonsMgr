using L4d2AddonsMgr.AcfFileSpace;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using L4d2AddonsMgr.AddonsLibrarySpace;
using L4d2AddonsMgr.MeowTaskSpace;
using L4d2AddonsMgr.Service;
using Newtonsoft.Json;

namespace L4d2AddonsMgr {

    /*
     * ...
     * https://stackoverflow.com/questions/32985342/how-to-bind-sum-of-observable-collection-in-wpf
     */
    public class MainWindowViewModel : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isLoading;

        private string _filterText;

        private readonly string libraryPath;

        public bool SupportsEnabledState => libraryPath == null;

        private List<VpkHolder> allFiles;

        private bool listReady;

        private bool haventToggleEnabled;

        public AddonsLibrary Library {
            get; private set;
        }

        public string FilterText {
            get => _filterText; set {
                if (value == null || value == "" || value.Trim() == "")
                    value = null;
                else
                    value = value.ToLowerInvariant();
                string previous = _filterText;
                if (value != previous) {
                    _filterText = value;
                    OnPropertyChanged(nameof(FilterText));
                    RefreshShownList();
                }
            }
        }

        public ObservableCollection<VpkHolder> Files {
            get; set;
        }

        public List<VpkFilter> BuiltinFilters {
            get; private set;
        }

        public List<VpkFilter> DownloadUrlFilters {
            get; private set;
        }

        public bool IsLoading {
            get => _isLoading;
            private set {
                bool oldVal = _isLoading;
                _isLoading = value;
                if (oldVal != value) Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(IsLoading)));
            }
        }

        public long ShownFilesSize {
            get {
                long sum = 0;
                foreach (var file in Files)
                    sum += file.FileSize;
                return sum;
            }
        }

        public MainWindowViewModel() : this(null) { }

        public MainWindowViewModel(string libraryPath) {
            this.libraryPath = libraryPath;
            haventToggleEnabled = true;
        }

        public void Load() {
            if (libraryPath == null) {
                try {
                    string gameDir = GameDirService.LocateInstalledGame();
                    var addonsList = AddonListService.GetAddonsList(gameDir);
                    Library = new GameDirAddonsLibrary(gameDir, addonsList);
                } catch (GameDirService.Exception gameDirException) {
                    switch (gameDirException.TheReason) {
                    case GameDirService.Exception.Reason.SteamPathNotFoundInRegistry:
                        ErrBox("未能从您的计算机查找到Steam安装信息。");
                        break;
                    case GameDirService.Exception.Reason.GamePathNotFound:
                        ErrBox("未能从您的Steam库中查找到左4死2。");
                        break;
                    case GameDirService.Exception.Reason.GamePathBroken:
                        ErrBox("从您的Steam库中查找到的左4死2文件夹结构不完整。");
                        break;
                    default:
                        ErrBox("找不到您的左4死2安装。");
                        break;
                    }
                    return;
                } catch (AddonListService.Exception addonListException) {
                    ErrBox("读取附加组件配置文件失败，为避免数据丢失，部分功能将不可用。" +
                                    "建议您检查并更正addonlist.txt中的格式错误");
                }
            } else {
                Library = new ExternalDirectoryAddonsLibrary(libraryPath);
            }
            listReady = false;

            allFiles = new List<VpkHolder>();
            BuiltinFilters = new List<VpkFilter>();
            DownloadUrlFilters = new List<VpkFilter>();
            Files = new ObservableCollection<VpkHolder>();
            Files.CollectionChanged +=
                (s, e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ShownFilesSize"));
            ReloadFromLibrary();
        }

        private void ErrBox(string text) => MsgBox(text, "出现异常");

        private void MsgBox(string text, string caption = null)
            => Application.Current.Dispatcher.Invoke(() => MessageBox.Show(text, caption));

        public void ReloadFromLibrary() {
            IsLoading = true;
            ClearAddons();
            try {
                foreach (var item in Library) AddAddon(item);
            } catch (Exception) { }
            IsLoading = false;
            listReady = true;
        }

        private void AddAddon(VpkHolder vpkHolder) =>
            Application.Current.Dispatcher.Invoke(() => {
                allFiles.Add(vpkHolder);
                if (Filter(vpkHolder))
                    Files.Add(vpkHolder);
            });

        private void ClearAddons() =>
            Application.Current.Dispatcher.Invoke(() => {
                allFiles.Clear();
                Files.Clear();
            });

        public void ToggleAddonEnabledState(VpkHolder holder, bool enabled) {
            if (!(Library is GameDirAddonsLibrary gameDirAddonsLibrary)) throw new Exception();
            gameDirAddonsLibrary.listTxt.ToggleAddonEnabledStateAndWriteBack(holder.FileDispName, enabled);
            if (!haventToggleEnabled) return;
            var procL4d2 = ProcessQuitWaiter.GetRunningProcessOfPath(Path.Combine(gameDirAddonsLibrary.gameDir, CommonConsts.L4d2ExeFileName));
            if (procL4d2 == null) return;
            procL4d2.Dispose();
            MessageBox.Show("检测到L4D2正在运行。\n要使本软件中启用或禁用的操作" +
                            "立即生效，您【不需要】重新启动游戏，但是需要以下额外操作：\n" +
                            "1. 回到游戏主界面；\n" +
                            "2. 点击“附加内容”选项，有两个的话应该是第一个，也可能显示Addons；\n" +
                            "3. 如果有弹窗，点击“确定”；\n" +
                            "4. 点击“完成”。", "需要额外操作");
            haventToggleEnabled = false;
        }

        public void ToggleFilter(VpkFilter filter) {
            if (BuiltinFilters.Contains(filter))
                RemoveFilterRecur(filter);
            else
                AddFilterRecur(filter);
            RefreshShownList();
            OnPropertyChanged(nameof(BuiltinFilters));
        }

        public void AddFilter(VpkFilter filter) {
            if (!AddFilterRecur(filter)) return;
            OnPropertyChanged(nameof(BuiltinFilters));
            RefreshShownList();
        }

        public void RemoveFilter(VpkFilter filter) {
            if (!RemoveFilterRecur(filter)) return;
            OnPropertyChanged(nameof(BuiltinFilters));
            RefreshShownList();
        }

        public void ExportMapsList(string path) {
            var list = new Dictionary<string, List<string>>();
            foreach (var holder in Files) {
                try {
                    if (!holder.HasMission) continue;
                    var missions = holder.vpk.Vindex[CommonConsts.AddonTxtExtensionName][CommonConsts.AddonMissionsPathName];
                    var mission = missions.Select(desc => AcfFile.ParseString(holder.vpk.GetContainedFileText(desc.Value), true)).FirstOrDefault();
                    //MessageBox.Show(holder.MissionTitle??holder.AddonTitle);
                    if (!(mission?.GetNodeByPath(CommonConsts.AddonMissionConfigAcfNodeMission, "modes", "coop") is AcfFile.CompoundNode maps)) continue;
                    var subList = new List<string>();
                    foreach (var map in maps.Value) {
                        if (map is AcfFile.CompoundNode mapNode && mapNode.GetChild("Map") is AcfFile.LeafNode leaf) {
                            subList.Add(leaf.Value);
                        }
                    }
                    list[holder.MissionTitle ?? holder.AddonTitle] = subList;
                } catch (Exception) { }
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(list, Formatting.Indented));
        }

        private bool AddFilterRecur(VpkFilter filter) {
            if (BuiltinFilters.Contains(filter)) return false;
            // "I hate them."
            foreach (var hate in filter.hates) RemoveFilterRecur(hate);
            // "I hate those of my type."
            bool notDone;
            if (filter.exclusiveOfType)
                do {
                    notDone = false;
                    foreach (var item in BuiltinFilters.Where(item => item.GetType() == filter.GetType())) {
                        RemoveFilterRecur(item);
                        notDone = true;
                        break;
                    }
                } while (notDone);
            // "They hate me."
            do {
                notDone = false;
                foreach (var item in BuiltinFilters.Where(item => item.hates.Contains(filter))) {
                    RemoveFilterRecur(item);
                    notDone = true;
                    break;
                }
            } while (notDone);
            BuiltinFilters.Add(filter);
            // "I need these."
            foreach (var need in filter.needs)
                AddFilterRecur(need);
            return true;
        }

        private bool RemoveFilterRecur(VpkFilter filter) {
            if (BuiltinFilters.Remove(filter)) {
                bool notDone;
                // "They cannot survive without me."
                do {
                    notDone = false;
                    foreach (var item in BuiltinFilters)
                        if (item.needs.Contains(filter)) {
                            RemoveFilterRecur(item);
                            notDone = true;
                            break;
                        }
                } while (notDone);
                return true;
            }
            return false;
        }

        private bool Filter(VpkHolder holder) {
            if (!FilterByText(holder))
                return false;
            foreach (var filter in BuiltinFilters)
                if (!filter.Filter(holder))
                    return false;
            //foreach (var filter in urlFilters) if (!filter(holder)) return false;
            return true;
        }

        private bool FilterByText(VpkHolder holder) {
            if (_filterText == null)
                return true;
            if (holder.AddonTitle != null) {
                if (holder.AddonTitle.Contains(_filterText))
                    return true;
                if (holder.AddonSearchName.Match(_filterText)) {
                    Debug.WriteLine(holder.AddonTitle);
                    return true;
                }
            }
            if (holder.MissionTitle != null) {
                if (holder.MissionTitle.Contains(_filterText))
                    return true;
                if (holder.MissionSearchName.Match(_filterText)) {
                    Debug.WriteLine(holder.MissionTitle);
                    return true;
                }
            }
            if (holder.FileNameNoExt.Contains(_filterText))
                return true;
            if (holder.FileSearchName.Match(_filterText)) {
                Debug.WriteLine(holder.FileNameNoExt);
                return true;
            }
            return false;
        }

        private void RefreshShownList() {
            IsLoading = true;
            Application.Current.Dispatcher.Invoke(() => Files.Clear());
            foreach (var item in allFiles)
                if (Filter(item)) {
                    // Vitualize the wrap panel.
                    // https://stackoverflow.com/questions/32720694/virtualizing-wrappanel-as-listviews-itemstemplate
                    Application.Current.Dispatcher.Invoke(() => Files.Add(item));
                    //Thread.Sleep(10);
                }
            IsLoading = false;
        }

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public class VpkFilter {

            public bool exclusiveOfType;

            public readonly VpkFilter[] needs;

            public readonly VpkFilter[] hates;

            public readonly Func<VpkHolder, bool> Filter;

            public VpkFilter(VpkFilter[] needs, VpkFilter[] hates, Func<VpkHolder, bool> filter) {
                this.needs = needs;
                this.hates = hates;
                Filter = filter;
            }
        }

        public class DownloadSourceUrlUsedByFilter {

            private VpkFilter _filter;

            public readonly string name;

            public readonly string[] urls;

            public VpkFilter Filter => _filter ?? (_filter = new VpkFilter(
                new VpkFilter[0], new VpkFilter[] { GeneralFilters.SourceSbeamFilter },
                vpkHolder => {
                    try {
                        string txt = vpkHolder.vpk.GetContainedFileText(CommonConsts.AddonRuntimeRootPathName,
                        CommonConsts.AddonAddonInfoFileName, CommonConsts.AddonTxtExtensionName);
                        foreach (string str in urls)
                            if (txt.Contains(str))
                                return true;
                    } catch (Exception) { }
                    return false;
                }) {
                exclusiveOfType = true
            });

            public DownloadSourceUrlUsedByFilter(string name, string[] urls) {
                this.name = name;
                this.urls = urls;
            }
        }

        public static class GeneralFilters {

            public static readonly VpkFilter SourceSbeamFilter;

            public static readonly VpkFilter SourceNonSbeamFilter;

            public static readonly VpkFilter TypeMapFilter;

            public static readonly VpkFilter TypeNonMapFilter;

            public static readonly VpkFilter StateEnabledFilter;

            public static readonly VpkFilter StateDisabledFilter;

            static GeneralFilters() {
                SourceSbeamFilter = new VpkFilter(new VpkFilter[0], new VpkFilter[0],
                    (vpkHolder) => (vpkHolder.vpkDir == VpkHolder.VpkDirType.WorkShopDir));

                SourceNonSbeamFilter = new VpkFilter(new VpkFilter[0], new VpkFilter[] { SourceSbeamFilter },
                    (vpkHolder) => (vpkHolder.vpkDir != VpkHolder.VpkDirType.WorkShopDir));

                TypeMapFilter = new VpkFilter(new VpkFilter[0], new VpkFilter[0],
                    (vpkHolder) => vpkHolder.HasMission);

                TypeNonMapFilter = new VpkFilter(new VpkFilter[0], new VpkFilter[] { TypeMapFilter },
                    (vpkHolder) => !vpkHolder.HasMission);

                StateEnabledFilter = new VpkFilter(new VpkFilter[0], new VpkFilter[0],
                    (vpkHolder) => vpkHolder.IsEnabled);

                StateDisabledFilter = new VpkFilter(new VpkFilter[0], new VpkFilter[] { StateEnabledFilter },
                    (vpkHolder) => !vpkHolder.IsEnabled);
            }

        }

        public static class KnownSourceUrls {

            public static readonly DownloadSourceUrlUsedByFilter L4d2Cc;

            public static readonly DownloadSourceUrlUsedByFilter GameMaps;

            public static readonly DownloadSourceUrlUsedByFilter Steam;

            public static Dictionary<string, DownloadSourceUrlUsedByFilter> RegisteredFilters;

            static KnownSourceUrls() {
                L4d2Cc = new DownloadSourceUrlUsedByFilter("l4d2.cc", new string[] { "l4d2.cc", "kk175.com" });
                GameMaps = new DownloadSourceUrlUsedByFilter("Game Maps", new string[] { "gamemaps.com" });
                Steam = new DownloadSourceUrlUsedByFilter("Steam Workshop", new string[] { "steamcommunity.com" });
                RegisteredFilters = new Dictionary<string, DownloadSourceUrlUsedByFilter> {
                    { L4d2Cc.name, L4d2Cc },
                    { GameMaps.name, GameMaps },
                    { Steam.name, Steam }
                };
            }

        }

    }

}
