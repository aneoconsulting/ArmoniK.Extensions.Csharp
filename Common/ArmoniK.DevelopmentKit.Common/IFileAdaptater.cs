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

namespace ArmoniK.DevelopmentKit.Common
{
  /// <summary>
  /// The interface to download file 
  /// </summary>
  public interface IFileAdaptater
  {
    /// <summary>
    /// Get The directory where the file will be downloaded
    /// </summary>
    public string DestinationDirPath { get; set; }
    /// <summary>
    /// The method to download file from form remote server
    /// </summary>
    /// <param name="fileName">The filename with extension and without directory path</param>
    /// <returns>Returns the path where the file has been downloaded</returns>
    public string DownloadFile(string fileName);
  }
}
