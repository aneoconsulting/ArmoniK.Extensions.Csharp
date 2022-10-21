// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;

namespace ArmoniK.DevelopmentKit.Worker.Common;

public class ZipArchiver
{
  private static readonly string RootAppPath = "/tmp/packages";

  /// <summary>
  /// </summary>
  /// <param name="assemblyNameFilePath"></param>
  /// <returns></returns>
  public static bool IsZipFile(string assemblyNameFilePath)
  {
    //ATm ONLY Check the extensions 

    var extension = Path.GetExtension(assemblyNameFilePath);
    if (extension?.ToLower() == ".zip")
    {
      return true;
    }

    return false;
  }

  /// <summary>
  /// </summary>
  /// <param name="assemblyNameFilePath"></param>
  /// <returns></returns>
  /// <exception cref="WorkerApiException"></exception>
  public static IEnumerable<string> ExtractNameAndVersion(string assemblyNameFilePath)
  {
    string filePathNoExt;
    string appName;
    string versionName;

    try
    {
      filePathNoExt = Path.GetFileNameWithoutExtension(assemblyNameFilePath);
    }
    catch (ArgumentException e)
    {
      throw new WorkerApiException(e);
    }

    // Instantiate the regular expression object.
    var pat = @"(.*)-v(.+)";

    var r = new Regex(pat,
                      RegexOptions.IgnoreCase);

    var m = r.Match(filePathNoExt);

    if (m.Success)
    {
      appName = m.Groups[1]
                 .Value;
      versionName = m.Groups[2]
                     .Value;
    }
    else
    {
      throw new WorkerApiException("File name format doesn't match");
    }

    return new[]
           {
             appName,
             versionName,
           };
  }

  public static string GetLocalPathToAssembly(string pathToZip)
  {
    var assemblyInfo    = ExtractNameAndVersion(pathToZip);
    var info            = assemblyInfo as string[] ?? assemblyInfo.ToArray();
    var assemblyName    = info.ElementAt(0);
    var assemblyVersion = info.ElementAt(1);
    var basePath        = $"{RootAppPath}/{assemblyName}/{assemblyVersion}";

    return $"{basePath}/{assemblyName}.dll";
  }

  /// <summary>
  /// </summary>
  /// <param name="fileAdapter"></param>
  /// <param name="fileName"></param>
  /// <param name="waitForArchiver"></param>
  /// <returns></returns>
  /// <exception cref="WorkerApiException"></exception>
  public static bool ArchiveAlreadyExtracted(IFileAdapter fileAdapter,
                                             string         fileName,
                                             int            waitForArchiver = 300)
  {
    var assemblyInfo = ExtractNameAndVersion(Path.Combine(fileAdapter.DestinationDirPath,
                                                          fileName));
    var info            = assemblyInfo as string[] ?? assemblyInfo.ToArray();
    var assemblyName    = info.ElementAt(0);
    var assemblyVersion = info.ElementAt(1);
    var basePath        = $"{RootAppPath}/{assemblyName}/{assemblyVersion}";

    //Download File
    if (!File.Exists(Path.Combine(fileAdapter.DestinationDirPath,
                                  fileName)))
    {
      fileAdapter.DownloadFile(fileName);
    }


    if (!Directory.Exists($"{RootAppPath}/{assemblyName}/{assemblyVersion}"))
    {
      return false;
    }

    //Now at least if dll exists or if a lock file exists and wait for unlock
    if (File.Exists($"{basePath}/{assemblyName}.dll"))
    {
      return true;
    }

    if (!File.Exists($"{basePath}/{assemblyName}.lock"))
    {
      throw new FileNotFoundException($"Cannot find Service. Assembly name {basePath}/{assemblyName}.dll");
    }

    var retry       = 0;
    var loopingWait = 2; // 2 secs

    if (waitForArchiver == 0)
    {
      return true;
    }

    while (!File.Exists($"{basePath}/{assemblyName}.lock"))
    {
      Thread.Sleep(loopingWait * 1000);
      retry++;
      if (retry > waitForArchiver >> 2)
      {
        throw new WorkerApiException($"Wait for unlock unzip was timeout after {waitForArchiver * 2} seconds");
      }
    }

    return false;
  }

  /// <summary>
  ///   Unzip Archive if the temporary folder doesn't contain the
  ///   folder convention path should exist in /tmp/packages/{AppName}/{AppVersion/AppName.dll
  /// </summary>
  /// <param name="fileAdapter">
  ///   The path to the zip file
  ///   Pattern for zip file has to be {AppName}-v{AppVersion}.zip
  /// </param>
  /// <param name="fileName"></param>
  /// <returns>return string containing the path to the client assembly (.dll) </returns>
  public static string UnzipArchive(IFileAdapter fileAdapter,
                                    string         fileName)
  {
    if (!IsZipFile(fileName))
    {
      throw new WorkerApiException("Cannot yet extract or manage raw data other than zip archive");
    }

    var assemblyInfo    = ExtractNameAndVersion(fileName);
    var info            = assemblyInfo as string[] ?? assemblyInfo.ToArray();
    var assemblyVersion = info.ElementAt(1);
    var assemblyName    = info.ElementAt(0);


    var pathToAssembly    = $"{RootAppPath}/{assemblyName}/{assemblyVersion}/{assemblyName}.dll";
    var pathToAssemblyDir = $"{RootAppPath}/{assemblyName}/{assemblyVersion}";

    if (ArchiveAlreadyExtracted(fileAdapter,
                                fileName,
                                20))
    {
      return pathToAssembly;
    }

    if (!Directory.Exists(pathToAssemblyDir))
    {
      Directory.CreateDirectory(pathToAssemblyDir);
    }

    var lockFileName = $"{pathToAssemblyDir}/{assemblyName}.lock";


    using (var fileStream = new FileStream(lockFileName,
                                           FileMode.OpenOrCreate,
                                           FileAccess.ReadWrite,
                                           FileShare.ReadWrite))
    {
      var lockfileForExtractionString = "Lockfile for extraction";

      var unicodeEncoding = new UnicodeEncoding();
      var textLength      = unicodeEncoding.GetByteCount(lockfileForExtractionString);

      if (fileStream.Length == 0)
        //Try to lock file to protect extraction
      {
        fileStream.Write(new UnicodeEncoding().GetBytes(lockfileForExtractionString),
                         0,
                         unicodeEncoding.GetByteCount(lockfileForExtractionString));
      }

      try
      {
        fileStream.Lock(0,
                        textLength);
      }
      catch (IOException)
      {
        return pathToAssembly;
      }
      catch (Exception e)
      {
        throw new WorkerApiException(e);
      }


      try
      {
        ZipFile.ExtractToDirectory(Path.Combine(fileAdapter.DestinationDirPath,
                                                fileName),
                                   RootAppPath);
      }
      catch (Exception e)
      {
        throw new WorkerApiException(e);
      }
      finally
      {
        fileStream.Unlock(0,
                          textLength);
      }
    }

    File.Delete(lockFileName);

    //Check now if the assembly is present
    if (!File.Exists(pathToAssembly))
    {
      throw new WorkerApiException($"Fail to find assembly {pathToAssembly}. Something went wrong during the extraction. " +
                                   $"Please sure that tree folder inside is {assemblyName}/{assemblyVersion}/*.dll");
    }

    return pathToAssembly;
  }
}
