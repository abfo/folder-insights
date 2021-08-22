using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderInsights
{
    /// <summary>
    /// Default implementation of IFolderProgress
    /// </summary>
    public class FolderProgress : IFolderProgress
    {
        private readonly object _lock = new object();
        private bool _requestCancel = false;

        /// <summary>
        /// True if the folder scan should be canceled
        /// </summary>
        public bool RequestCancel
        {
            get
            {
                lock(_lock)
                {
                    return _requestCancel;
                }
            }
            set
            {
                lock(_lock)
                {
                    _requestCancel = value;
                }
            }
        }

        /// <summary>
        /// Event fired when progress is available
        /// </summary>
        public event EventHandler<FolderProgressEventArgs> Progress;

        /// <summary>
        /// Report progress
        /// </summary>
        /// <param name="progress">Progress message</param>
        public void ReportProgress(string progress)
        {
            lock (_lock)
            {
                Progress?.Invoke(this, new FolderProgressEventArgs { Message = progress });
            }
        }
    }
}
