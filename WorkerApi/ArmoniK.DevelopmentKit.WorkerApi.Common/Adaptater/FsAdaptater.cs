using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common.Adaptater
{
  public class FsAdaptater : IFileAdaptater
  {
    public FsAdaptater(string sourceDirPath, string localZipDir = "/tmp/packages/zip")
    {
      SourceDirPath      = sourceDirPath;
      DestinationDirPath = localZipDir;

      if (!Directory.Exists(DestinationDirPath))
      {
        Directory.CreateDirectory(DestinationDirPath);
      }
    }

    public string DestinationDirPath { get; set; }

    private string SourceDirPath { get; set; }

    /// <summary>
    /// The getter to retrieve the last downloaded file
    /// </summary>
    public string DestinationFullPath { get; set; }

    public string DownloadFile(string fileName)
    {
      DestinationFullPath = Path.Combine(DestinationDirPath,
                                         fileName);

      try
      {
        File.Copy(Path.Combine(SourceDirPath,
                               fileName),
                  DestinationFullPath);
      }
      catch (Exception ex)
      {
        throw new WorkerApiException($"Fail to copy {fileName} from [{SourceDirPath}/{fileName}] to [{DestinationFullPath}]",
                                     ex);
      }

      return DestinationFullPath;
    }
  }
}