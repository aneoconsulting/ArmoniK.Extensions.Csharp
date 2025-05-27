// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2025. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
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
using System.IO;
using System.IO.Compression;

using ArmoniK.DevelopmentKit.Common.Exceptions;

namespace ArmoniK.DevelopmentKit.Worker.Common.Archive;

/// <summary>
///   Class used do handle zip archives
/// </summary>
public class ZipArchiver : IArchiver
{
  private readonly string rootAppPath_;

  /// <summary>
  ///   Creates a zip archive handler
  /// </summary>
  /// <param name="assembliesBasePath">Base path to extract zip files</param>
  public ZipArchiver(string assembliesBasePath)
    => rootAppPath_ = assembliesBasePath;

  /// <inheritdoc />
  public bool ArchiveAlreadyExtracted(PackageId packageId)
    => File.Exists(Path.Combine(rootAppPath_,
                                $".{packageId}.extracted"));

  /// <inheritdoc cref="IArchiver" />
  /// <exception cref="WorkerApiException">
  ///   Thrown if the file isn't a zip archive or if the lock file isn't lockable when the
  ///   archive is being extracted by another process
  /// </exception>
  public string ExtractArchive(string    filename,
                               PackageId packageId)
  {
    if (!IsZipFile(filename))
    {
      throw new WorkerApiException("Cannot yet extract or manage raw data other than zip archive");
    }

    var pathToAssemblyDir = Path.Combine(rootAppPath_,
                                         packageId.PackageSubpath);
    var signalPath = Path.Combine(rootAppPath_,
                                  $".{packageId}.extracted");

    if (File.Exists(signalPath))
    {
      return pathToAssemblyDir;
    }

    var extractPath = Path.Combine(rootAppPath_,
                                   $".{packageId}.{Guid.NewGuid()}");

    try
    {
      FileExt.TryCreateDirectory(extractPath);
    }
    catch (Exception e)
    {
      throw new WorkerApiException($"Cannot create extract directory {extractPath}",
                                   e);
    }

    try
    {
      try
      {
        ZipFile.ExtractToDirectory(filename,
                                   extractPath);
      }
      catch (Exception e)
      {
        throw new WorkerApiException($"Cannot extract archive {filename} to directory {extractPath}",
                                     e);
      }

      var extractedMainAssembly = Path.Combine(extractPath,
                                               packageId.PackageSubpath,
                                               packageId.MainAssemblyFileName);

      // Check now if the assembly is present
      if (!File.Exists(extractedMainAssembly))
      {
        throw new WorkerApiException($"Fail to find assembly {extractedMainAssembly}. The extracted zip should have the following structure" +
                                     $" {packageId.PackageSubpath}/ which will contains all the dll files.");
      }

      try
      {
        FileExt.MoveOrDeleteDirectory(extractPath,
                                      rootAppPath_);
      }
      catch (Exception e)
      {
        throw new WorkerApiException($"Cannot move extracted archive {filename} from directory {extractPath} to directory {rootAppPath_}",
                                     e);
      }
    }
    finally
    {
      FileExt.TryDeleteDirectory(extractPath);
    }

    try
    {
      File.CreateText(signalPath)
          .Dispose();
    }
    catch (Exception e)
    {
      if (!File.Exists(signalPath))
      {
        throw new WorkerApiException($"Cannot finalize extraction of {Path.Combine(pathToAssemblyDir, packageId.MainAssemblyFileName)}",
                                     e);
      }
    }

    return pathToAssemblyDir;
  }

  /// <summary>
  /// </summary>
  /// <param name="assemblyNameFilePath"></param>
  /// <returns></returns>
  public static bool IsZipFile(string assemblyNameFilePath)
  {
    // ATm ONLY Check the extensions

    var extension = Path.GetExtension(assemblyNameFilePath);
    return extension?.ToLower() == ".zip";
  }
}
