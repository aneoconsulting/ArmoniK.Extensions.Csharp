// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
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
