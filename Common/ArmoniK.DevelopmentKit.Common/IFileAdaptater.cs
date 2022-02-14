using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.Common
{
  /// <summary>
  /// The interface to download file 
  /// </summary>
  public interface IFileAdaptater
  {
    /// <summary>
    /// Get The directory where the file will be downloaded
    /// </summary>
    public string DestinationDirPath { get; set; }
    /// <summary>
    /// The method to download file from form remote server
    /// </summary>
    /// <param name="fileName">The filename with extension and without directory path</param>
    /// <returns>Returns the path where the file has been downloaded</returns>
    public string DownloadFile(string fileName);
  }
}
