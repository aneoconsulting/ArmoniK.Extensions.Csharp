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

using System;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Extensions.Common.StreamWrapper.Tests.Common;
using ArmoniK.Extensions.Common.StreamWrapper.Worker;

namespace ArmoniK.Extensions.Common.StreamWrapper.Tests.Server
{
  public class WorkerService : WorkerStreamWrapper
  {
    public override Task<Output> Process(ITaskHandler taskHandler)
    {
      var output = new Output();
      try
      {
        var payload = TestPayload.Deserialize(taskHandler.Payload);
        if (payload != null && payload.Type == TestPayload.TaskType.Compute)
        {
          int input = BitConverter.ToInt32(payload.DataBytes);
          var result = new TestPayload
          {
            Type      = TestPayload.TaskType.Result,
            DataBytes = BitConverter.GetBytes(input * input),
          };
          taskHandler.SendResult(taskHandler.TaskId,
                                 result.Serialize());
          output = new Output
          {
            Ok = new Empty()
          };
        }
      }
      catch (Exception ex)
      {
        output.Error = new Output.Types.Error
        {
          Details = ex.StackTrace,
        };
      }
      return Task.FromResult(output);
    }
  }
}