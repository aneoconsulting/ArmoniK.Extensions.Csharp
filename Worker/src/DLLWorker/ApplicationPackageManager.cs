// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
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

using System.IO;
using System.Linq;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.Worker.Common;
using ArmoniK.DevelopmentKit.Worker.Common.Adapter;
using ArmoniK.DevelopmentKit.Worker.Common.Archive;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;

namespace ArmoniK.DevelopmentKit.Worker.DLLWorker;

public class ApplicationPackageManager
{
  private readonly string       archivePath_;
  private readonly IArchiver    archiver_;
  private readonly string       assembliesSearchPath_;
  private readonly IFileAdapter fileAdapter_;

  public ApplicationPackageManager(IConfiguration configuration)
  {
    assembliesSearchPath_ = configuration[AppsOptions.GridAssemblyPathKey] ?? "/tmp/assemblies";
    archivePath_          = "/tmp/zip";
    switch (configuration["FileStorageType"])
    {
      case "FS":
        fileAdapter_ = new FsAdapter(configuration[AppsOptions.GridDataVolumesKey] ?? "/data",
                                     archivePath_);
        break;
      case "S3":
      {
        var configurationSection = configuration.GetSection("S3Storage");
        fileAdapter_ = new S3Adapter(configurationSection["ServiceURL"],
                                     configurationSection["BucketName"],
                                     configurationSection["AccessKeyId"],
                                     configurationSection["SecretAccessKey"],
                                     "",
                                     configurationSection.GetValue("MustForcePathStyle",
                                                                   false),
                                     archivePath_);
        break;
      }
      default:
        throw new WorkerApiException("Cannot find the FileStorageType in the IConfiguration. Please make sure you have properly set the field [FileStorageType]");
    }

    archiver_ = new ZipArchiver(assembliesSearchPath_);
  }

  [CanBeNull]
  public string LoadApplicationPackage(PackageId packageId)
  {
    var localFile = GetApplicationAssemblyFile(packageId,
                                               packageId.ApplicationName);
    if (localFile != null)
    {
      return Path.GetDirectoryName(localFile);
    }

    var localZip = GetLocalApplicationZip(packageId);
    if (localZip != null && !archiver_.ArchiveAlreadyExtracted(packageId))
    {
      archiver_.ExtractArchive(localZip,
                               packageId);
      return Path.GetDirectoryName(GetApplicationAssemblyFile(packageId,
                                                              packageId.ApplicationName));
    }

    var destinationZip = fileAdapter_.DownloadFile(packageId.ZipFileName);
    if (!archiver_.ArchiveAlreadyExtracted(packageId))
    {
      archiver_.ExtractArchive(destinationZip,
                               packageId);
    }

    return Path.GetDirectoryName(GetApplicationAssemblyFile(packageId,
                                                            packageId.ApplicationName));
  }

  [CanBeNull]
  public string GetApplicationAssemblyFile(PackageId            packageId,
                                           string               assemblyName,
                                           [CanBeNull] string[] searchPaths = null)
    => (searchPaths ?? new[]
                       {
                         Path.Combine(assembliesSearchPath_,
                                      assemblyName),
                         Path.Combine(assembliesSearchPath_,
                                      packageId.PackageSubpath,
                                      assemblyName),
                       }).FirstOrDefault(File.Exists);

  [CanBeNull]
  private string GetLocalApplicationZip(PackageId packageId)
  {
    var zipPath = Path.Combine(archivePath_,
                               packageId.ZipFileName);
    return File.Exists(zipPath)
             ? zipPath
             : null;
  }
}
