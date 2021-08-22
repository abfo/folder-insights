using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderInsights
{
    /// <summary>
    /// Scans a folder and generates reports
    /// </summary>
    public class FolderScanner
    {
        private readonly IList<DirectoryInfo> _roots;
        private readonly IFolderProgress _progress;
        private readonly Dictionary<DateTime, long> _bytesByDate;
        private readonly Dictionary<string, long> _bytesByExtension;
        private readonly Dictionary<string, long> _bytesByFolder;
        private readonly string _dateReportPath;
        private readonly string _extensionReportPath;
        private readonly string _folderReportPath;
        private readonly long _folderReportMinBytes;
        private DateTime _startTimeUtc;

        /// <summary>
        /// Scans a folder and generates reports.
        /// </summary>
        /// <param name="roots">List of one or more root directories to scan</param>
        /// <param name="progress">Optional progress reporting and cancellation. FolderProgress provides a default implementation.</param>
        public FolderScanner(IList<DirectoryInfo> roots, 
            string dateReportPath,
            string extensionReportPath,
            string folderReportPath,
            long folderReportMinBytes = 0,
            IFolderProgress progress = null)
        {
            if (roots == null) { throw new ArgumentNullException("roots"); }
            if (roots.Count == 0) { throw new InvalidOperationException("roots must contain at least one DirectoryInfo"); }
            _roots = roots;

            if (dateReportPath == null) { throw new ArgumentNullException("dateReportPath"); }
            if (!Directory.Exists(Path.GetDirectoryName(dateReportPath))) { throw new DirectoryNotFoundException($"Directory not found for {dateReportPath}"); }
            _dateReportPath = dateReportPath;

            if (extensionReportPath == null) { throw new ArgumentNullException("extensionReportPath"); }
            if (!Directory.Exists(Path.GetDirectoryName(extensionReportPath))) { throw new DirectoryNotFoundException($"Directory not found for {extensionReportPath}"); }
            _extensionReportPath = extensionReportPath;

            if (folderReportPath == null) { throw new ArgumentNullException("folderReportPath"); }
            if (!Directory.Exists(Path.GetDirectoryName(folderReportPath))) { throw new DirectoryNotFoundException($"Directory not found for {folderReportPath}"); }
            _folderReportPath = folderReportPath;

            _folderReportMinBytes = folderReportMinBytes;

            // can be null
            _progress = progress;

            _bytesByDate = new Dictionary<DateTime, long>();
            _bytesByExtension = new Dictionary<string, long>();
            _bytesByFolder = new Dictionary<string, long>();
        }

        /// <summary>
        /// Start the scan
        /// </summary>
        public void Scan()
        {
            _startTimeUtc = DateTime.UtcNow;
            ReportProgress("FolderScanner Starting");

            _bytesByDate.Clear();
            _bytesByExtension.Clear();
            _bytesByFolder.Clear();

            foreach(DirectoryInfo root in _roots)
            {
                ScanRootFolder(root);
            }

            if (CancelRequested) { return; }
            ReportProgress("Writing size by extension report");
            using (FileStream fs = File.Create(_extensionReportPath))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write("Extension,GB\r\n");
                    foreach (KeyValuePair<string, long> kvp in _bytesByExtension)
                    {
                        sw.Write($"{kvp.Key},{BytesToGB(kvp.Value)}\r\n");
                    }
                }
            }

            if (CancelRequested) { return; }
            ReportProgress("Writing size by date report");
            using (FileStream fs = File.Create(_dateReportPath))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write("Date,GB\r\n");
                    foreach (KeyValuePair<DateTime, long> kvp in _bytesByDate)
                    {
                        sw.Write($"{kvp.Key:yyyy-MM-dd},{BytesToGB(kvp.Value)}\r\n");
                    }
                }
            }

            if (CancelRequested) { return; }
            ReportProgress("Writing size by folder report");
            using (FileStream fs = File.Create(_folderReportPath))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write("Folder,GB\r\n");
                    foreach (KeyValuePair<string, long> kvp in _bytesByFolder)
                    {
                        if (kvp.Value >= _folderReportMinBytes)
                        {
                            sw.Write($"{kvp.Key},{BytesToGB(kvp.Value)}\r\n");
                        }
                    }
                }
            }

            ReportProgress("Done");
        }

        private void ScanRootFolder(DirectoryInfo root)
        {
            if (CancelRequested) { return; }

            ReportProgress($"Building folder list for {root.FullName}");

            List<DirectoryInfo> folders = new List<DirectoryInfo>();
            BuildFolderList(root, folders);

            // scan root as child
            ScanChildFolder(root);

            // scan each child
            foreach(DirectoryInfo child in folders)
            {
                ScanChildFolder(child);
                if (CancelRequested) { return; }
            }
        }

        private void ScanChildFolder(DirectoryInfo folder)
        {
            if (CancelRequested) { return; }

            ReportProgress($"Scanning files in {folder.FullName}");

            if (!_bytesByFolder.ContainsKey(folder.FullName)) { _bytesByFolder.Add(folder.FullName, 0); }

            FileInfo[] files = folder.GetFiles();
            foreach(FileInfo file in files)
            {
                if (CancelRequested) { return; }

                // entire folder size
                _bytesByFolder[folder.FullName] += file.Length;

                // earliest date size
                DateTime earliest = file.LastWriteTimeUtc;
                if (file.CreationTimeUtc < earliest)
                {
                    earliest = file.CreationTimeUtc;
                }

                if (!_bytesByDate.ContainsKey(earliest.Date)) { _bytesByDate.Add(earliest.Date, 0); }
                _bytesByDate[earliest.Date] += file.Length;

                // extension size
                string extension = file.Extension;
                if (!_bytesByExtension.ContainsKey(extension)) { _bytesByExtension.Add(extension, 0); }
                _bytesByExtension[extension] += file.Length;
            }
        }

        private static void BuildFolderList(DirectoryInfo root, List<DirectoryInfo> folderList)
        {
            folderList.Add(root);
            DirectoryInfo[] folders = root.GetDirectories();
            foreach (DirectoryInfo folder in folders)
            {
                if (folder.Attributes.HasFlag(FileAttributes.ReparsePoint)) { continue; }
                BuildFolderList(folder, folderList);
            }
        }

        private decimal BytesToGB(long bytes)
        {
            return (decimal)bytes / 1073741824.0M;
        }

        private void ReportProgress(string message)
        {
            if (_progress != null)
            {
                TimeSpan elapsed = DateTime.UtcNow - _startTimeUtc;
                _progress.ReportProgress($"{DateTime.Now:HH:mm:ss} {elapsed:hh\\:mm\\:ss} - {message}");
            }
        }

        private bool CancelRequested
        {
            get
            {
                return _progress != null && _progress.RequestCancel;
            }
        }
    }
}
