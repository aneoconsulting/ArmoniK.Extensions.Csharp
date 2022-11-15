// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace ArmoniK.Extensions.Csharp.UnitTests.Helpers;

internal class NunitConfigurationHelpers
{
  public static (IConfigurationRoot configuration, LoggerFactory loggerFactory) GetConfigurationAndLoggerFactory()
  {
    Console.WriteLine("Hello Armonik End to End Tests !");


    var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                            .AddJsonFile("appsettings.json",
                                                         true,
                                                         false)
                                            .AddEnvironmentVariables();


    var configuration = builder.Build();

    var loggerFactory = new LoggerFactory(new[]
                                          {
                                            new SerilogLoggerProvider(new LoggerConfiguration().ReadFrom.Configuration(configuration)
                                                                                               .CreateLogger()),
                                          },
                                          new LoggerFilterOptions().AddFilter("Grpc",
                                                                              LogLevel.Error));


    //logger.LogInformation($"EntryPoint : {configuration.GetSection("Grpc")["EndPoint"]}");
    //logger.LogInformation($"CaCert     : {configuration.GetSection("Grpc")["CaCert"]}");
    //logger.LogInformation($"ClientCert : {configuration.GetSection("Grpc")["ClientCert"]}");
    //logger.LogInformation($"ClientKey  : {configuration.GetSection("Grpc")["ClientKey"]}");

    return (configuration, loggerFactory);
  }
}
