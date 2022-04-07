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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;

namespace ArmoniK.DevelopmentKit.WorkerApi
{
  public class Program
  {
    private static readonly string SocketPath = "/cache/armonik.sock";

    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    // Additional configuration is required to successfully run gRPC on macOS.
    // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
    public static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
          .UseSerilog((context, services, configuration) => configuration
                                                            .ReadFrom.Configuration(context.Configuration)
                                                            .ReadFrom.Services(services)
                                                            .MinimumLevel
                                                            .Override("Microsoft.AspNetCore",
                                                                      LogEventLevel.Debug)
                                                            .Enrich.FromLogContext())
          .ConfigureWebHostDefaults(webBuilder =>
          {
            webBuilder.UseStartup<Startup>()
                      .ConfigureKestrel(options =>
                      {
                        options.Limits.MaxRequestBodySize = 2097152000;
                        if (File.Exists(SocketPath))
                        {
                          File.Delete(SocketPath);
                        }

                        options.ListenUnixSocket(SocketPath,
                                                 listenOptions => { listenOptions.Protocols = HttpProtocols.Http2; });
                      });
          });
  }
}
