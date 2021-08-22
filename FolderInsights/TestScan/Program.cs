using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FolderInsights;

namespace TestScan
{
    class Program
    {
        static void Main(string[] args)
        {
            FolderProgress progress = new FolderProgress();
            progress.Progress += Progress_Progress;

            List<DirectoryInfo> roots = new List<DirectoryInfo> 
            { 
                new DirectoryInfo(@"C:\GitHub\shadow"),
                new DirectoryInfo(@"C:\Users\rob") 
            };

            FolderScanner scanner = new FolderScanner(roots,
                @"D:\FolderInsights\DateReport.csv",
                @"D:\FolderInsights\ExtensionReport.csv",
                @"D:\FolderInsights\FolderReport.csv",
                1073741824,
                progress);

            scanner.Scan();
        }

        private static void Progress_Progress(object sender, FolderProgressEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
