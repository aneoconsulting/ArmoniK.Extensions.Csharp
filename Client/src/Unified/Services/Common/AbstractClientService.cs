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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using ArmoniK.DevelopmentKit.Client.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Unified.Services.Common;

/// <summary>
///   The abstract for client service creation
/// </summary>
public abstract class AbstractClientService : IDisposable
{
  /// <summary>
  ///   The default constructor with properties information
  /// </summary>
  /// <param name="properties"></param>
  /// <param name="loggerFactory"></param>
  public AbstractClientService(Properties         properties,
                               ILoggerFactory? loggerFactory = null)
  {
    LoggerFactory = loggerFactory;

    ResultHandlerDictionary = new ConcurrentDictionary<string, IServiceInvocationHandler>();
  }

  /// <summary>
  ///   Instant view of currently handled task ids.
  ///   The list is only valid at the time of access.
  ///   The actual list may differ due to background processes.
  /// </summary>
  public IReadOnlyCollection<string> CurrentlyHandledTaskIds
    => (IReadOnlyCollection<string>)ResultHandlerDictionary.Keys;

  /// <summary>
  ///   The result dictionary to return result
  /// </summary>
  protected ConcurrentDictionary<string, IServiceInvocationHandler> ResultHandlerDictionary { get; set; }

  /// <summary>
  ///   The properties to get LoggerFactory or to override it
  /// </summary>
  protected ILoggerFactory? LoggerFactory { get; set; }

  /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
  public abstract void Dispose();
}
