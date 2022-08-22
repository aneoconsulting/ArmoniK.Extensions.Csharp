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

using ArmoniK.DevelopmentKit.SymphonyApi;
using ArmoniK.DevelopmentKit.SymphonyApi.api;
using ArmoniK.EndToEndTests.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ArmoniK.EndToEndTests.Tests.CheckTypeOfSubmission
{
  public class ServiceContainer : ServiceContainerBase
  {
    public override void OnCreateService(ServiceContext serviceContext)
    {
      //END USER PLEASE FIXME
    }

    public override void OnSessionEnter(SessionContext sessionContext)
    {
      //END USER PLEASE FIXME
    }


    private static double ExpM1(double x)
    {
      return ((((((((((((((15.0 + x) * x + 210.0) * x + 2730.0) * x + 32760.0) * x + 360360.0) * x + 3603600.0) * x + 32432400.0) * x + 259459200.0) * x +
                   1816214400.0) *
                  x +
                  10897286400.0) *
                 x +
                 54486432000.0) *
                x +
                217945728000.0) *
               x +
               653837184000.0) *
              x +
              1307674368000.0) *
             x *
             7.6471637318198164759011319857881e-13;
    }

    public byte[] ComputeSum(TaskContext taskContext, ClientPayload clientPayload)
    {
      Logger.LogInformation($"Enter in function : ComputeSum");

      if (clientPayload.Numbers.Count == 0)
        return new ClientPayload
          {
            Type   = ClientPayload.TaskType.Result,
            Result = 0,
          }
          .Serialize(); // Nothing to do


      ClientPayload aggPayload = new()
      {
        Type   = ClientPayload.TaskType.Result,
        Result = clientPayload.Numbers.Sum(),
      };

      return aggPayload.Serialize(); //nothing to do
    }

    private byte[] AggregateValues(TaskContext taskContext)
    {
      Logger.LogInformation($"Aggregate Task from Dependencies TaskIds : [{string.Join(", ", taskContext.DependenciesTaskIds)}]");

      var sum = taskContext.DataDependencies?.Select(x => ClientPayload.Deserialize(x.Value).Result).Sum() ?? 0;


      ClientPayload childResult = new()
      {
        Type   = ClientPayload.TaskType.Result,
        Result = sum,
      };

      return childResult.Serialize();
    }

    public override byte[] OnInvoke(SessionContext sessionContext, TaskContext taskContext)
    {
      var payload = ClientPayload.Deserialize(taskContext.TaskInput);

      switch (payload.Type)
      {
        case ClientPayload.TaskType.SubTask when payload.NbSubTasks > 0:
        {
          var subPayload = new ClientPayload()
          {
            Type    = ClientPayload.TaskType.None,
            Numbers = payload.Numbers
          }.Serialize();

          var listPayload = new List<byte[]>();

          for (var i = 0; i < payload.NbSubTasks; i++)
          {
            listPayload.Add(subPayload);
          }

          var taskIds = SubmitTasks(listPayload);

          var aggPayload = new ClientPayload()
          {
            Type = ClientPayload.TaskType.Aggregation,
          };

          this.SubmitTaskWithDependencies(aggPayload.Serialize(),
                                          taskIds.Select(id => id.ResultIds.Single()).ToList(), true);

          return null; //Delegate to subTasks
        }
        case ClientPayload.TaskType.SubTask:
          return new ClientPayload
            {
              Type   = ClientPayload.TaskType.Result,
              Result = payload.Numbers.Sum(),
            }
            .Serialize(); //nothing to do
        case ClientPayload.TaskType.Aggregation:
          return AggregateValues(taskContext);

        default:
          return new ClientPayload
            {
              Type   = ClientPayload.TaskType.Result,
              Result = payload.Numbers.Sum(),
            }
            .Serialize(); //nothing to do
      }
      /////////////////// TO SERVER SIDE TEST HERE //////////////////////////////////////////
    }


    public override void OnSessionLeave(SessionContext sessionContext)
    {
      //END USER PLEASE FIXME
    }

    public override void OnDestroyService(ServiceContext serviceContext)
    {
      //END USER PLEASE FIXME
    }
  }
}