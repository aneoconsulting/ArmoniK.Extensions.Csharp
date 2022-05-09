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

using System.IO;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Common
{
  public class ClientTestHelpers
  {
    public ClientTestHelpers()
    {
      var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                              .AddJsonFile("appsettings.json",
                                                           true,
                                                           false)
                                              .AddEnvironmentVariables();
    }

    public static TaskOptions InitializeTaskOptions()
    {
      TaskOptions taskOptions = new()
      {
        MaxDuration = new Duration
        {
          Seconds = 300,
        },
        MaxRetries = 5,
        Priority   = 1,
      };
      taskOptions.Options.Add(AppsOptions.GridAppNameKey,
                              "ArmoniK.Samples.EndToEndTests");

      taskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                              "1.0.0");

      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.Samples.EndToEndTests");
      return taskOptions;
    }
  }
}