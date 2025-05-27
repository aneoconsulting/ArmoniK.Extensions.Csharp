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

namespace ArmoniK.DevelopmentKit.Worker.Common.Archive;

/// <summary>
///   Interface for archive extraction
/// </summary>
public interface IArchiver
{
  /// <summary>
  ///   Extracts an archive file
  /// </summary>
  /// <param name="filename">File name</param>
  /// <param name="packageId">Package Id</param>
  /// <param name="overwrite">Overwrite the files if they have been already extracted</param>
  /// <returns>Path to extracted package folder</returns>
  public string ExtractArchive(string    filename,
                               PackageId packageId);

  /// <summary>
  ///   Checks if the archive has already been extracted. If the file is being extracted by another process, waits for its
  ///   completion to return an answer
  /// </summary>
  /// <param name="packageId">Package Id</param>
  /// <returns>True if the archive has already been extracted, false otherwise</returns>
  public bool ArchiveAlreadyExtracted(PackageId packageId);
}
