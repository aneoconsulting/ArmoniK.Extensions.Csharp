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
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Worker.DLLWorker;

/// <summary>
///   Manages application packages
/// </summary>
public class ApplicationPackageManager
{
  private readonly string       archivePath_;
  private readonly IArchiver    archiver_;
  private readonly string       assembliesSearchPath_;
  private readonly IFileAdapter fileAdapter_;
  private readonly ILogger      logger_;

  /// <summary>
  ///   Creates an application package manager
  /// </summary>
  /// <param name="configuration">DLLWorker configuration</param>
  /// <param name="loggerFactory">Logger factory</param>
  /// <exception cref="WorkerApiException">Thrown when the FileStorageType is unspecified in the configuration</exception>
  public ApplicationPackageManager(IConfiguration configuration,
                                   ILoggerFactory loggerFactory)
  {
    assembliesSearchPath_ = configuration[AppsOptions.GridAssemblyPathKey] ?? "/tmp/assemblies";
    archivePath_          = configuration[AppsOptions.GridZipVolumePath]   ?? "/data";
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
    logger_   = loggerFactory.CreateLogger<ApplicationPackageManager>();
  }

  /// <summary>
  ///   Loads the application package. If the package is already loaded just returns its base path.
  /// </summary>
  /// <param name="packageId">Package Id</param>
  /// <returns>Path to the application package</returns>
  [CanBeNull]
  public string LoadApplicationPackage(PackageId packageId)
  {
    var localFile = GetApplicationAssemblyFile(packageId,
                                               packageId.MainAssemblyFileName);
    if (localFile != null)
    {
      logger_.LogDebug("Package {packageId} is already loaded",
                       packageId);
      // Package is already loaded
      return Path.GetDirectoryName(localFile);
    }

    // Try to get the local zip, download it if it doesn't exist
    var localZip = GetLocalApplicationZip(packageId) ?? fileAdapter_.DownloadFile(packageId.ZipFileName);

    if (!archiver_.ArchiveAlreadyExtracted(packageId))
    {
      logger_.LogInformation("Extracting {packageId} from archive {localZip}",
                             packageId,
                             localZip);
      var extractedPath = archiver_.ExtractArchive(localZip,
                                                   packageId);
      logger_.LogInformation("Package {packageId} successfully extracted from {localZip}",
                             packageId,
                             localZip);
      return extractedPath;
    }

    // Get the directory where the main assembly is located
    return Path.GetDirectoryName(GetApplicationAssemblyFile(packageId,
                                                            packageId.MainAssemblyFileName));
  }

  /// <summary>
  ///   Get the path to the given assembly of the package
  /// </summary>
  /// <param name="packageId">PackageId</param>
  /// <param name="assemblyName">Name of the assembly</param>
  /// <param name="searchPaths">
  ///   List of search paths for the assembly, defaults to the usual package location if not
  ///   specified
  /// </param>
  /// <returns>Path to the assembly in the package, null if it cannot be found</returns>
  [CanBeNull]
  public string GetApplicationAssemblyFile(PackageId            packageId,
                                           string               assemblyName,
                                           [CanBeNull] string[] searchPaths = null)
    => (searchPaths?.AsEnumerable()
                   .Select(path => Path.Combine(path,
                                                assemblyName)) ?? new[]
                                                                  {
                                                                    Path.Combine(assembliesSearchPath_,
                                                                                 assemblyName),
                                                                    Path.Combine(assembliesSearchPath_,
                                                                                 packageId.PackageSubpath,
                                                                                 assemblyName),
                                                                  }).FirstOrDefault(File.Exists);

  /// <summary>
  ///   Get the path to the local zip file for the package
  /// </summary>
  /// <param name="packageId">PackageId</param>
  /// <returns>Path to the zip file, null if it cannot be found</returns>
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
