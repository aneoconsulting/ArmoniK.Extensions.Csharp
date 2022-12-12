using System;
using System.Collections.Concurrent;

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
  public AbstractClientService(Properties                 properties,
                               [CanBeNull] ILoggerFactory loggerFactory = null)
  {
    LoggerFactory = loggerFactory;

    ResultHandlerDictionary = new ConcurrentDictionary<string, IServiceInvocationHandler>();
  }

  /// <summary>
  ///   The result dictionary to return result
  /// </summary>
  protected ConcurrentDictionary<string, IServiceInvocationHandler> ResultHandlerDictionary { get; set; }

  /// <summary>
  ///   The properties to get LoggerFactory or to override it
  /// </summary>
  [CanBeNull]
  protected ILoggerFactory LoggerFactory { get; set; }

  /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
  public abstract void Dispose();
}

