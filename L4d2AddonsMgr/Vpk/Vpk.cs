using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using L4d2AddonsMgr.AcfFileSpace;

/*
 * Based on https://github.com/ValvePython/vpk
 * https://forum.xentax.com/viewtopic.php?f=10&t=11208
 * 
 * To understand `struct.unpack`s in the python script https://docs.python.org/3/library/struct.html
 */
[assembly: InternalsVisibleTo("L4d2AddonsMgrTest")]
namespace L4d2AddonsMgr.VpkSpace {

    internal partial class Vpk {

        private readonly FileInfo fileInfo;
        private VpkHeader header;

        // Ahh, dictionary.
        // https://stackoverflow.com/questions/1273139/c-sharp-java-hashmap-equivalent
        public Dictionary<string, Dictionary<string, Dictionary<string, VpkContainedFileDescription>>> Vindex { get; }

        public static Vpk OpenFile(FileInfo fileInfo) {
            var ret = new Vpk(fileInfo);
            using (var file = fileInfo.OpenRead()) {
                ret.ReadHeader(file);
                ret.ReadIndex(file);
            }
            return ret;
        }

        private static string JoinName(string name, string ext) {
            if (ext.Length > 0 && ext != " ")
                name = String.Format("{0}.{1}", name, ext);
            return name;
        }

        private static string JoinPath(string path, string name, string ext) {
            return Path.Combine(path, JoinName(name, ext));
        }

        private Vpk(FileInfo fileInfo) {
            this.fileInfo = fileInfo;
            header = new VpkHeader();
            Vindex = new Dictionary<string, Dictionary<string,
                Dictionary<string, VpkContainedFileDescription>>>();
        }

        public string GetContainedFileText(VpkContainedFileDescription description) {
            using (var reader = new StreamReader(
                // This way. The file can be in UTF8 with or w/o BOM and UTF16.
                // https://stackoverflow.com/questions/3545402/any-difference-between-file-readalltext-and-using-a-streamreader-to-read-file
                // https://stackoverflow.com/questions/11701341/encoding-utf8-getstring-doesnt-take-into-account-the-preamble-bom
                new VpkContainedFileStream(description, fileInfo), Encoding.UTF8, true)) {
                return reader.ReadToEnd();
            }
        }

        public string GetContainedFileText(string path, string name, string extensionName) {
            using (var reader = new StreamReader(
                GetContainedFileStream(path, name, extensionName), Encoding.UTF8, true)) {
                return reader.ReadToEnd();
            }
        }

        public byte[] GetContainedFileBytes(VpkContainedFileDescription description) {
            var len = description.meta.fileLength + description.meta.preloadLength;
            using (var stream = new VpkContainedFileStream(description, fileInfo)) {
                byte[] arr = new byte[len];
                stream.Read(arr, 0, len);
                return arr;
            }
        }

        public byte[] GetContainedFileBytes(string path, string name, string extensionName) {
            return GetContainedFileBytes(GetIndexerOf(path, name, extensionName));
        }

        public Stream GetContainedFileStream(string path, string name, string extensionName) {
            return new VpkContainedFileStream(GetIndexerOf(path, name, extensionName), fileInfo);
        }

        private VpkContainedFileDescription GetIndexerOf(
            string path, string name, string extensionName) => Vindex[extensionName][path][name];

        private void ReadHeader(FileStream file) {

            byte[] arr = new byte[16];
            int readLen = file.Read(arr, 0, 12);
            if (readLen != 12) {

                // Debug writeLine? Okey.
                // https://www.google.com/search?q=c%23+log+debug+message
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.debug.write?view=netframework-4.8

                // Format string instead of sprintf.. It's more like java or more like c++.. "Managed code" langs are alike right?
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/9e6481a6-6ff3-4389-81a2-e6d23607859f/sprintf-equivalent-in-c?forum=wpf
                Debug.WriteLine(String.Format(
                    "ERROR: File reading: Expecting length {0}, got {1}.",
                    12, readLen));
                throw new VpkException(VpkException.ExceptionReason.Unknown);
            }

            uint headerMarker = BitConverter.ToUInt32(arr, 0);
            header.version = BitConverter.ToInt32(arr, 4);
            header.directorySize = BitConverter.ToInt32(arr, 8);

            if (headerMarker != VpkHeader.HeaderMarker) {
                Debug.WriteLine(String.Format(
                    // For hex, this helped.
                    // https://stackoverflow.com/questions/11618387/string-format-for-hex
                    "ERROR: Expecting header marker 0x{0:X}, got 0x{1:X}",
                    VpkHeader.HeaderMarker, headerMarker));
                throw new VpkException(VpkException.ExceptionReason.WrongHeaderMarker);
            }

            if (header.version == 1)
                header.headerSize = 12;
            else if (header.version == 2) {
                header.headerSize = 28;
                Debug.WriteLine("Warning: Found vpk version 2 of which the support has not been tested yet.");
                readLen = file.Read(arr, 0, 16);
                if (readLen != 16) {
                    Debug.WriteLine(String.Format(
                        "ERROR: File reading: Expecting length {0}, got {1}.",
                        16, readLen));
                    throw new VpkException(VpkException.ExceptionReason.Unknown);
                }
                // This helped.
                // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/how-to-convert-a-byte-array-to-an-int
                header.embeddedChunkSize = BitConverter.ToInt32(arr, 0);
                header.chunkHashesSize = BitConverter.ToInt32(arr, 4);
                header.selfHashesSize = BitConverter.ToInt32(arr, 8);
                header.signatureSize = BitConverter.ToInt32(arr, 12);
                throw new VpkException(VpkException.ExceptionReason.UnsupportedVersion);
            } else {
                Debug.WriteLine(String.Format("ERROR: Unknown vpk version {0}.", header.version));
                throw new VpkException(VpkException.ExceptionReason.UnknownVersion);
            }

        }

        private void ReadIndex(FileStream file) {

            var arr = new byte[header.directorySize];
            int readLen = file.Read(arr, 0, header.directorySize);
            if (readLen != header.directorySize) {
                Debug.WriteLine(String.Format(
                        "ERROR: File reading: Expecting length {0}, got {1}.",
                        header.directorySize, readLen));
                throw new VpkException(VpkException.ExceptionReason.Unknown);
            }
            try {
                for (int i = 0; i < readLen;) {
                    // Ahh, ref.Cannot imagine whatif it's in java.
                    // https://www.c-sharpcorner.com/article/ref-keyword-in-c-sharp/
                    // https://stackoverflow.com/questions/186891/why-use-the-ref-keyword-when-passing-an-object
                    var extensionName = CStringUtil.ReadCString(arr, ref i);
                    if (extensionName.Length == 0) break;
                    var extensionNode = new Dictionary<string, Dictionary<string, VpkContainedFileDescription>>();
                    Vindex[extensionName] = extensionNode;
                    i = ReadUnderExtensionName(arr, i, extensionNode);
                }
            } catch (InvalidOperationException) {
                Debug.WriteLine(
                    "ERROR: File index parsing: Reading null-terminated string out of bound."
                   );
                throw new VpkException(VpkException.ExceptionReason.Unknown);
            }
        }

        private int ReadUnderExtensionName(byte[] buffer, int offset,
            Dictionary<string, Dictionary<string, VpkContainedFileDescription>> extensionNode) {
            int maxLen = buffer.Length;
            while (offset < maxLen) {
                var path = CStringUtil.ReadCString(buffer, ref offset);
                if (path.Length == 0) break;
                if (path == " ") path = "";
                var pathNode = new Dictionary<string, VpkContainedFileDescription>();
                extensionNode[path] = pathNode;
                offset = ReadUnderPath(buffer, offset, pathNode);
            }
            return offset;
        }

        private int ReadUnderPath(byte[] buffer, int offset, Dictionary<string, VpkContainedFileDescription> pathNode) {
            int maxLen = buffer.Length;
            while (offset < maxLen) {
                var name = CStringUtil.ReadCString(buffer, ref offset);
                if (name.Length == 0) break;
                //name = JoinPath(path, name, extensionName);
                var fileNode = new VpkContainedFileDescription(name);
                pathNode[name] = fileNode;
                offset = ReadFileIndexerOfName(buffer, offset, fileNode);
            }
            return offset;
        }

        private int ReadFileIndexerOfName(byte[] buffer, int offset, VpkContainedFileDescription fileNode) {
            fileNode.meta.crc32 = BitConverter.ToUInt32(buffer, offset);
            offset += 4;
            fileNode.meta.preloadLength = BitConverter.ToInt16(buffer, offset);
            Debug.WriteLineIf(fileNode.meta.preloadLength != 0, fileNode.meta.preloadLength);
            offset += 2;
            fileNode.meta.archieveIndex = BitConverter.ToInt16(buffer, offset);
            offset += 2;
            fileNode.meta.archieveOffset = BitConverter.ToInt32(buffer, offset);
            offset += 4;
            fileNode.meta.fileLength = BitConverter.ToInt32(buffer, offset);
            offset += 4;
            ushort suffix = BitConverter.ToUInt16(buffer, offset);
            if (suffix != ContainedFileMeta.Suffix) {
                Debug.WriteLine(String.Format(
                    "ERROR: File index parsing: Unexpected suffix while reading file meta from index, expecting {0:X}, got {1:X}.",
                    ContainedFileMeta.Suffix, suffix));
                throw new VpkException(VpkException.ExceptionReason.Unknown);
            }
            offset += 2;
            if (fileNode.meta.archieveIndex == 0x7fff)
                fileNode.meta.archieveOffset += header.headerSize + header.directorySize;
            fileNode.preload = new byte[fileNode.meta.preloadLength];
            Array.Copy(buffer, offset, fileNode.preload, 0, fileNode.meta.preloadLength);
            offset += fileNode.meta.preloadLength;
            return offset;
        }

    }
}
