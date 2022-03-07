#if NET5_0_OR_GREATER
using Grpc.Net.Client;
#else
using Grpc.Core;
#endif
using System;
using System.Collections.Generic;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  /// <summary>
  ///   The main object to communicate with the control Plane from the client side
  ///   The class will connect to the control plane to createSession, SubmitTask,
  ///   Wait for result and get the result.
  ///   See an example in the project ArmoniK.Samples in the sub project
  ///   https://github.com/aneoconsulting/ArmoniK.Samples/tree/main/Samples/GridServerLike
  ///   Samples.ArmoniK.Sample.SymphonyClient
  /// </summary>
  [MarkDownDoc]
  public class ArmonikDataSynapseClientService
  {
    private readonly  IConfigurationSection                    controlPlanAddress_;
    internal readonly ILogger<ArmonikDataSynapseClientService> Logger;
    private Submitter.SubmitterClient ControlPlaneService { get; set; }

    /// <summary>
    /// Returns the section key Grpc from appSettings.json
    /// </summary>
    public static string SectionControlPlan { get; } = "Grpc";

    /// <summary>
    /// Set or Get TaskOptions with inside MaxDuration, Priority, AppName, VersionName and AppNamespace
    /// </summary>
    public TaskOptions TaskOptions { get; set; }

    /// <summary>
    /// Only used for internal DO NOT USED IT
    /// Get or Set SessionId object stored during the call of SubmitTask, SubmitSubTask,
    /// SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
    /// </summary>
    public Session SessionId { get; private set; }

    /// <summary>
    /// Only used for internal DO NOT USED IT
    /// Get or Set SubSessionId object stored during the call of SubmitTask, SubmitSubTask,
    /// SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
    /// </summary>
    public Session SubSessionId { get; set; }


    private ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// The ctor with IConfiguration and optional TaskOptions
    /// 
    /// </summary>
    /// <param name="configuration">IConfiguration to set Client Data information and Grpc EndPoint</param>
    /// <param name="loggerFactory">The factory to create the logger for clientService</param>
    /// <param name="taskOptions">TaskOptions for any Session</param>
    public ArmonikDataSynapseClientService(IConfiguration configuration, ILoggerFactory loggerFactory, TaskOptions taskOptions = null)
    {
      controlPlanAddress_ = configuration.GetSection(SectionControlPlan);
      LoggerFactory       = loggerFactory;
      Logger              = loggerFactory.CreateLogger<ArmonikDataSynapseClientService>();

      if (taskOptions != null) TaskOptions = taskOptions;
    }

    /// <summary>
    /// Create the session to submit task
    /// </summary>
    /// <param name="taskOptions">Optional parameter to set TaskOptions during the Session creation</param>
    /// <returns></returns>
    public SessionService CreateSession(TaskOptions taskOptions = null)
    {
      if (taskOptions != null) TaskOptions = taskOptions;

      ControlPlaneConnection();

      Logger.LogDebug("Creating Session... ");

      return new SessionService(LoggerFactory,
                                ControlPlaneService,
                                taskOptions);
    }

    private void ControlPlaneConnection()
    {
#if NET5_0_OR_GREATER
      var channel = GrpcChannel.ForAddress(controlPlanAddress_["Endpoint"]);
#else
      Environment.SetEnvironmentVariable("GRPC_DNS_RESOLVER",
                                         "native");
      var uri = new Uri(controlPlanAddress_["Endpoint"]);
      var channel = new Channel($"{uri.Host}:{uri.Port}",
                                ChannelCredentials.Insecure);
#endif
      ControlPlaneService ??= new Submitter.SubmitterClient(channel);
    }

    /// <summary>
    /// Set connection to an already opened Session
    /// </summary>
    /// <param name="sessionId">SessionId previously opened</param>
    /// <param name="clientOptions"></param>
    public SessionService OpenSession(string sessionId, IDictionary<string, string> clientOptions = null)
    {
      ControlPlaneConnection();

      return new SessionService(LoggerFactory,
                                ControlPlaneService,
                                new Session()
                                {
                                  Id = sessionId,
                                },
                                clientOptions);
    }

    /// <summary>
    /// This method is creating a default taskOptions initialization where
    /// MaxDuration is 40 seconds, MaxRetries = 2 The app name is ArmoniK.DevelopmentKit.GridServer
    /// The version is 1.0.0 the namespace ArmoniK.DevelopmentKit.GridServer and simple service FallBackServerAdder 
    /// </summary>
    /// <returns>Return the default taskOptions</returns>
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
                              "1.0.0");

      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.DevelopmentKit.GridServer");

      taskOptions.Options.Add(AppsOptions.GridServiceNameKey,
                              "FallBackServerAdder");

      return taskOptions;
    }
  }
}