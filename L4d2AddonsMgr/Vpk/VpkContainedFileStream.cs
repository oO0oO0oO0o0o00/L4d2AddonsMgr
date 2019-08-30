using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace L4d2AddonsMgr.VpkSpace {

    internal partial class Vpk {

        /*
         * Use stream.
         * https://stackoverflow.com/questions/8119631/is-there-a-c-sharp-equivalent-way-for-java-inputstream-and-outputstream
         * https://stackoverflow.com/questions/4367539/what-is-the-difference-between-reader-and-inputstream
         */
        class VpkContainedFileStream : Stream {

            private VpkContainedFileDescription desc;

            private FileStream fileStream;

            public override bool CanRead => fileStream.CanRead;

            public override bool CanSeek => fileStream.CanSeek;

            public override bool CanWrite => false;

            public override long Length => desc.meta.fileLength + desc.meta.preloadLength;

            private int position;

            public VpkContainedFileStream(VpkContainedFileDescription desc, FileInfo fileInfo) {
                this.desc = desc;
                fileStream = fileInfo.OpenRead();
                fileStream.Position = desc.meta.archieveOffset;
                position = 0;
            }

            public override long Position {
                get => position; set {
                    position = (int)value;
                    if (position > desc.meta.preloadLength)
                        fileStream.Position = Math.Min(value - desc.meta.preloadLength, desc.meta.fileLength)
                            + desc.meta.archieveOffset;
                    else fileStream.Position = desc.meta.archieveOffset;
                }
            }

            public override void Flush() {
                throw new InvalidOperationException();
            }

            public override int Read(byte[] buffer, int offset, int count) {
                int n;
                if (position < desc.meta.preloadLength) {
                    n = Math.Min(desc.meta.preloadLength - position, count);
                    Array.Copy(desc.preload, position, buffer, offset, n);
                    offset += n;
                    count -= n;
                } else n = 0;
                if (count > 0)
                    n += fileStream.Read(buffer, offset,
                        Math.Min(count, desc.meta.fileLength + desc.meta.archieveOffset - (int)fileStream.Position));
                position += n;
                return n;
            }

            public override long Seek(long offset, SeekOrigin origin) {
                throw new NotImplementedException();
            }

            public override void SetLength(long value) {
                throw new InvalidOperationException();
            }

            public override void Write(byte[] buffer, int offset, int count) {
                throw new InvalidOperationException();
            }

            public override void Close() {
                // Close and dispose?
                // https://stackoverflow.com/questions/7524903/should-i-call-close-or-dispose-for-stream-objects
                // https://stackoverflow.com/questions/10984336/using-statement-vs-idisposable-dispose
                Dispose(true);
            }

            protected override void Dispose(bool disposing) {
                fileStream.Close();
                base.Dispose(disposing);
            }
        }
    }
}
