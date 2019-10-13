using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using L4d2AddonsMgr.AcfFileSpace;
using L4d2AddonsMgr.SearchSpace;
using L4d2AddonsMgr.VpkSpace;

namespace L4d2AddonsMgr {

    public class VpkHolder : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly bool loadFailed;
        private bool hasLoadedBriefDescr;

        public AddonBriefInfo addonBriefInfo;
        private bool _isEnabled;
        internal readonly Vpk vpk;
        public readonly VpkDirType vpkDir;

        private SearchName _fileSearchName;
        private SearchName _addonSearchName;
        private SearchName _missionSearchName;

        private string _fileNameNoExt;

        public bool IsEnabled {
            get => _isEnabled; set {
                _isEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        public FileInfo FileInf { get; }

        public string FileDispName => vpkDir == VpkDirType.WorkShopDir ? "workshop\\" + FileInf.Name : FileInf.Name;

        public string FileNameNoExt => _fileNameNoExt ?? (_fileNameNoExt = Path.GetFileNameWithoutExtension(FileInf.Name));

        public long FileSize { get; }

        public string Name {
            get {
                if (!hasLoadedBriefDescr) LoadBriefDescr();
                string ret = addonBriefInfo.preferMissionTitle ?
                    addonBriefInfo.missionTitle : addonBriefInfo.addonTitle;
                if (ret == null) ret = addonBriefInfo.preferMissionTitle ?
                        addonBriefInfo.addonTitle : addonBriefInfo.missionTitle;
                if (ret == null) ret = addonBriefInfo.missionNameAsIdentifier;
                return ret ?? CommonConsts.AddonUndefinedName;
            }
        }

        public string AddonTitle => addonBriefInfo.addonTitle;

        public string MissionTitle => addonBriefInfo.missionTitle;

        public bool HasMission {
            get {
                if (!hasLoadedBriefDescr) LoadBriefDescr();
                return addonBriefInfo.missionsCount > 0;
            }
        }

        public SearchName FileSearchName => _fileSearchName ?? (_fileSearchName = new SearchName(FileNameNoExt));

        public SearchName AddonSearchName => _addonSearchName ?? (
            AddonTitle == null ? null : (_addonSearchName = new SearchName(AddonTitle)));

        public SearchName MissionSearchName => _missionSearchName ?? (
            MissionTitle == null ? null : (_missionSearchName = new SearchName(MissionTitle)));

        public void LoadBriefDescr() {

            addonBriefInfo.missionsCount = 0;
            addonBriefInfo.addonTitle = null;
            addonBriefInfo.missionTitle = null;
            addonBriefInfo.missionNameAsIdentifier = null;

            if (loadFailed) return;

            // If a readable mission titlee exists we prefer using it than addon title.
            addonBriefInfo.preferMissionTitle = true;

            // Find mission and addon meta data.

            bool res = vpk.Vindex.TryGetValue(CommonConsts.AddonTxtExtensionName, out var txtNode);
            if (!res) goto AfterReadingTxts;

            res = txtNode.TryGetValue(CommonConsts.AddonMissionsPathName, out var missionsNode);
            if (!res || missionsNode.Count <= 0) goto AfterReadingMissionMeta;
            addonBriefInfo.missionsCount = missionsNode.Count;

            Vpk.VpkContainedFileDescription descr = null;
            foreach (var v in missionsNode) {
                descr = v.Value;
            }

            AcfFile acf;
            try {
                acf = AcfFile.ParseString(vpk.GetContainedFileText(descr), true);
                Debug.WriteLineIf(acf.HasError, string.Format(
                    "Warning: Mission definition file in {0} has error but was still attempted to be parsed.", FileDispName));
            } catch (Exception) {
                goto AfterReadingMissionMeta;
            }

            addonBriefInfo.missionTitle = acf.GetValueOfPath(
                CommonConsts.AddonMissionConfigAcfNodeMission,
                CommonConsts.AddonMissionConfigAcfNodeTitle);
            addonBriefInfo.missionNameAsIdentifier = acf.GetValueOfPath(
                CommonConsts.AddonMissionConfigAcfNodeMission,
                CommonConsts.AddonMissionConfigAcfNodeName);
            if (addonBriefInfo.missionTitle == null) {
                addonBriefInfo.preferMissionTitle = false;
                addonBriefInfo.missionTitle = addonBriefInfo.missionNameAsIdentifier;
            }

AfterReadingMissionMeta:

// This hardly fails so try-catch is cheaper than do-and-check.
            try {
                acf = AcfFile.ParseString(vpk.GetContainedFileText(
                    txtNode
                        [CommonConsts.AddonRuntimeRootPathName]
                        [CommonConsts.AddonAddonInfoFileName]
                    ), true);
                Debug.WriteLineIf(acf.HasError, string.Format(
                    "Warning: Addon definition file in {0} has error but was still attempted to be parsed.", FileDispName));
                var addonInfoNode = AcfFile.GetNode(
                    acf.Root, CommonConsts.AddonInfoAcfNodeRoot
                ) as AcfFile.CompoundNode;
                if (AcfFile.GetNode(
                    addonInfoNode, CommonConsts.AddonInfoAcfNodeTitle
                ) is AcfFile.LeafNode leaf)
                    addonBriefInfo.addonTitle = leaf.Value;

                if (addonBriefInfo.addonTitle == null &&
                    (leaf = AcfFile.GetNodeIgnoreCase(
                        addonInfoNode, CommonConsts.AddonInfoAcfNodeTitle
                    ) as AcfFile.LeafNode) != null)
                    addonBriefInfo.addonTitle = leaf.Value;

                if (addonBriefInfo.addonTitle == null && addonBriefInfo.missionTitle == null
                    && addonBriefInfo.missionNameAsIdentifier == null) {
                    leaf = AcfFile.GetNode(
                        addonInfoNode, CommonConsts.AddonInfoAcfNodeTagLine
                    ) as AcfFile.LeafNode;
                    if (leaf != null) addonBriefInfo.addonTitle = leaf.Value;
                }
                leaf = AcfFile.GetNode(
                    addonInfoNode, CommonConsts.AddonInfoAcfNodeHasCompaign
                ) as AcfFile.LeafNode;
                if (leaf != null && addonBriefInfo.missionsCount == 0
                    && leaf.Value == CommonConsts.StringRepresentationForTrue)
                    addonBriefInfo.missionsCount = 1;
            } catch (Exception) { }

AfterReadingTxts:

            hasLoadedBriefDescr = true;
        }

        public VpkHolder(FileInfo fileInfo, VpkDirType dirType) {
            FileInf = fileInfo;
            try {
                vpk = Vpk.OpenFile(fileInfo);
                loadFailed = false;
            } catch (Exception e) {
                Debug.WriteLine("Warning: A vpk file was not opened due to exception.");
                Debug.WriteLine(e.Message);
                loadFailed = true;
            }
            vpkDir = dirType;
            FileSize = fileInfo.Length;
            LoadBriefDescr();
        }

        public void UpdateFileName() {
            _fileNameNoExt = null;
            _fileSearchName = null;
            OnPropertyChanged(nameof(FileDispName));
            OnPropertyChanged(nameof(FileNameNoExt));
            OnPropertyChanged(nameof(FileSearchName));
        }

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public enum VpkDirType {
            AddonsDir, WorkShopDir, DisabledDir, External
        }

    }

}
