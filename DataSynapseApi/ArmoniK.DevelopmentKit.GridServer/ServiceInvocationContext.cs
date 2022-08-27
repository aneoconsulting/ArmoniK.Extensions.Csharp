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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;


namespace ArmoniK.DevelopmentKit.GridServer
{
  /// <summary>
  /// The ServiceInvocationContext class provides an interface for interacting with
  /// an invocation, such as getting the session and task IDs, while it is running on an
  /// Engine.This is an alternative to using, for example, the system properties when
  /// running a Java Service.Using this class enables immediate updating of invocation
  /// information.In contrast, setting the INVOCATION_INFO system property only
  /// updates at the end of the invocation.
  /// The ServiceInvocationContext object can be reused; the method calls always
  /// apply to the currently executing Service Session and invocation.Make all method
  /// calls by a service, update, or init method; if not, the method call might throw
  /// an IllegalStateException or return invalid data.Note that you cannot call this
  /// method from a different thread; it will fail if it is not called from the main thread.
  /// </summary>
  [MarkDownDoc]
  public class ServiceInvocationContext
    {
      /// <summary>
      /// Get the sessionId created by an createSession call before. 
      /// </summary>
      public Session SessionId { get; set; }

      /// <summary>
      /// Check if the session is the same as previously created
      /// </summary>
      /// <param name="session"></param>
      /// <returns>Return boolean True if SessionId is null or equals to session parameters</returns>
      public bool IsEquals(string session)
        => SessionId != null && session != null && SessionId.Id.Equals(session);
    }
}
