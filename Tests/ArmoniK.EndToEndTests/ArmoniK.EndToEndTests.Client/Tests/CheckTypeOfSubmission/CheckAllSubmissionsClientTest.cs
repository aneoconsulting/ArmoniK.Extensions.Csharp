using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ArmoniK.DevelopmentKit.Common.Extensions;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

using Assert = NUnit.Framework.Assert;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckTypeOfSubmission;

public class CheckAllSubmissionsClientTest
{
  public enum GetResultType
  {
    GetResult,
    TryGetResult,
  }

  public enum SubmissionType
  {
    Sequential,
    Batch,
  }

  private const string             ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckTypeOfSubmission";
  private const string             ApplicationService   = "ServiceContainer";
  private       SymphonyTestHelper symphonyTestHelper_;

  [SetUp]
  public void Setup()
    => symphonyTestHelper_ = new SymphonyTestHelper(ApplicationNamespace,
                                                    ApplicationService);


  [TearDown]
  public void Cleanup()
  {
  }

  [TestCase(10,
            2,
            SubmissionType.Sequential,
            GetResultType.GetResult)]
  [TestCase(200,
            0,
            SubmissionType.Batch,
            GetResultType.TryGetResult)]
  public void Check_That_All_Kinds_Of_Submissions_Are_Working(int            nbJob,
                                                              int            nbSubTasks,
                                                              SubmissionType submissionType,
                                                              GetResultType  getResultType)
  {
    var numbers = new List<int>
                  {
                    1,
                    2,
                    3,
                  };
    var clientPayloads = new ClientPayload
                         {
                           IsRootTask = true,
                           Numbers    = numbers,
                           NbSubTasks = nbSubTasks,
                           Type       = ClientPayload.TaskType.SubTask,
                         };

    //Prepare List of jobs
    var listOfPayload = new List<byte[]>();

    for (var i = 0; i < nbJob; i++)
    {
      listOfPayload.Add(clientPayloads.Serialize());
    }

    var expectedResult = numbers.Sum() * nbJob * Math.Max(nbSubTasks,
                                                          1);

    var result = SubmissionTask(listOfPayload,
                                nbJob,
                                nbSubTasks,
                                submissionType,
                                getResultType);
    Assert.That(result,
                Is.EqualTo(expectedResult));
  }


  private int SubmissionTask(List<byte[]>   listOfPayload,
                             int            nbJob,
                             int            nbSubTasks,
                             SubmissionType submissionType,
                             GetResultType  getResultType)
  {
    symphonyTestHelper_.Log.LogInformation($"==  Running {nbJob} Tasks with {nbSubTasks} subTasks " +
                                           $" {submissionType.GetName()} submit, Result method {getResultType.GetName()} =====");


    //Start Submission tasks
    var stopWatch = new Stopwatch();
    stopWatch.Start();
    IEnumerable<string> taskIds;
    if (submissionType == SubmissionType.Sequential)
    {
      taskIds = listOfPayload.Select(symphonyTestHelper_.SessionService.SubmitTask)
                             .ToArray();
    }
    else // (submissionType == SubmissionType.Batch)
    {
      taskIds = symphonyTestHelper_.SessionService.SubmitTasks(listOfPayload)
                                   .ToArray();
    }

    stopWatch.Stop();
    var ts = stopWatch.Elapsed;
    // Format and display the TimeSpan value.
    var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
    symphonyTestHelper_.Log.LogInformation("End of submission in " + elapsedTime);


    stopWatch.Start();
    symphonyTestHelper_.Log.LogInformation("Starting to retrieve the result : ");
    IEnumerable<Tuple<string, byte[]?>> results;

    if (getResultType == GetResultType.GetResult)
    {
      results = symphonyTestHelper_.WaitForTaskResults(taskIds);
    }
    else
    {
      results = symphonyTestHelper_.WaitForTasksResult(taskIds.ToList());
    }

    var tuples = results as Tuple<string, byte[]?>[] ?? results.ToArray();
    stopWatch.Stop();
    ts = stopWatch.Elapsed;
    // Format and display the TimeSpan value.
    elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
    symphonyTestHelper_.Log.LogInformation("Finished to get Results in " + elapsedTime);


    stopWatch.Start();

    symphonyTestHelper_.Log.LogInformation($"Starting to deserialize {tuples.Count()} results : ");

    var computedResult = tuples.Select(x => ClientPayload.Deserialize(x.Item2)
                                                         .Result)
                               .Sum();
    var nTasks = nbSubTasks > 0
                   ? nbSubTasks
                   : 1;
    return computedResult;
  }
}
