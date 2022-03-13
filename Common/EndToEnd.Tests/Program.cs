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

using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Type = System.Type;

namespace ArmoniK.EndToEndTests
{
  public class Program
  {
    private static IConfiguration Configuration { get; set; }
    private static ILogger<Program> Logger { get; set; }
    private static ILoggerFactory LoggerFactory { get; set; }

    private static void Main()
    {
      Console.WriteLine("Hello Armonik End to End Tests !");


      var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                              .AddJsonFile("appsettings.json",
                                                           true,
                                                           true)
                                              .AddEnvironmentVariables();

      Configuration = builder.Build();

      LoggerFactory = new LoggerFactory(new[]
      {
        new SerilogLoggerProvider(new LoggerConfiguration()
                                  .ReadFrom
                                  .Configuration(Configuration)
                                  .CreateLogger())
      });

      Logger = LoggerFactory.CreateLogger<Program>();

      IEnumerable<TestContext> clientsContainers = RetrieveClientTests();

      foreach (var clientContainer in clientsContainers)
      {
        if (clientContainer.MethodTests == null)
          continue;

        foreach (var methodTest in clientContainer.MethodTests)
        {
          Logger.LogInformation($"\n\n-------- [TEST] : {clientContainer.ClassClient} : {methodTest.Name}");
          methodTest.Invoke(clientContainer.ClientClassInstance,
                            null);
        }
      }
    }

    private static IEnumerable<TestContext> RetrieveClientTests()
    {
      //Get first test where ServiceContainer Exists

      var serviceContainerTypes = new[] { Assembly.GetExecutingAssembly() }
                                  .SelectMany(x =>
                                  {
                                    try
                                    {
                                      return x.GetTypes();
                                    }
                                    catch (ReflectionTypeLoadException ex)
                                    {
                                      return ex.Types.Where(t => t != null);
                                    }
                                    catch
                                    {
                                      return Type.EmptyTypes;
                                    }
                                  })
                                  .Where(x => x != null)
                                  .Where(x => x.IsPublic &&
                                              !x.IsGenericType &&
                                              !typeof(Delegate).IsAssignableFrom(x) &&
                                              !x.GetCustomAttributes<ObsoleteAttribute>().Any() &&
                                              !x.GetCustomAttributes<DisabledAttribute>().Any())
                                  .ToArray();

      var results = serviceContainerTypes.Select(x => new Tuple<Type, MethodInfo[]>(x,
                                                                                    GetMethods(x)))
                                         .Where(x => x.Item2 != null &&
                                                     x.Item2.Length > 0 &&
                                                     x.Item2.Any(m => m.GetCustomAttributes<EntryPointAttribute>().Any()))
                                         .Select(x => new TestContext()
                                         {
                                           ClassClient = x.Item1,
                                           ClientClassInstance = Activator.CreateInstance(x.Item1,
                                                                                          Configuration,
                                                                                          LoggerFactory),
                                           NameSpaceTest = x.Item1.Namespace ?? string.Empty,
                                           MethodTests   = x.Item2,
                                         });

      var retrieveClientTests = results.ToList();

      Logger.LogInformation($"List of tests : \n\t{string.Join("\n\t", retrieveClientTests.Select(x => $"{x.NameSpaceTest}.{x.ClassClient}"))}");

      return retrieveClientTests;
    }


    public static MethodInfo[] GetMethods(Type type)
    {
      return type.GetMethods(BindingFlags.Public |
                             BindingFlags.Instance |
                             BindingFlags.NonPublic |
                             BindingFlags.InvokeMethod)
                 .Where(x => !x.IsSpecialName &&
                             !x.GetCustomAttributes<ObsoleteAttribute>().Any() &&
                             x.GetCustomAttributes<EntryPointAttribute>().Any() &&
                             !x.IsPrivate)
                 .ToArray();
    }
  }
}