using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace L4d2AddonsMgr.Utils {

    internal class FileInodeComparer {

        // https://stackoverflow.com/questions/410705/best-way-to-determine-if-two-path-reference-to-same-file-in-c-sharp
        public struct ByHandleFileInformation {

            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out ByHandleFileInformation lpFileInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile([MarshalAs(UnmanagedType.LPTStr)] string filename,
          [MarshalAs(UnmanagedType.U4)] FileAccess access,
          [MarshalAs(UnmanagedType.U4)] FileShare share,
          IntPtr securityAttributes,
          [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
          [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
          IntPtr templateFile);

        public static bool IsSameFile(string path1, string path2) {
            using (var sfh1 = CreateFile(path1, FileAccess.Read, FileShare.ReadWrite,
                IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero)) {
                if (sfh1.IsInvalid)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                using (var sfh2 = CreateFile(path2, FileAccess.Read, FileShare.ReadWrite,
                  IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero)) {
                    if (sfh2.IsInvalid)
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                    var result1 = GetFileInformationByHandle(sfh1, out var fileInfo1);
                    if (!result1)
                        throw new IOException(string.Format("GetFileInformationByHandle has failed on {0}", path1));

                    var result2 = GetFileInformationByHandle(sfh2, out var fileInfo2);
                    if (!result2)
                        throw new IOException(string.Format("GetFileInformationByHandle has failed on {0}", path2));

                    return fileInfo1.VolumeSerialNumber == fileInfo2.VolumeSerialNumber
                      && fileInfo1.FileIndexHigh == fileInfo2.FileIndexHigh
                      && fileInfo1.FileIndexLow == fileInfo2.FileIndexLow;
                }
            }
        }
    }
}
