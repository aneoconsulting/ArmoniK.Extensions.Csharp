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
//   D. Brasseur       <dbrasseur@aneo.fr>
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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Extensions;
using ArmoniK.DevelopmentKit.Worker.Unified;
using ArmoniK.EndToEndTests.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Worker.Tests.AggregationPriority;

/// <summary>
///   AggregationPriority is a TaskWorkerService class that contains methods to compute basic array cube, reduce, reduce
///   cube, compute madd, compute matrix, compute vector, compute scalar, and aggregate results.
/// </summary>
[UsedImplicitly]
public class AggregationPriority : TaskWorkerService
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="AggregationPriority" /> class.
  /// </summary>
  public AggregationPriority()
  {
  }

  /// <summary>
  ///   Computes the basic array cube.
  /// </summary>
  /// <param name="inputs">The inputs.</param>
  /// <returns>An array of the cube of the inputs.</returns>
  public static double[] ComputeBasicArrayCube(double[] inputs)
    => inputs.Select(x => x * x * x)
             .ToArray();

  /// <summary>
  ///   Computes the reduce.
  /// </summary>
  /// <param name="inputs">The inputs.</param>
  /// <returns>The sum of the inputs.</returns>
  public static double ComputeReduce(double[] inputs)
    => inputs.Sum();

  /// <summary>
  ///   Computes the reduce cube.
  /// </summary>
  /// <param name="inputs">The inputs.</param>
  /// <param name="workloadTimeInMs">The workload time in ms.</param>
  /// <returns>The sum of the cube of the inputs.</returns>
  public static double ComputeReduceCube(double[] inputs,
                                         int      workloadTimeInMs = 10)
  {
    Thread.Sleep(workloadTimeInMs);

    return inputs.Select(x => x * x * x)
                 .Sum();
  }

  /// <summary>
  ///   Computes the reduce cube.
  /// </summary>
  /// <param name="inputs">The inputs.</param>
  /// <returns>The sum of the cube of the inputs.</returns>
  public static double ComputeReduceCube(byte[] inputs)
  {
    var doubles = inputs.ConvertToArray();

    return doubles.Select(x => x * x * x)
                  .Sum();
  }

  /// <summary>
  ///   Computes the madd.
  /// </summary>
  /// <param name="inputs1">The inputs1.</param>
  /// <param name="inputs2">The inputs2.</param>
  /// <param name="k">The k.</param>
  /// <returns>An array of the madd of the inputs.</returns>
  public static double[] ComputeMadd(byte[] inputs1,
                                     byte[] inputs2,
                                     double k)
  {
    var doubles1 = inputs1.ConvertToArray()
                          .ToArray();
    var doubles2 = inputs2.ConvertToArray()
                          .ToArray();


    return doubles1.Select((x,
                            idx) => k * x * doubles2[idx])
                   .ToArray();
  }

  /// <summary>
  ///   Non statics the compute madd.
  /// </summary>
  /// <param name="inputs1">The inputs1.</param>
  /// <param name="inputs2">The inputs2.</param>
  /// <param name="k">The k.</param>
  /// <returns>An array of the madd of the inputs.</returns>
  public double[] NonStaticComputeMadd(byte[] inputs1,
                                       byte[] inputs2,
                                       double k)
  {
    var doubles1 = inputs1.ConvertToArray()
                          .ToArray();
    var doubles2 = inputs2.ConvertToArray()
                          .ToArray();


    return doubles1.Select((x,
                            idx) => k * x * doubles2[idx])
                   .ToArray();
  }

  /// <summary>
  ///   Deserializes a byte array into a TaskPayload object.
  /// </summary>
  /// <param name="payload">The byte array to deserialize.</param>
  /// <returns>The deserialized TaskPayload object.</returns>
  /// <exception cref="ArgumentException">Thrown when the payload is null or empty.</exception>
  private static TaskPayload FromArmoniKPayload(byte[] payload)
  {
    if (payload == null || payload.Length == 0)
    {
      throw new ArgumentException($"Expected message type {nameof(TaskPayload)}",
                                  nameof(payload));
    }

    var taskPayload = JsonSerializer.Deserialize<TaskPayload>(payload);

    if (taskPayload is null)
    {
      throw new ArgumentException($"Expected message type {nameof(TaskPayload)}",
                                  nameof(payload));
    }

    return taskPayload;
  }

  /// <summary>
  ///   This method creates an ArmonikPayload object from a given method name and object argument.
  /// </summary>
  /// <param name="methodName">The name of the method.</param>
  /// <param name="argument">The argument object.</param>
  /// <returns>A byte array containing the ArmonikPayload object.</returns>
  private static byte[] ToArmoniKPayload(string methodName,
                                         object argument)
  {
    var payload = JsonSerializer.SerializeToUtf8Bytes(argument);
    return new ArmonikPayload
           {
             MethodName          = methodName,
             ClientPayload       = ProtoSerializer.SerializeMessageObject(payload),
             SerializedArguments = false,
           }.Serialize();
  }

  /// <summary>
  ///   Deserializes a TaskResult from a byte array.
  /// </summary>
  /// <param name="payload">The byte array to deserialize.</param>
  /// <returns>The deserialized TaskResult.</returns>
  /// <exception cref="System.ArgumentException">Thrown if the payload is null or empty.</exception>
  private static TaskResult FromTaskResult(byte[] payload)
  {
    if (payload == null || payload.Length == 0)
    {
      throw new ArgumentException($"Expected message type {nameof(TaskResult)}",
                                  nameof(payload));
    }

    var deprot     = ProtoSerializer.DeSerializeMessageObjectArray(payload);
    var taskResult = TaskResult.Deserialize(deprot[0] as byte[]);

    if (taskResult is null)
    {
      throw new ArgumentException($"Expected message type {nameof(TaskPayload)}",
                                  nameof(payload));
    }

    return taskResult;
  }

  /// <summary>
  ///   Computes a matrix of size squareMatrixSize.
  /// </summary>
  /// <param name="squareMatrixSize">The size of the matrix to be computed.</param>
  /// <returns>A byte array.</returns>
  public byte[] ComputeMatrix(int squareMatrixSize)
  {
    Logger.LogInformation("Enter: {MethodName}",
                          nameof(ComputeMatrix));

    var matrixLength = squareMatrixSize;

    /*
     * 1 2 3 ... n
     * . .       .
     * .  .      .
     * .    .    .
     * .       . .
     * .        ..
     * 1 2 3 ... n
     */
    var matrix = Enumerable.Repeat(Enumerable.Range(0,
                                                    matrixLength)
                                             .Select(e => e)
                                             .ToArray(),
                                   matrixLength)
                           .ToArray();

    var priorities = new List<int>();
    var prio       = 9;
    for (var i = 0; i < matrixLength; i++)
    {
      prio = (i) % ((matrixLength) / 8) == 0 ? prio-- < 1 ? 1 : prio : prio;
      prio = 1;

      priorities.Add(prio);
    }

    var payloads = matrix.Select(vector => ToArmoniKPayload(nameof(ComputeVector),
                                                            new TaskPayload
                                                            {
                                                              Vector = vector,
                                                            }));

    var subtaskIds = payloads.Select((p,
                                      n) =>
                                     {
                                       var taskOptions = new TaskOptions(TaskOptions)
                                                         {
                                                           Priority = priorities[n],
                                                         };
                                       return SubmitTask(p,
                                                         taskOptions: taskOptions);
                                     })
                             .ToArray();
    var aggPayload = ToArmoniKPayload(nameof(AggregateResults),
                                      new TaskPayload());

    var taskOptions = new TaskOptions(TaskOptions)
                      {
                        Priority = Math.Min(TaskOptions.Priority + 8,
                                            9),
                      };

    _ = SubmitTaskWithDependencie(aggPayload,
                                  subtaskIds,
                                  true,
                                  taskOptions: taskOptions);

    Logger.LogInformation("Exit: {MethodName} returning void",
                          nameof(ComputeMatrix));
    return null;
  }

  /// <summary>
  ///   Computes a vector based on a given payload.
  /// </summary>
  /// <param name="payload">The payload to be used for the computation.</param>
  /// <returns>A byte array containing the result of the computation.</returns>
  public byte[] ComputeVector(byte[] payload)
  {
    Logger.LogInformation("Enter: {MethodName}",
                          nameof(ComputeVector));

    var taskPayload = FromArmoniKPayload(payload);

    var workloadTimeInMs = taskPayload.WorkloadTimeInMs;
    var vector           = taskPayload.Vector;
    Thread.Sleep(workloadTimeInMs);

    var rd = new Random();
    var payloads = vector.Select(scalar => ToArmoniKPayload(nameof(ComputeScalar),
                                                            new TaskPayload
                                                            {
                                                              Scalar = scalar,
                                                              WorkloadTimeInMs = rd.Next(5,
                                                                                         300),
                                                            }));
    var subtaskIds = payloads.Select(p => SubmitTask(p))
                             .ToArray();
    var aggPayload = ToArmoniKPayload(nameof(AggregateResults),
                                      new TaskPayload());

    var taskOptions = TaskOptions.Clone();
    taskOptions.Priority = Math.Min(TaskOptions.Priority + 8,
                                    9);

    _ = SubmitTaskWithDependencie(aggPayload,
                                  subtaskIds,
                                  true,
                                  taskOptions: taskOptions);

    Logger.LogInformation("Exit: {MethodName} returning void",
                          nameof(ComputeVector));
    return null;
  }

  /// <summary>
  ///   Computes the scalar value of a given payload.
  /// </summary>
  /// <param name="payload">The payload to compute the scalar value of.</param>
  /// <returns>The scalar value of the payload.</returns>
  public byte[] ComputeScalar(byte[] payload)
  {
    Logger.LogInformation("Enter: {MethodName}",
                          nameof(ComputeScalar));
    var taskPayload = FromArmoniKPayload(payload);

    var workloadTimeInMs = taskPayload.WorkloadTimeInMs;
    var scalar           = taskPayload.Scalar;
    Thread.Sleep(workloadTimeInMs);
    var result = scalar * scalar;

    Logger.LogInformation("Exit: {MethodName} ComputeScalar : {result}",
                          nameof(ComputeScalar),
                          result);
    var taskResult = new TaskResult
                     {
                       Result   = result,
                       Priority = TaskOptions.Priority,
                     };
    var byteResult = taskResult.Serialize();

    return byteResult;
  }

  /// <summary>
  ///   Aggregates the results of a task payload.
  /// </summary>
  /// <param name="payload">The payload to aggregate.</param>
  /// <returns>The aggregated results.</returns>
  public byte[] AggregateResults(byte[] payload)
  {
    Logger.LogInformation("Enter: {MethodName}",
                          nameof(AggregateResults));
    var taskPayload = FromArmoniKPayload(payload);

    var workloadTimeInMs = taskPayload.WorkloadTimeInMs;
    Thread.Sleep(workloadTimeInMs);

    var resultsToAgg = TaskContext.DataDependencies.Values.ToList()
                                  .Select(FromTaskResult)
                                  .ToArray();
    var sum = resultsToAgg.Select(t =>
                                  {
                                    Logger.LogInformation("parent value {res} and test is [{test}]",
                                                          t.Result,
                                                          t.ResultString);
                                    return t.Result;
                                  })
                          .Sum();

    Logger.LogInformation("Exit: {MethodName} with sum {sum}",
                          nameof(AggregateResults),
                          sum);

    return new TaskResult
           {
             ResultString = "agg",
             Result       = sum,
             Priority     = TaskOptions.Priority,
           }.Serialize();
  }
}
