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

using System.Collections.Generic;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   Common Interface for Submitter services.
/// </summary>
public interface ISubmitterService
{
  /// <summary>
  ///   The Id of the current session
  /// </summary>
  string SessionId { get; }

  /// <summary>
  ///   The method submit will execute task asynchronously on the server
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">A list of objects that can be passed in parameters of the function</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <returns>Return the taskId string</returns>
  string Submit(string                    methodName,
                object[]                  arguments,
                IServiceInvocationHandler handler);

  /// <summary>
  ///   The method submits a list of task with a list of arguments for each task which will be serialized into a byte[] for
  ///   each call
  ///   MethodName(byte[] argument)
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">A list of parameters that can be passed in parameters of the each call of function</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <returns>Return the list of created taskIds</returns>
  IEnumerable<string> Submit(string                    methodName,
                             IEnumerable<object[]>     arguments,
                             IServiceInvocationHandler handler);
}
