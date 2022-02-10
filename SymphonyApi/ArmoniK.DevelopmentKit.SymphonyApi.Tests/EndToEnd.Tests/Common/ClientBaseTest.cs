// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
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

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Common
{
  public abstract class ClientBaseTest<T>
  {
    protected IConfiguration Configuration { get; set; }

    protected ILogger<T> Log { get; set; }

    protected TaskOptions TaskOptions { get; set; }
    protected ILoggerFactory LoggerFactory { get; set; }

    public ClientBaseTest(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
      Configuration = configuration;

      LoggerFactory = loggerFactory;

      Log = LoggerFactory.CreateLogger<T>();
    }

    public virtual TaskOptions InitializeTaskOptions()
    {
      TaskOptions = new()
      {
        MaxDuration = new Duration
        {
          Seconds = 300,
        },
        MaxRetries = 5,
        Priority   = 1,
        IdTag      = "ArmonikTag",
      };
      
      TaskOptions.Options[AppsOptions.GridAppNameKey] = "ArmoniK.EndToEndTests";

      var version = typeof(ClientBaseTest<T>).Assembly.GetName().Version;
      if (version != null)
        TaskOptions.Options[AppsOptions.GridAppVersionKey] = $"{version.Major}.{version.Minor}.{version.Build}";
      else
      {
        TaskOptions.Options[AppsOptions.GridAppVersionKey] = "1.0.0";
      }

      TaskOptions.Options[AppsOptions.GridAppNamespaceKey] = typeof(T).Namespace;

      return TaskOptions;
    }

    public void SetNamespaceTest(string namespaceTest)
    {
      TaskOptions.Options[AppsOptions.GridAppNamespaceKey] = namespaceTest;
    }


    public abstract void EntryPoint();
  }

  public static class TaskOptionsExt
  {
    public static TaskOptions SetVersion(this TaskOptions taskOptions, string version)
    {
      taskOptions.Options[AppsOptions.GridAppVersionKey] = version;

      return taskOptions;
    }

    public static TaskOptions SetEngineType(this TaskOptions taskOptions, string engineType)
    {
      taskOptions.Options[AppsOptions.EngineTypeNameKey] = engineType;

      return taskOptions;
    }

    public static TaskOptions SetNamespaceService(this TaskOptions taskOptions, string namespaceService)
    {
      taskOptions.Options[AppsOptions.GridAppNamespaceKey] = namespaceService;

      return taskOptions;
    }

    public static TaskOptions SetApplicationName(this TaskOptions taskOptions, string applicationName)
    {
      taskOptions.Options[AppsOptions.GridAppNameKey] = applicationName;

      return taskOptions;
    }

    public static TaskOptions SetSessionPriority(this TaskOptions taskOptions, int priority)
    {
      taskOptions.Priority = priority;

      return taskOptions;
    }


  }
}