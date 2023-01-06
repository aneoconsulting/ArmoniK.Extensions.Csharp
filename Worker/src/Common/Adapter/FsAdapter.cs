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
using System.IO;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;

namespace ArmoniK.DevelopmentKit.Worker.Common.Adapter;

public class FsAdapter : IFileAdapter
{
  public FsAdapter(string sourceDirPath,
                   string localZipDir = "/tmp/packages/zip")
  {
    SourceDirPath      = sourceDirPath;
    DestinationDirPath = localZipDir;

    if (!Directory.Exists(DestinationDirPath))
    {
      Directory.CreateDirectory(DestinationDirPath);
    }
  }

  private string SourceDirPath { get; }

  /// <summary>
  ///   The getter to retrieve the last downloaded file
  /// </summary>
  public string? DestinationFullPath { get; set; }

  public string DestinationDirPath { get; set; }

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
