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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.Worker.Worker;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.Worker.Common;
using ArmoniK.DevelopmentKit.Worker.Common.Adapter;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Worker.DLLWorker;

public class ServiceId : IEquatable<ServiceId>
{
  public ServiceId(string engineTypeName,
                   string pathToZipFile,
                   string namespaceService)
    => Key = $"{engineTypeName}#{pathToZipFile}#{namespaceService}".ToLower();

  public string Key { get; }

  /// <inheritdoc />
  public bool Equals(ServiceId other)
  {
    if (other is null)
    {
      return false;
    }

    if (ReferenceEquals(this,
                        other))
    {
      return true;
    }

    return Key == other.Key;
  }

  /// <summary>Returns a string that represents the current object.</summary>
  /// <returns>A string that represents the current object.</returns>
  public override string ToString()
    => Key;

  /// <inheritdoc />
  public override bool Equals(object obj)
  {
    if (obj is null)
    {
      return false;
    }

    if (ReferenceEquals(this,
                        obj))
    {
      return true;
    }

    return obj.GetType() == GetType() && Equals((ServiceId)obj);
  }

  /// <inheritdoc />
  public override int GetHashCode()
    => Key != null
         ? Key.GetHashCode()
         : 0;


  /// <summary>
  ///   Checks if both ServiceIds are equal
  /// </summary>
  /// <param name="a">ServiceId a</param>
  /// <param name="b">ServiceId b</param>
  /// <returns>Same as a.Equals(b)</returns>
  public static bool operator ==(ServiceId a,
                                 ServiceId b)
    => a?.Equals(b) ?? false;

  /// <summary>
  ///   Checks if both ServiceIds are different
  /// </summary>
  /// <param name="a">ServiceId a</param>
  /// <param name="b">ServiceId b</param>
  /// <returns>Same as !a.Equals(b)</returns>
  public static bool operator !=(ServiceId a,
                                 ServiceId b)
    => !(a == b);
}

public class ArmonikServiceWorker : IDisposable
{
  public ArmonikServiceWorker()
    => Initialized = false;

  public ServiceId ServiceId { get; set; }

  public AppsLoader  AppsLoader { get; set; }
  public IGridWorker GridWorker { get; set; }

  public bool Initialized { get; set; }

  /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
  public void Dispose()
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker?.Dispose();
    }

    GridWorker = null;
    AppsLoader.Dispose();
    AppsLoader  = null;
    Initialized = false;
  }

  public void CloseSession()
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker?.SessionFinalize();
    }
  }

  public void Configure(IConfiguration configuration,
                        TaskOptions    requestTaskOptions)
  {
    if (Initialized)
    {
      return;
    }

    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker.Configure(configuration,
                           requestTaskOptions,
                           AppsLoader);
    }

    Initialized = true;
  }

  public void InitializeSessionWorker(Session     sessionId,
                                      TaskOptions taskHandlerTaskOptions)
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker.InitializeSessionWorker(sessionId,
                                         taskHandlerTaskOptions);
    }
  }

  public byte[] Execute(ITaskHandler taskHandler)
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      return GridWorker.Execute(taskHandler);
    }
  }

  public void DestroyService()
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker.DestroyService();
    }
  }

}

public class ServiceRequestContext
{
  private readonly ILogger<ServiceRequestContext> logger_;

  [CanBeNull]
  private ArmonikServiceWorker currentService_;

  public ServiceRequestContext(ILoggerFactory loggerFactory)
  {
    LoggerFactory   = loggerFactory;
    currentService_ = null;
    logger_         = loggerFactory.CreateLogger<ServiceRequestContext>();
  }

  public Session SessionId { get; set; }

  public ILoggerFactory LoggerFactory { get; set; }

  public bool IsNewSessionId(Session sessionId)
  {
    if (SessionId == null)
    {
      return true;
    }

    return SessionId.Id != sessionId.Id;
  }

  public bool IsNewSessionId(string sessionId)
  {
    if (sessionId == null)
    {
      throw new ArgumentNullException(nameof(sessionId));
    }

    if (SessionId == null)
    {
      return true;
    }

    var currentSessionId = new Session
                           {
                             Id = sessionId,
                           };

    return IsNewSessionId(currentSessionId);
  }

  public ArmonikServiceWorker CreateOrGetArmonikService(IConfiguration configuration,
                                                        string         engineTypeName,
                                                        IFileAdapter   fileAdapter,
                                                        string         fileName,
                                                        TaskOptions    requestTaskOptions)
  {
    if (string.IsNullOrEmpty(requestTaskOptions.ApplicationNamespace))
    {
      throw new WorkerApiException("Cannot find namespace service in TaskOptions. Please set the namespace");
    }

    var serviceId = GenerateServiceId(engineTypeName,
                                      Path.Combine(fileAdapter.DestinationDirPath,
                                                   fileName),
                                      requestTaskOptions.ApplicationNamespace);

    if (currentService_?.ServiceId == serviceId)
    {
      return currentService_;
    }

    logger_.LogInformation($"Worker needs to load new context, from {currentService_?.ServiceId?.ToString() ?? "null"} to {serviceId}");

    currentService_?.DestroyService();
    currentService_?.Dispose();
    currentService_ = null;

    var appsLoader = new AppsLoader(configuration,
                                    LoggerFactory,
                                    engineTypeName,
                                    fileAdapter,
                                    fileName);

    currentService_ = new ArmonikServiceWorker
                      {
                        AppsLoader = appsLoader,
                        GridWorker = appsLoader.GetGridWorkerInstance(configuration,
                                                                      LoggerFactory),
                        ServiceId = serviceId,
                      };

    currentService_.Configure(configuration,
                              requestTaskOptions);

    return currentService_;
  }

  public static ServiceId GenerateServiceId(string engineTypeName,
                                            string uniqueKey,
                                            string namespaceService)
    => new(engineTypeName,
           uniqueKey,
           namespaceService);

  public static IFileAdapter CreateOrGetFileAdapter(IConfiguration configuration,
                                                    string         localDirectoryZip)
  {
    var sectionStorage = configuration.GetSection("FileStorageType");
    if (sectionStorage.Exists() && configuration["FileStorageType"] == "FS")
    {
      return new FsAdapter(localDirectoryZip);
    }

    if ((sectionStorage.Exists() && configuration["FileStorageType"] == "S3") || !sectionStorage.Exists())
    {
      var configurationSection = configuration.GetSection("S3Storage");
      return new S3Adapter(configurationSection["ServiceURL"],
                           configurationSection["BucketName"],
                           configurationSection["AccessKeyId"],
                           configurationSection["SecretAccessKey"],
                           "",
                           configurationSection.GetValue("MustForcePathStyle",
                                                         false));
    }

    throw new WorkerApiException("Cannot find the FileStorageType in the IConfiguration. Please make sure you have properly set the field [FileStorageType]");
  }
}
