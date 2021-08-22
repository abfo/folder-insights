using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderInsights
{
    /// <summary>
    /// Interface for reporting progress during a folder scan and requesting cancel of the scan
    /// </summary>
    public interface IFolderProgress
    {
        /// <summary>
        /// True if the folder scan should be canceled
        /// </summary>
        bool RequestCancel { get; }

        /// <summary>
        /// Event fired when progress is available
        /// </summary>
        event EventHandler<FolderProgressEventArgs> Progress;

        /// <summary>
        /// Report progress
        /// </summary>
        /// <param name="progress">Progress message</param>
        void ReportProgress(string progress);
    }
}
