// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;

using JetBrains.Annotations;

namespace ArmoniK.Extensions.Common.StreamWrapper.Worker
{

  [PublicAPI]
  public interface ITaskHandler
  {
    string SessionId { get; }

    string TaskId { get; }

    IReadOnlyDictionary<string, string> TaskOptions { get; }

    byte[] Payload { get; }

    IReadOnlyDictionary<string, byte[]> DataDependencies { get; }

    ConfigurationReply Configuration { get; }

    Task CreateTasksAsync(IEnumerable<TaskRequest> tasks, TaskOptions taskOptions);

    Task<byte[]> RequestResource(string key);

    Task<byte[]> RequestCommonData(string key);

    Task<byte[]> RequestDirectData(string key);

    Task SendResult(string key, byte[] data);
  }
}
