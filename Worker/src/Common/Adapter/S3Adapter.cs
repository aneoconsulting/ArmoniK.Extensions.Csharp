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
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.Utils;

namespace ArmoniK.DevelopmentKit.Worker.Common.Adapter;

public class S3Adapter : IFileAdapter
{
  public S3Adapter(string endPointRegion,
                   string bucketName,
                   string awsAccessKeyId,
                   string awsSecretAccessKey,
                   string remoteS3Path,
                   bool   mustForcePathStyle = false,
                   string localZipDir        = "/tmp/packages/zip")
  {
    var config = new AmazonS3Config
                 {
                   ServiceURL     = endPointRegion,
                   ForcePathStyle = mustForcePathStyle,
                 };

    BucketName   = bucketName;
    RemoteS3Path = remoteS3Path;
    LocalZipDir  = localZipDir;

    Client = string.IsNullOrEmpty(awsAccessKeyId)
               ? new AmazonS3Client(config)
               : new AmazonS3Client(awsAccessKeyId,
                                    awsSecretAccessKey,
                                    config);

    DestinationDirPath = localZipDir;

    if (!Directory.Exists(DestinationDirPath))
    {
      Directory.CreateDirectory(DestinationDirPath);
    }
  }

  private AmazonS3Config ConfigAmazonS3 { get; set; }

  private string LocalZipDir { get; }

  private string RemoteS3Path { get; }

  private string BucketName { get; }

  private string AwsSessionToken { get; set; }

  private AmazonS3Client Client { get; }

  /// <summary>
  ///   Get the directory where the file will be downloaded
  /// </summary>
  public string DestinationDirPath { get; set; }


  /// <summary>
  ///   The method to download file from form remote server
  /// </summary>
  /// <param name="fileName">The filename with extension and without directory path</param>
  /// <returns>Returns the path where the file has been downloaded</returns>
  public string DownloadFile(string fileName)
    => DownloadFileAsync(fileName)
      .WaitSync();

  /// <summary>
  ///   The method to download file from form remote server
  /// </summary>
  /// <param name="fileName">The filename with extension and without directory path</param>
  /// <param name="cancellationToken">Abort download of the file</param>
  /// <returns>Returns the path where the file has been downloaded</returns>
  public async Task<string> DownloadFileAsync(string            fileName,
                                              CancellationToken cancellationToken = default)
  {
    var r = await Client.GetObjectAsync(new GetObjectRequest
                                        {
                                          BucketName = BucketName,
                                          Key        = fileName,
                                        },
                                        cancellationToken);

    var materializedFileName = Path.Combine(LocalZipDir,
                                            fileName + Guid.NewGuid());

    var targetFileName = Path.Combine(DestinationDirPath,
                                      fileName);

    try
    {
      await r.WriteResponseStreamToFileAsync(materializedFileName,
                                             false,
                                             cancellationToken)
             .ConfigureAwait(false);

      FileExt.MoveOrDelete(materializedFileName,
                           targetFileName);

      return targetFileName;
    }
    catch (AmazonS3Exception amazonS3Exception)
    {
      if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
      {
        throw new Exception("Check the provided AWS Credentials.");
      }

      throw new Exception("Error occurred: " + amazonS3Exception.Message);
    }
    finally
    {
      FileExt.TryDelete(materializedFileName);
    }
  }
}
