// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023.All rights reserved.
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
using System.Collections.Generic;
using System.Linq;

namespace ArmoniK.DevelopmentKit.Common.Exceptions;

/// <summary>
///   Bundle an exception with list of task in Error
/// </summary>
public class ClientResultsException : Exception
{
  /// <summary>
  ///   The default constructor to refer the list of task in error
  /// </summary>
  /// <param name="taskIds">The list of taskId</param>
  public ClientResultsException(params string[] taskIds)
    : base(BuildMessage(taskIds))
    => TaskIds = taskIds;

  /// <summary>
  ///   The default constructor to refer the list of task in error
  /// </summary>
  /// <param name="message">the message in exception</param>
  /// <param name="taskIds">The list of taskId</param>
  public ClientResultsException(string          message,
                                params string[] taskIds)
    : base(message)
    => TaskIds = taskIds;

  /// <summary>
  ///   The default constructor to refer the list of task in error
  /// </summary>
  /// <param name="message">the message in exception</param>
  /// <param name="innerException">Exception that caused this one to be raised</param>
  /// <param name="taskIds">The list of taskId</param>
  public ClientResultsException(string          message,
                                Exception       innerException,
                                params string[] taskIds)
    : base(message,
           innerException)
    => TaskIds = taskIds;


  /// <summary>
  ///   The list of taskId in error
  /// </summary>
  public string[] TaskIds { get; }

  private static string BuildMessage(IEnumerable<string> taskIds)
  {
    var arrTaskIds = taskIds as string[] ?? taskIds.ToArray();
    var msg =
      $"The missing tasks are in error. Please check log for more information on Armonik grid server list of taskIds in Error : [ {string.Join(", ", arrTaskIds)}";

    msg += " ]";

    return msg;
  }
}
