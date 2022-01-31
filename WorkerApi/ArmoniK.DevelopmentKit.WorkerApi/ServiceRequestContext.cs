// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2021. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;

using Google.Protobuf.Collections;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.WorkerApi
{
  public class ServiceId
  {
    public ServiceId(string engineTypeName, string pathToZipFile, string namespaceService)
    {
      Key = $"{engineTypeName}#{pathToZipFile}#{namespaceService}".ToLower();
    }

    public string Key { get; set; }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
      return Key;
    }
  }

  public class ArmonikServiceWorker : IDisposable
  {
    public AppsLoader AppsLoader { get; set; }
    public IGridWorker GridWorker { get; set; }

    public bool Initialized { get; set; }

    public ArmonikServiceWorker()
    {
      Initialized = false;
    }

    public void CloseSession()
    {
      GridWorker?.SessionFinalize();
    }

    public void Configure(IConfiguration configuration, MapField<string, string> requestTaskOptions)
    {
      if (!Initialized)
      {
        GridWorker.Configure(configuration,
                             requestTaskOptions,
                             AppsLoader);
        Initialized = true;
      }
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
      GridWorker?.Dispose();
      GridWorker = null;
      AppsLoader.Dispose();
      AppsLoader = null;
    }
  }

  public class ServiceRequestContext
  {
    public SessionId SessionId { get; set; }

    public LoggerFactory LoggerFactory { get; set; }
    private IDictionary<string, ArmonikServiceWorker> ServicesMapper { get; set; }

    public ServiceRequestContext(LoggerFactory loggerFactory)
    {
      LoggerFactory  = loggerFactory;
      ServicesMapper = new Dictionary<string, ArmonikServiceWorker>();
    }

    public bool IsNewSessionId(SessionId sessionId)
    {
      if (SessionId == null) return true;

      return SessionId.Session != sessionId.Session;
    }

    public bool IsNewSessionId(string sessionId)
    {
      if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));

      if (SessionId == null) return true;

      SessionId currentSessionId = sessionId.CanUnPackTaskId()
        ? sessionId.UnPackSessionId()
        : new SessionId()
        {
          Session    = sessionId,
          SubSession = sessionId
        };

      return IsNewSessionId(currentSessionId);
    }

    public ServiceId CreateOrGetArmonikService(IConfiguration           configuration,
                                               string                   engineTypeName,
                                               string                   pathToZipFile,
                                               MapField<string, string> requestTaskOptions)
    {
      if (!requestTaskOptions.ContainsKey(AppsOptions.GridAppNamespaceKey))
      {
        throw new WorkerApiException("Cannot find namespace service in TaskOptions. Please set the namespace");
      }

      var serviceId = GenerateServiceId(engineTypeName,
                                        pathToZipFile,
                                        requestTaskOptions[AppsOptions.GridAppNamespaceKey]);

      if (ServicesMapper.ContainsKey(serviceId.Key))
        return serviceId;

      var appsLoader = new AppsLoader(configuration,
                                      LoggerFactory,
                                      engineTypeName,
                                      pathToZipFile);

      var armonikServiceWorker = new ArmonikServiceWorker()
      {
        AppsLoader = appsLoader,
        GridWorker = appsLoader.GetGridWorkerInstance(configuration,
                                                      LoggerFactory)
      };
      
      ServicesMapper[serviceId.Key] = armonikServiceWorker;

      if (!armonikServiceWorker.Initialized)
      {
        armonikServiceWorker.Configure(configuration,
                                       requestTaskOptions);
      }

      return serviceId;
    }

    public ArmonikServiceWorker GetService(ServiceId serviceId)
    {
      return ServicesMapper[serviceId.Key];
    }

    public static ServiceId GenerateServiceId(string engineTypeName,
                                              string pathToZipFile,
                                              string namespaceService)
    {
      return new ServiceId(engineTypeName,
                                    pathToZipFile,
                                    namespaceService);
    }

  }
}