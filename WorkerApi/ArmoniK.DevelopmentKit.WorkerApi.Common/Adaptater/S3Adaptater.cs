using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common.Adaptater
{
  public class Program
  {
    private static IAmazonS3 client;
    private const  string    bucketName = "my-bucket";
    private const  string    keyName    = "first_key";
    private const  string    keyName1   = "first_key";
    private const  string    keyName2   = "second_key";

    private const string filePath = @"to destination";

    public static void Main()
    {
      //client = new AmazonS3Client(bucketRegion);
      AmazonS3Config config = new AmazonS3Config();
      config.ServiceURL = "EndPoint";
      var client = new AmazonS3Client(
        "XXXXXXXXXXXXXXXXXXX",
        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
      config

      );
      WritingAnObjectAsync().Wait();
      ReadObjectDataAsync().Wait();
    }
    static async Task ReadObjectDataAsync()
    {
      string responseBody = "";
      try
      {
        GetObjectRequest request = new GetObjectRequest
        {
          BucketName = bucketName,
          Key        = keyName
        };
        using var       response       = await client.GetObjectAsync(request);
        await using var responseStream = response.ResponseStream;
        using var       reader         = new StreamReader(responseStream);
        var          title          = response.Metadata["x-amz-meta-title"]; // Assume you have "title" as medata added to the object.
        var          contentType    = response.Headers["Content-Type"];
        Console.WriteLine("Object metadata, Title: {0}",
                          title);
        Console.WriteLine("Content type: {0}",
                          contentType);
        responseBody = reader.ReadToEnd(); // Now you process the response body.
      }
      catch (AmazonS3Exception e)
      {
        // If bucket or object does not exist
        Console.WriteLine("Error encountered ***. Message:'{0}' when reading object",
                          e.Message);
      }
      catch (Exception e)
      {
        Console.WriteLine("Unknown encountered on server. Message:'{0}' when reading object",
                          e.Message);
      }
    }

    static async Task WritingAnObjectAsync()
    {
      try
      {
        // 1. Put object-specify only key name for the new object.
        var putRequest1 = new PutObjectRequest
        {
          BucketName  = bucketName,
          Key         = keyName1,
          ContentBody = "sample text"
        };
        PutObjectResponse response1 = await client.PutObjectAsync(putRequest1);
        // 2. Put the object-set ContentType and add metadata.
        var putRequest2 = new PutObjectRequest
        {
          BucketName  = bucketName,
          Key         = keyName2,
          FilePath    = filePath,
          ContentType = "text/plain"
        };
        putRequest2.Metadata.Add("x-amz-meta-title",
                                 "someTitle");
        PutObjectResponse response2 = await client.PutObjectAsync(putRequest2);
      }
      catch (AmazonS3Exception e)
      {
        Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object",
                          e.Message);
      }
      catch (Exception e)
      {
        Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object",
                          e.Message);
      }
    }
  }
}