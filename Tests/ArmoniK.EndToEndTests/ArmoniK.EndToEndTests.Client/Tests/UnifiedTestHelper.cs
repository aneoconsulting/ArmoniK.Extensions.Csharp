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
  private readonly ConcurrentDictionary<string, object> expectedResults_ = new();

  public UnifiedTestHelper(EngineType engineType,
                           string     applicationNamespace,
                           string     applicationService)
    : base(engineType,
           applicationNamespace,
           applicationService)
    => InitService(engineType,
                   applicationNamespace,
                   applicationService);

  public ISubmitterService Service      { get; private set; }
  public ServiceAdmin      ServiceAdmin { get; private set; }

  public void HandleError(ServiceInvocationException e,
                          string                     taskId)
  {
    Log.LogError($"Error (ignore) from {taskId} : " + e.Message);
    expectedResults_[taskId] = e;
  }

  public void HandleResponse(object response,
                             string taskId)
    => expectedResults_[taskId] = response;

  public void InitService(EngineType engineType,
                          string     applicationNamespace,
                          string     applicationService)
  {
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
                                .CreateService(TaskOptions.ApplicationName,
                                               Props);
        ServiceAdmin = null;
        break;
      }
    }

    Log.LogInformation($"New session created : {Service.SessionId}");
  }

  internal object WaitForResultcompletion(string taskIdToWait)
    => WaitForResultcompletion(new[]
                               {
                                 taskIdToWait,
                               })
       .First()
       .Value;

  internal Dictionary<string, object> WaitForResultcompletion(IEnumerable<string> tasksIdToWait)
  {
    while (tasksIdToWait.Any(key => expectedResults_.ContainsKey(key) == false))
    {
      Thread.Sleep(1000);
    }

    return tasksIdToWait.Select(taskIdToWait => (taskIdToWait, expectedResults_[taskIdToWait]))
                        .ToDictionary(result => result.taskIdToWait,
                                      result => result.Item2);
  }
}
