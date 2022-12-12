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

namespace ArmoniK.DevelopmentKit.Worker.Unified.Exceptions;

/// <summary>
///   The exception class for Server side reporting Grid Error
/// </summary>
public class GridServerException : Exception
{
  /// <summary>
  ///   The constructor in string message in parameters
  /// </summary>
  /// <param name="message">the message to include in the exception</param>
  public GridServerException(string message)
    : base(message)
  {
  }

  /// <summary>
  ///   The constructor with Message and Exception
  /// </summary>
  /// <param name="message">The string message in the new exception</param>
  /// <param name="e">the inner exception</param>
  public GridServerException(string    message,
                             Exception e)
    : base(message,
           e)
  {
  }
}

