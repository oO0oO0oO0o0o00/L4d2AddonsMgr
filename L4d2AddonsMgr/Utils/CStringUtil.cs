using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace L4d2AddonsMgr {

    static class CStringUtil {

        /*
         * Good suggestions.
         * https://stackoverflow.com/questions/11713878/reading-a-null-terminated-string
         * https://social.msdn.microsoft.com/Forums/vstudio/en-US/f050b3f9-03bc-4fd2-bbcf-af4b45cc839f/convert-null-terminated-char-to-string-how-?forum=netfxbcl
         * TODO: Not just UTF8.
         */
        public static string ReadCString(byte[] arr, ref int offset) {
            for (int i = offset; i < arr.Length; i++) {
                if (arr[i] == 0) {
                    var ret = Encoding.UTF8.GetString(arr, offset, i - offset);
                    offset = i + 1;
                    return ret;
                }
            }
            throw new InvalidOperationException();
        }

    }
}
