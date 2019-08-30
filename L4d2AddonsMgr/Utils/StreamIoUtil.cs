using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace L4d2AddonsMgr {
    public static class StreamIoUtil {

        public static string ReadAllText(Stream inputStream) {
            StreamReader sr = new StreamReader(inputStream, false);
            return sr.ReadToEnd();
        }

    }
}
