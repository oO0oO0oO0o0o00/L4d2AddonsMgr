using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using L4d2AddonsMgr.AcfFileSpace;

namespace L4d2AddonsMgr {

    public class AddonsListTxt {

        private readonly string filePath;
        private AcfFile addonList;
        private AcfFile.CompoundNode addonListBaseNode;
        private bool dirty;

        public bool IsAddonListCreated { get; private set; }

        public AddonsListTxt(string gameDir) {
            IsAddonListCreated = false;
            filePath = Path.Combine(gameDir, CommonConsts.L4d2MainSubdirName, CommonConsts.AddonListTxtFileName);
            try {
                using (var reader = new StreamReader(filePath, Encoding.Default)) {
                    addonList = AcfFile.ParseString(reader.ReadToEnd(), false);
                    addonListBaseNode = addonList.Root.GetChild(CommonConsts.AddonListAcfRootKey) as AcfFile.CompoundNode;
                    if (addonListBaseNode == null) {
                        Debug.WriteLine("Warning: Syntax error within addonlist.txt.");
                        throw new LoadingException();
                    }
                }
                dirty = false;
            } catch (FileNotFoundException e) {
                Debug.WriteLine("Warning: Addonlist.txt is not found and will be created");
                Debug.WriteLine(e);
                CreateAddonList();
            } catch (Exception e) {
                Debug.WriteLine("Warning: Addonlist.txt cannot be read, some features would be inaccessible.");
                Debug.WriteLine(e);
                throw new LoadingException();
            }
        }

        private void CreateAddonList() {
            addonList = AcfFile.CreateNew();
            addonListBaseNode = new AcfFile.CompoundNode(CommonConsts.AddonListAcfRootKey, addonList.Root);
            addonList.Root.Add(addonListBaseNode);
            IsAddonListCreated = true;
        }

        public bool IsAddonEnabledInList(string name) {
            foreach (var node in addonListBaseNode.Value) {
                if (node is AcfFile.LeafNode leaf && name == leaf.Key) {
                    return CommonConsts.StringRepresentationForFalse != leaf.Value;
                }
            }
            return true;
        }

        public void ToggleAddonEnabledStateAndWriteBack(string name, bool enabled) {
            ToggleAddonEnabledState(name, enabled);
            SaveToFile();
        }

        public void ToggleAddonEnabledState(string name, bool enabled) {
            bool notFound = true;
            foreach (var node in addonListBaseNode.Value) {
                if (name == node.Key) {
                    if (node is AcfFile.LeafNode leaf) {
                        leaf.Value = enabled ?
                            CommonConsts.StringRepresentationForTrue : CommonConsts.StringRepresentationForFalse;
                        notFound = false;
                    } else
                        addonListBaseNode.Value.Remove(node);
                    break;
                }
            }
            if (notFound) {
                var leaf = new AcfFile.LeafNode(name, enabled ?
                    CommonConsts.StringRepresentationForTrue : CommonConsts.StringRepresentationForFalse,
                    addonListBaseNode);
                addonListBaseNode.Add(leaf);
            }
            dirty = true;
        }

        public void RemoveAddonEnabledState(string name) {
            foreach (var node in addonListBaseNode.Value) {
                if (name == node.Key) {
                    addonListBaseNode.Value.Remove(node);
                    break;
                }
            }
        }

        public void SaveToFile() {
            if (dirty) {
                File.WriteAllText(filePath, addonList.ToString(), Encoding.Default);
                dirty = false;
            }
        }

        public class LoadingException : Exception { }

    }

}
