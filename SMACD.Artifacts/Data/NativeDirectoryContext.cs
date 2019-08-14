using System;
using System.IO;
using System.IO.Compression;

namespace SMACD.Artifacts.Data
{
    public class NativeDirectoryContext : IDisposable
    {
        internal event EventHandler Disposing;

        internal byte[] StoredData { get; private set; } = new byte[0];

        /// <summary>
        /// Directory path allocated by this Context to store files and directories
        ///   in; contents will be compressed in full upon Context disposal
        /// </summary>
        public string Directory { get; }

        /// <summary>
        /// Directory path and a filename concatenated together
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns></returns>
        public string DirectoryWithFile(string fileName)
        {
            return Path.Combine(Directory, fileName);
        }

        internal NativeDirectoryContext(byte[] data) : this()
        {
            StoredData = data;
            if (StoredData != null && StoredData.Length > 0)
            {
                string zipFile = Path.GetTempFileName();
                File.WriteAllBytes(zipFile, StoredData);
                ZipFile.ExtractToDirectory(zipFile, Directory);
                File.Delete(zipFile);
            }
        }
        internal NativeDirectoryContext()
        {
            Directory = Path.Combine(Path.GetTempPath(), "wks", new Random((int)DateTime.Now.Ticks).Next(1024, int.MaxValue).ToString());
            System.IO.Directory.CreateDirectory(Directory);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                string target = Path.GetTempFileName();
                File.Delete(target);
                ZipFile.CreateFromDirectory(Directory, target);
                StoredData = File.ReadAllBytes(target);
                File.Delete(target);
                Disposing?.Invoke(this, new EventArgs());
                disposedValue = true;
            }
        }

        ~NativeDirectoryContext() => Dispose(false);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
