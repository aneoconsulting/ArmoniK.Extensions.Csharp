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

using ArmoniK.DevelopmentKit.Common;

namespace ArmoniK.DevelopmentKit.Worker.Common.Archive;

/// <summary>
/// Interface for archive extraction
/// </summary>
internal interface IArchiver
{

  /// <summary>
  /// Extracts an archive file
  /// </summary>
  /// <param name="fileAdapter">File adapter to fetch the file</param>
  /// <param name="filename">File name</param>
  /// <returns>Path to assembly file</returns>
  internal string ExtractArchive(IFileAdapter fileAdapter,
                        string       filename);

  /// <summary>
  /// Checks if the archive has already been extracted
  /// </summary>
  /// <param name="fileAdapter">File adapter to fetch the file</param>
  /// <param name="fileName">File name</param>
  /// <param name="waitForArchiver">Number of 2 seconds intervals to wait for the lock file to be </param>
  /// <returns>True if the archive has already been extracted, false otherwise</returns>
  internal bool ArchiveAlreadyExtracted(IFileAdapter fileAdapter,
                                        string       fileName,
                                        int          waitForArchiver);
}
