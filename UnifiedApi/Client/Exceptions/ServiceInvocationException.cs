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

using ArmoniK.DevelopmentKit.Common;

//TODO : remove pragma
#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.Client.Exceptions
{
  [MarkDownDoc]
  public class ServiceInvocationException : Exception
  {
    private readonly string message_ = "ServiceInvocationException during call function";

    public ServiceInvocationException()
    {
    }

    public ServiceInvocationException(string message) => message_ = message;

    public ServiceInvocationException(Exception e) : base(e.Message,
                                                          e) => message_ = $"{message_} with InnerException {e.GetType()} message : {e.Message}";

    public ServiceInvocationException(string message, ArgumentException e) : base(message,
                                                                                  e)
      => message_ = message;

    //Overriding the Message property
    public override string Message => message_;
  }
}