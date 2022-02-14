using Amazon.S3;
using Amazon.S3.Model;

using ArmoniK.DevelopmentKit.Common;

using System;
using System.IO;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common.Adaptater
{
  public class S3Adaptater : IFileAdaptater
  {
    private AmazonS3Config ConfigAmazonS3 { get; set; }

    private string LocalZipDir { get; set; }

    private string RemoteS3Path { get; set; }

    private string BucketName { get; set; }

    private string AwsSessionToken { get; set; }

    private AmazonS3Client Client { get; set; }

    /// <summary>
    /// Get the directory where the file will be downloaded
    /// </summary>
    public string DestinationDirPath { get; set; }

    public S3Adaptater(string endPointRegion,
                       string bucketName,
                       string awsAccessKeyId,
                       string awsSecretAccessKey,
                       string remoteS3Path,
                       string localZipDir = "/tmp/packages/zip")
    {
      var config = new AmazonS3Config
      {
        ServiceURL = endPointRegion,
      };

      BucketName   = bucketName;
      RemoteS3Path = remoteS3Path;
      LocalZipDir  = localZipDir;

      Client = string.IsNullOrEmpty(awsAccessKeyId)
        ? new AmazonS3Client(config)
        : new AmazonS3Client(awsAccessKeyId,
                             awsSecretAccessKey,
                             config);

      DestinationDirPath = localZipDir;

      if (!Directory.Exists(DestinationDirPath))
      {
        Directory.CreateDirectory(DestinationDirPath);
      }
    }


    public async Task DownloadFileAsync(string fileName)
    {
      var ms = new MemoryStream();

      var r = await Client.GetObjectAsync(new GetObjectRequest()
      {
        BucketName = BucketName,
        Key        = fileName,
      });
      var stream2 = new BufferedStream(r.ResponseStream);

      var file = new FileStream(Path.Combine(LocalZipDir,
                                             fileName),
                                FileMode.Create,
                                FileAccess.Write);
      try
      {
        var buffer = new byte[0x2000];
        var count  = 0;
        while ((count = stream2.Read(buffer,
                                     0,
                                     buffer.Length)) >
               0)
        {
          ms.Write(buffer,
                   0,
                   count);
        }

        ms.WriteTo(file);
        file.Close();
        ms.Close();
      }
      catch (AmazonS3Exception amazonS3Exception)
      {
        if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
        {
          throw new Exception("Check the provided AWS Credentials.");
        }
        else
        {
          throw new Exception("Error occurred: " + amazonS3Exception.Message);
        }
      }
    }


    /// <summary>
    /// The method to download file from form remote server
    /// </summary>
    /// <param name="fileName">The filename with extension and without directory path</param>
    /// <returns>Returns the path where the file has been downloaded</returns>
    public string DownloadFile(string fileName)
    {
      DownloadFileAsync(fileName).Wait();

      return Path.Combine(DestinationDirPath,
                          fileName);
    }
  }
}