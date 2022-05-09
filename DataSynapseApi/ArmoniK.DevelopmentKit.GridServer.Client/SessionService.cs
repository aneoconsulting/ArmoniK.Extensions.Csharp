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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Submitter;
using ArmoniK.Extensions.Common.StreamWrapper.Client;

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  /// <summary>
  /// The class SessionService will be create each time the function CreateSession or OpenSession will
  /// be called by client or by the worker.
  /// </summary>
  [MarkDownDoc]
  public class SessionService : BaseClientSubmitter<SessionService>
  {
#pragma warning restore CS1591

    /// <summary>
    /// Ctor to instantiate a new SessionService
    /// This is an object to send task or get Results from a session
    /// </summary>
    public SessionService(ILoggerFactory            loggerFactory,
                          Submitter.SubmitterClient controlPlaneService,
                          TaskOptions               taskOptions = null) : base(loggerFactory)
    {
      taskOptions ??= InitializeDefaultTaskOptions();

      TaskOptions = CopyTaskOptionsForClient(taskOptions);

      ControlPlaneService = controlPlaneService;

      Logger.LogDebug("Creating Session... ");

      SessionId = CreateSession();

      Logger.LogDebug($"Session Created {SessionId}");
    }

    /// <summary>
    /// Create SessionService with a previous opened session
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="controlPlaneService"></param>
    /// <param name="clientOptions">Client options passed during the CreateSession</param>
    /// <param name="sessionId"></param>
    public SessionService(ILoggerFactory              loggerFactory,
                          Submitter.SubmitterClient   controlPlaneService,
                          Session                     sessionId,
                          IDictionary<string, string> clientOptions) : base(loggerFactory)
    {
      TaskOptions = CopyClientToTaskOptions(clientOptions);

      ControlPlaneService = controlPlaneService;


      Logger.LogDebug("Creating Session... ");

      SessionId = sessionId;

      Logger.LogInformation($"Session Created {SessionId} with taskOptions.Priority : {TaskOptions.Priority}");
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
      if (SessionId?.Id != null)
        return SessionId?.Id;
      else
        return "Session_Not_ready";
    }

    public static TaskOptions InitializeDefaultTaskOptions()
    {
      TaskOptions taskOptions = new()
      {
        MaxDuration = new()
        {
          Seconds = 40,
        },
        MaxRetries = 2,
        Priority   = 1,
      };

      taskOptions.Options.Add(AppsOptions.EngineTypeNameKey,
                              EngineType.DataSynapse.ToString());

      taskOptions.Options.Add(AppsOptions.GridAppNameKey,
                              "ArmoniK.DevelopmentKit.GridServer");

      taskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                              "1.X.X");

      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.DevelopmentKit.GridServer");

      taskOptions.Options.Add(AppsOptions.GridServiceNameKey,
                              "FallBackServerAdder");

      return taskOptions;
    }

    private static TaskOptions CopyTaskOptionsForClient(TaskOptions taskOptions)
    {
      var res = new TaskOptions
      {
        MaxDuration = taskOptions.MaxDuration,
        MaxRetries  = taskOptions.MaxRetries,
        Priority    = taskOptions.Priority,
        Options =
        {
          ["MaxDuration"] = taskOptions.MaxDuration.Seconds.ToString(),
          ["MaxRetries"]  = taskOptions.MaxRetries.ToString(),
          ["Priority"]    = taskOptions.Priority.ToString(),
        },
      };

      taskOptions.Options.ToList()
                 .ForEach(pair => res.Options[pair.Key] = pair.Value);

      return res;
    }

    private TaskOptions CopyClientToTaskOptions(IDictionary<string, string> clientOptions)
    {
      TaskOptions defaultTaskOption = InitializeDefaultTaskOptions();

      TaskOptions taskOptions = new()
      {
        MaxDuration = new()
        {
          Seconds = clientOptions.ContainsKey("MaxDuration") ? long.Parse(clientOptions["MaxDuration"]) : defaultTaskOption.MaxDuration.Seconds,
        },
        MaxRetries = clientOptions.ContainsKey("MaxRetries") ? int.Parse(clientOptions["MaxRetries"]) : defaultTaskOption.MaxRetries,
        Priority   = clientOptions.ContainsKey("Priority") ? int.Parse(clientOptions["Priority"]) : defaultTaskOption.Priority,
      };

      defaultTaskOption.Options.ToList()
                       .ForEach(pair => taskOptions.Options[pair.Key] = pair.Value);

      clientOptions.ToList()
                   .ForEach(pair => taskOptions.Options[pair.Key] = pair.Value);

      return taskOptions;
    }

    private Session CreateSession()
    {
      using var _         = Logger.LogFunction();
      var       sessionId = Guid.NewGuid().ToString();
      var createSessionRequest = new CreateSessionRequest
      {
        DefaultTaskOption = TaskOptions,
        Id                = sessionId,
      };
      var session = ControlPlaneService.CreateSession(createSessionRequest);
      switch (session.ResultCase)
      {
        case CreateSessionReply.ResultOneofCase.Error:
          throw new Exception("Error while creating session : " + session.Error);
        case CreateSessionReply.ResultOneofCase.None:
          throw new Exception("Issue with Server !");
        case CreateSessionReply.ResultOneofCase.Ok:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      return new Session()
      {
        Id = sessionId,
      };
    }

    /// <summary>
    ///   Set connection to an already opened Session
    /// </summary>
    /// <param name="session">SessionId previously opened</param>
    public void OpenSession(Session session)
    {
      if (SessionId == null) Logger.LogDebug($"Open Session {session.Id}");
      SessionId = session;
    }

    /// <summary>
    ///   User method to submit task from the client
    ///   Need a client Service. In case of ServiceContainer
    ///   controlPlaneService can be null until the OpenSession is called
    /// </summary>
    /// <param name="payloads">
    ///   The user payload list to execute. General used for subTasking.
    /// </param>
    public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads)
    {
      return SubmitTasksWithDependencies(payloads.Select(payload => new Tuple<byte[], IList<string>>(payload,
                                                                                                     null)));
    }

    /// <summary>
    ///   User method to submit task from the client
    /// </summary>
    /// <param name="client">The client instance for extension</param>
    /// <param name="payload">
    ///   The user payload to execute.
    /// </param>
    public string SubmitTask(byte[] payload)
    {
      Thread.Sleep(2); // Twice the keep alive 
      return SubmitTasks(new[] { payload })
        .Single();
    }


    /// <summary>
    ///   The method to submit One task with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="client">The client instance for extension</param>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created task</param>
    /// <returns>return the taskId of the created task </returns>
    public string SubmitTaskWithDependencies(byte[] payload, IList<string> dependencies)
    {
      return SubmitTasksWithDependencies(new[]
      {
        Tuple.Create(payload,
                     dependencies),
      }).Single();
    }
  }
}