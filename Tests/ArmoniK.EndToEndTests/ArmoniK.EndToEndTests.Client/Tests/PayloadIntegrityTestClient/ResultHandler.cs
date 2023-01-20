using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Exceptions;

namespace ArmoniK.EndToEndTests.Client.Tests.PayloadIntegrityTestClient;

public delegate void HandleErrorType(ServiceInvocationException e,
                                     string                     taskId);

public delegate void HandleResponseType(object response,
                                        string taskId);

public class ResultHandler : IServiceInvocationHandler
{
  private readonly HandleErrorType    _onError;
  private readonly HandleResponseType _onResponse;

  public ResultHandler(HandleErrorType    onError,
                       HandleResponseType onResponse)
  {
    _onError    = onError;
    _onResponse = onResponse;
  }

  public void HandleError(ServiceInvocationException e,
                          string                     taskId)
    => _onError(e,
                taskId);

  public void HandleResponse(object response,
                             string taskId)
    => _onResponse(response,
                   taskId);
}
