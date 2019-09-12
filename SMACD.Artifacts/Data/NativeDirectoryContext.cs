﻿using System;
using System.IO;
using System.IO.Compression;

namespace SMACD.Artifacts.Data
{
    public class NativeDirectoryContext : IDisposable
    {
        internal NativeDirectoryContext(byte[] data) : this()
        {
            StoredData = data;
            if (StoredData != null && StoredData.Length > 0)
            {
                var zipFile = Path.GetTempFileName();
                File.WriteAllBytes(zipFile, StoredData);
                ZipFile.ExtractToDirectory(zipFile, Directory);
                File.Delete(zipFile);
            }
        }

        internal NativeDirectoryContext()
        {
            Directory = Path.Combine(Path.GetTempPath(), "wks",
                new Random((int) DateTime.Now.Ticks).Next(1024, int.MaxValue).ToString());
            System.IO.Directory.CreateDirectory(Directory);
        }

        internal byte[] StoredData { get; private set; } = new byte[0];

        /// <summary>
        ///     Directory path allocated by this Context to store files and directories
        ///     in; contents will be compressed in full upon Context disposal
        /// </summary>
        public string Directory { get; }

        internal event EventHandler Disposing;

        /// <summary>
        ///     Directory path and a filename concatenated together
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns></returns>
        public string DirectoryWithFile(string fileName)
        {
            return Path.Combine(Directory, fileName);
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                var target = Path.GetTempFileName();
                File.Delete(target);
                ZipFile.CreateFromDirectory(Directory, target);
                StoredData = File.ReadAllBytes(target);
                File.Delete(target);
                Disposing?.Invoke(this, new EventArgs());
                disposedValue = true;
            }
        }

        ~NativeDirectoryContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}