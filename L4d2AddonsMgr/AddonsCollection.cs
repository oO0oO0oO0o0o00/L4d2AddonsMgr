using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace L4d2AddonsMgr {

    /*
     * ...
     * https://stackoverflow.com/questions/32985342/how-to-bind-sum-of-observable-collection-in-wpf
     */
    public class AddonsCollection : INotifyPropertyChanged {

        private string _filterText;

        public event PropertyChangedEventHandler PropertyChanged;

        private List<VpkHolder> allFiles;
        private bool _isLoading;

        public string FilterText {
            get => _filterText; set {
                if (value == null || value == "" || value.Trim() == "") value = null;
                else value = value.ToLowerInvariant();
                var previous = _filterText;
                if (value != previous) {
                    _filterText = value;
                    OnPropertyChanged(nameof(FilterText));
                    RefreshShownList();
                }
            }
        }

        public ObservableCollection<VpkHolder> Files { get; set; }

        public List<VpkFilter> BuiltinFilters { get; }

        public List<VpkFilter> DownloadUrlFilters { get; }

        public bool IsLoading {
            get => _isLoading;
            set {
                var oldVal = _isLoading;
                _isLoading = value;
                if (oldVal != value) OnPropertyChanged(nameof(IsLoading));
            }
        }

        public long ShownFilesSize {
            get {
                long sum = 0;
                foreach (var file in Files) sum += file.FileSize;
                return sum;
            }
        }

        public AddonsCollection() {
            allFiles = new List<VpkHolder>();
            BuiltinFilters = new List<VpkFilter>();
            DownloadUrlFilters = new List<VpkFilter>();
            Files = new ObservableCollection<VpkHolder>();
            Files.CollectionChanged +=
                (s, e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ShownFilesSize"));
        }

        public void Add(VpkHolder vpkHolder) {
            allFiles.Add(vpkHolder);
            if (Filter(vpkHolder)) Files.Add(vpkHolder);
        }

        public void Clear() {
            allFiles.Clear();
            Files.Clear();
        }

        public void ToggleFilter(VpkFilter filter) {
            if (BuiltinFilters.Contains(filter)) RemoveFilterRecur(filter);
            else AddFilterRecur(filter);
            RefreshShownList();
            OnPropertyChanged(nameof(BuiltinFilters));
        }

        public void AddFilter(VpkFilter filter) {
            if (AddFilterRecur(filter)) {
                OnPropertyChanged(nameof(BuiltinFilters));
                RefreshShownList();
            }
        }

        public void RemoveFilter(VpkFilter filter) {
            if (RemoveFilterRecur(filter)) {
                OnPropertyChanged(nameof(BuiltinFilters));
                RefreshShownList();
            }
        }

        private bool AddFilterRecur(VpkFilter filter) {
            if (BuiltinFilters.Contains(filter)) return false;
            // I hate them.
            foreach (var hate in filter.hates) RemoveFilterRecur(hate);
            // I hate those of my type.
            bool notDone;
            if (filter.exclusiveOfType)
                do {
                    notDone = false;
                    foreach (var item in BuiltinFilters)
                        if (item.GetType() == filter.GetType()) {
                            RemoveFilterRecur(item);
                            notDone = true;
                            break;
                        }
                } while (notDone);
            // They hate me.
            do {
                notDone = false;
                foreach (var item in BuiltinFilters)
                    if (item.hates.Contains(filter)) {
                        RemoveFilterRecur(item);
                        notDone = true;
                        break;
                    }
            } while (notDone);
            BuiltinFilters.Add(filter);
            // I need these.
            foreach (var need in filter.needs) AddFilterRecur(need);
            return true;
        }

        private bool RemoveFilterRecur(VpkFilter filter) {
            if (BuiltinFilters.Remove(filter)) {
                bool notDone;
                // They cannot survive without me.
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
            if (!FilterByText(holder)) return false;
            foreach (var filter in BuiltinFilters) if (!filter.Filter(holder)) return false;
            //foreach (var filter in urlFilters) if (!filter(holder)) return false;
            return true;
        }

        private bool FilterByText(VpkHolder holder) {
            if (_filterText == null) return true;
            if (holder.AddonTitle != null) {
                if (holder.AddonTitle.Contains(_filterText)) return true;
                if (holder.AddonSearchName.Match(_filterText)) {
                    Debug.WriteLine(holder.AddonTitle);
                    return true;
                }
            }
            if (holder.MissionTitle != null) {
                if (holder.MissionTitle.Contains(_filterText)) return true;
                if (holder.MissionSearchName.Match(_filterText)) {
                    Debug.WriteLine(holder.MissionTitle);
                    return true;
                }
            }
            if (holder.FileNameNoExt.Contains(_filterText)) return true;
            if (holder.FileSearchName.Match(_filterText)) {
                Debug.WriteLine(holder.FileNameNoExt);
                return true;
            }
            return false;
        }

        private async void RefreshShownList() {
            IsLoading = true;
            Files.Clear();
            // Get a dispatcher.
            // https://stackoverflow.com/questions/11625208/accessing-ui-main-thread-safely-in-wpf
            await Task.Run(() => {
                foreach (var item in allFiles)
                    if (Filter(item)) {
                        // Vitualize the wrap panel.
                        // https://stackoverflow.com/questions/32720694/virtualizing-wrappanel-as-listviews-itemstemplate
                        Application.Current.Dispatcher.Invoke(() => Files.Add(item));
                        //Thread.Sleep(10);
                    }
            });
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
                        var txt = vpkHolder.vpk.GetContainedFileText(CommonConsts.AddonRuntimeRootPathName,
                        CommonConsts.AddonAddonInfoFileName, CommonConsts.AddonTxtExtensionName);
                        foreach (var str in urls) if (txt.Contains(str)) return true;
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