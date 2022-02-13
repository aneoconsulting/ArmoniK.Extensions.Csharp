using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.Common
{
  public interface IFileAdaptater
  {
    public void DownloadFile(string sourceFilePath, string destFilePath);
  }
}
