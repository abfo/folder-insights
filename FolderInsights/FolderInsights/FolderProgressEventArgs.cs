using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderInsights
{
    /// <summary>
    /// EventArgs fired when progress is available
    /// </summary>
    public class FolderProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Progress message
        /// </summary>
        public string Message { get; set; }
    }
}
