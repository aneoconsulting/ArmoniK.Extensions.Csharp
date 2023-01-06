using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Exceptions;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Client.Unified.Factory;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Admin;
using ArmoniK.DevelopmentKit.Common;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Client.Tests;

internal class UnifiedTestHelper : UnitTestHelperBase, IServiceInvocationHandler
{
  private readonly ConcurrentDictionary<string, object?> expectedResults_ = new();

  public UnifiedTestHelper(EngineType engineType,
                           string     applicationNamespace,
                           string     applicationService,
                           int        bufferRequestSize    = 100,
                           int        maxConcurrentBuffers = 2,
                           int        maxParallelChannels  = 2,
                           TimeSpan?  timeOut              = null)
    : base(engineType,
           applicationNamespace,
           applicationService)
  {
    Props.MaxConcurrentBuffers = maxConcurrentBuffers;
    Props.MaxTasksPerBuffer    = bufferRequestSize;
    Props.MaxParallelChannels  = maxParallelChannels;
    Props.TimeTriggerBuffer    = timeOut ?? Props.TimeTriggerBuffer;

    switch (engineType)
    {
      case EngineType.Unified:
      {
        Service = ServiceFactory.CreateService(Props,
                                               LoggerFactory);
        ServiceAdmin = ServiceFactory.GetServiceAdmin(Props,
                                                      LoggerFactory);
        break;
      }
      case EngineType.DataSynapse:
      {
        Service = DevelopmentKit.Client.GridServer.ServiceFactory.GetInstance()
                                .CreateService(TaskOptions!.ApplicationName,
                                               Props);
        ServiceAdmin = null;
        break;
      }
      default:
        throw new ArgumentOutOfRangeException(nameof(engineType));
    }

    Log.LogInformation($"New session created : {Service.SessionId}");
  }

  public ISubmitterService Service      { get; }
  public ServiceAdmin?     ServiceAdmin { get; }

  public void HandleError(ServiceInvocationException? e,
                          string                      taskId)
  {
    Log.LogError("Error (ignore) from {taskId} : [ {message} ]",
                 taskId,
                 e?.Message);
    expectedResults_[taskId] = e;
  }

  public void HandleResponse(object? response,
                             string  taskId)
    => expectedResults_[taskId] = response;

  internal object? WaitForResultcompletion(string taskIdToWait)
    => WaitForResultcompletion(new[]
                               {
                                 taskIdToWait,
                               })
       .First()
       .Value;

  internal Dictionary<string, object?> WaitForResultcompletion(IEnumerable<string> tasksIdToWait)
  {
    var idToWait = tasksIdToWait as string[] ?? tasksIdToWait.ToArray();
    while (idToWait.Any(key => expectedResults_.ContainsKey(key) == false))
    {
      Thread.Sleep(1000);
    }

    return idToWait.Select(taskIdToWait => (taskIdToWait, expectedResults_[taskIdToWait]))
                   .ToDictionary(result => result.taskIdToWait,
                                 result => result.Item2);
  }
}
