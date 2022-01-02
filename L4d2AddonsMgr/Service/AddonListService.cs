using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace L4d2AddonsMgr.Service {

    public static class AddonListService {

        public static AddonsListTxt GetAddonsList(string gameDir) {
            try {
                return new AddonsListTxt(gameDir);
            } catch (Exception ex) {
                Debug.WriteLine(ex);
                //MsgBox("读取附加组件配置文件失败，为避免数据丢失，部分功能将不可用。" +
                //        "建议您检查并更正addonlist.txt中的格式错误，或在线寻求帮助。");
                throw new Exception(Exception.Reason.AddonsListNotFound);
            }
        }

        public class Exception : System.Exception {

            public Reason TheReason {
                get;
            }

            public Exception(Reason reason) => TheReason = reason;

            public enum Reason {
                AddonsListNotFound
            }

        }
    }
}
