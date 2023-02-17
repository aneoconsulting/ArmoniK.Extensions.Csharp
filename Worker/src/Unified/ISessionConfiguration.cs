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

using ArmoniK.Api.gRPC.V1;

namespace ArmoniK.DevelopmentKit.Worker.Unified;

/// <summary>
///   implementation of this interface in <see cref="TaskWorkerService" /> or your own implementation allows to have the
///   <see cref="TaskOptions" /> configured automatically by the <see cref="GridWorker" />
/// </summary>
public interface ISessionConfiguration
{
  /// <summary>
  ///   Get or Set SubSessionId object stored during the call of SubmitTask, SubmitSubTask,
  ///   SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
  /// </summary>
  public Session SessionId { get; set; }

  /// <summary>
  ///   Prepare Session and create SessionService with the specific session
  /// </summary>
  /// <param name="sessionId">The ID of the current session</param>
  /// <param name="requestTaskOptions">The TaskOptions of the current session</param>
  public void ConfigureSession(Session     sessionId,
                               TaskOptions requestTaskOptions);

  /// <summary>
  ///   The internal function onSessionEnter to openSession for clientService under GridWorker
  /// </summary>
  /// <param name="sessionContext"></param>
  public void OnSessionEnter(SessionContext sessionContext);

  /// <summary>
  ///   The middleware triggers the invocation of this handler to unbind the Service Instance from its owning Session.
  ///   This handler should do any cleanup for any resources that were used in the onSessionEnter() method.
  /// </summary>
  /// <param name="sessionContext">
  ///   Holds all information on the state of the session at the start of the execution such as session ID.
  /// </param>
  public void OnSessionLeave(SessionContext sessionContext);

  /// <summary>
  ///   The middleware triggers the invocation of this handler just after a Service Instance is started.
  ///   The application developer must put any service initialization into this handler.
  ///   Default implementation does nothing.
  /// </summary>
  /// <param name="serviceContext">
  ///   Holds all information on the state of the service at the start of the execution.
  /// </param>
  public void OnCreateService(ServiceContext serviceContext);

  /// <summary>
  ///   The middleware triggers the invocation of this handler just before a Service Instance is destroyed.
  ///   This handler should do any cleanup for any resources that were used in the onCreateService() method.
  /// </summary>
  /// <param name="serviceContext">
  ///   Holds all information on the state of the service at the start of the execution.
  /// </param>
  public void OnDestroyService(ServiceContext serviceContext);
}
