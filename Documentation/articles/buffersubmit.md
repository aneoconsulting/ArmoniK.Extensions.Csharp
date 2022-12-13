# Buffering submission

## Introduction
A feature in Armonik.Extensions.Csharp allows batch submission of tasks even if the calls to submission are made iteratively. It is possible to buffer task submissions by the client when sending them.

In the situation where a client cannot control its iterative task submission calls, two function calls have been made available in ArmoniK.DevelopmentKit.Client.Unified. 

The asynchronous methods SubmitAsync allows first to iteratively submit tasks without waiting for the taskId results. 
This buffer system is based on a storage mechanism for tasks and 2 triggers which is Capacity of buffer and timeout trigger. The SDK then takes care of sending the tasks according to two criteria; If the number of tasks exceeds the capacity of the buffer or if the cumulative time of SubmitAsync functions calls exceeds a waiting time before sending the buffer ; The buffer will be partially filled and submitted with a number of tasks lower than the capacity buffer. A new buffer will then be made available to create a new batch of sends

## Buffer configuration

The buffering can be set from the object `Properties` provided when the Service is created
Here below an example of configuration : 
```csharp=
var props = new Properties(TaskOptions,
                             configuration.GetSection("Grpc")["EndPoint"])
              {
                MaxConcurrentBuffer = 10,
                MaxTasksPerBuffer   = 50,
                MaxParallelChannel  = 5,
                TimeTriggerBuffer   = TimeSpan.FromSeconds(10),
              };
```
where :
`MaxConcurrentBuffer` is the number of buffers that can be filled in asynchronous submitAsync calls

`MaxTasksPerBuffer` is the capacity of one buffer to contain tasks before a sending trigger

`MaxParallelChannel` is number of parallel grpc channel active able to submit in parallel

`TimeTriggerBuffer` is a TimeSpan to trigger a buffer to be sended over a grpc channel

In this example there 10 buffers of 50 tasks that will be sent over 5 Grpc channel in parallel. In antoher words 2 buffer of 50 will be prepared by Grpc channel

## Submission method
The functions are the following:

The method below will asynchronously send the task without arguments serialization
```csharp=
public async Task<string> SubmitAsync(string                    methodName,
                                      byte[]                    argument,
                                      IServiceInvocationHandler handler,
                                      CancellationToken         token = default)
```


The method below will asynchronously send the task and serialize arguments objects
```csharp=
public async Task<string> SubmitAsync(string                    methodName,
                                        object[]                  argument,
                                        IServiceInvocationHandler handler,
                                        CancellationToken         token = default)
```




## Example
Find below an example to configure and send tasks iteratively but sent by buffer 

### Creation and configuration Unified service

This is the instanciation and configuration of Unified service
```csharp=
 public class StressTests
  {
    public StressTests(IConfiguration configuration,
                       ILoggerFactory factory,
                       string         partition)
    {
      TaskOptions = new TaskOptions
                    {
                      MaxDuration = new Duration
                                    {
                                      Seconds = 3600 * 24,
                                    },
                      MaxRetries           = 3,
                      Priority             = 1,
                      EngineType           = EngineType.Unified.ToString(),
                      ApplicationVersion   = "1.0.0-700",
                      ApplicationService   = "ServiceApps",
                      ApplicationName      = "Armonik.Samples.StressTests.Worker",
                      ApplicationNamespace = "Armonik.Samples.StressTests.Worker",
                      PartitionId          = partition,
                    };

      var props = new Properties(TaskOptions,
                             configuration.GetSection("Grpc")["EndPoint"])
              {
                MaxConcurrentBuffer = 5,
                MaxTasksPerBuffer   = 50,
                MaxParallelChannel  = 5,
                TimeTriggerBuffer   = TimeSpan.FromSeconds(10),
              };

      Logger = factory.CreateLogger<StressTests>();

      Service = ServiceFactory.CreateService(Props,
                                             factory);

      ResultHandle = new ResultForStressTestsHandler(Logger);
    }
```
### Example of execution tasks
Here is the complete code to send list of tasks : 

```csharp=
/// <summary>
    ///   The first test developed to validate dependencies subTasking
    /// </summary>
    /// <param name="nbTasks">The number of task to submit</param>
    /// <param name="nbInputBytes">The number of element n x M in the vector</param>
    /// <param name="nbOutputBytes">The number of bytes to expect as result</param>
    /// <param name="workloadTimeInMs">The time spent to compute task</param>
    private IDisposable ComputeVector(int  nbTasks,
                                      long nbInputBytes,
                                      long nbOutputBytes    = 8,
                                      int  workloadTimeInMs = 1)
    {
      var       indexTask = 0;
      const int elapsed   = 30;

      var inputArrayOfBytes = Enumerable.Range(0,
                                               (int)(nbInputBytes / 8))
                                        .Select(x => Math.Pow(42.0 * 8 / nbInputBytes,
                                                              1        / 3.0))
                                        .ToArray();

      Logger.LogInformation($"===  Running from {nbTasks} tasks with payload by task {nbInputBytes / 1024.0} Ko Total : {nbTasks * nbInputBytes / 1024.0} Ko...   ===");
      var sw = Stopwatch.StartNew();
      var periodicInfo = Utils.PeriodicInfo(() =>
                                            {
                                              Logger.LogInformation($"Got {ResultHandle.NbResults} results. All tasks submitted ? {(indexTask == nbTasks).ToString()}");
                                            },
                                            elapsed);

      var result = Enumerable.Range(0,
                                    nbTasks)
                             .Chunk(nbTasks / Props.MaxParallelChannel)
                             .AsParallel()
                             .Select(subInt => subInt.Select(idx => Service.SubmitAsync("ComputeWorkLoad",
                                                                                        Utils.ParamsHelper(inputArrayOfBytes,
                                                                                                           nbOutputBytes,
                                                                                                           workloadTimeInMs),
                                                                                        ResultHandle))
                                                     .ToList());

      var taskIds = result.SelectMany(t => Task.WhenAll(t)
                                               .Result)
                          .ToHashSet();


      indexTask = taskIds.Count();

      Logger.LogInformation($"{taskIds.Count}/{nbTasks} tasks executed in : {sw.ElapsedMilliseconds / 1000.0:0.00} secs with Total bytes {nbTasks * nbInputBytes / 1024.0:0.00} Ko");

      return periodicInfo;
    }

```

### Result task handler 
```csharp=
private class ResultForStressTestsHandler : IServiceInvocationHandler
    {
      private readonly ILogger<StressTests> Logger_;

      public ResultForStressTestsHandler(ILogger<StressTests> Logger)
        => Logger_ = Logger;

      public int    NbResults { get; private set; }
      public double Total     { get; private set; }

      /// <summary>
      ///   The callBack method which has to be implemented to retrieve error or exception
      /// </summary>
      /// <param name="e">The exception sent to the client from the control plane</param>
      /// <param name="taskId">The task identifier which has invoke the error callBack</param>
      public void HandleError(ServiceInvocationException e,
                              string                     taskId)

      {
        if (e.StatusCode == ArmonikStatusCode.TaskCanceled)
        {
          Logger_.LogWarning($"Warning from {taskId} : " + e.Message);
        }
        else
        {
          Logger_.LogError($"Error from {taskId} : " + e.Message);
          throw new ApplicationException($"Error from {taskId}",
                                         e);
        }
      }

      /// <summary>
      ///   The callBack method which has to be implemented to retrieve response from the server
      /// </summary>
      /// <param name="response">The object receive from the server as result the method called by the client</param>
      /// <param name="taskId">The task identifier which has invoke the response callBack</param>
      public void HandleResponse(object response,
                                 string taskId)

      {
        switch (response)
        {
          case double[] doubles:
            Total += doubles.Sum();
            break;
          case null:
            Logger_.LogInformation("Task finished but nothing returned in Result");
            break;
        }

        NbResults++;
      }
    }
```


### Result output will be 

```log=
[10:37:01 INF] ===  Running from 224 tasks with payload by task 3935.3662109375 Ko Total : 881522.03125 Ko...   ===
[10:37:01 INF] Got 0 results. All tasks submitted ? False
[10:37:02 INF] Submitting buffer of 50 task...
[10:37:02 INF] Submitting buffer of 50 task...
[10:37:02 INF] Connecting to armoniK  : https:/xxxxxxxx:5001/ port : 5001
[10:37:02 INF] HTTPS Activated: False
[10:37:02 INF] Created and acquired new channel Grpc.Net.Client.GrpcChannel from pool
[10:37:12 INF] Submitting buffer of 12 task...
[10:37:12 INF] Connecting to armoniK  : https:/xxxxxxxx:5001/ port : 5001
[10:37:12 INF] HTTPS Activated: False
[10:37:12 INF] Created and acquired new channel Grpc.Net.Client.GrpcChannel from pool

...

[10:43:39 INF] Submitting buffer of 50 task...
[10:43:39 INF] Submitting buffer of 50 task...
[10:43:40 INF] Submitting buffer of 12 task...
[10:43:53 INF] Connecting to armoniK  : https:/xxxxxxxx:5001/ port : 5001
[10:43:53 INF] HTTPS Activated: False
[10:43:53 INF] Created and acquired new channel Grpc.Net.Client.GrpcChannel from pool
[10:44:01 INF] Got 0 results. All tasks submitted ? False
[10:44:31 INF] Got 6 results. All tasks submitted ? False
[10:45:01 INF] Got 6 results. All tasks submitted ? False
[10:45:31 INF] Got 12 results. All tasks submitted ? False

...

[10:50:19 INF] 224/224 tasks executed in : 798.28 secs with Total bytes 881522.03 Ko

...

[11:07:31 INF] Got 174 results. All tasks submitted ? True
[11:07:46 INF] Got 224 results. All tasks submitted ? True
[11:07:46 INF] Total result is 9407.98365779803
```
