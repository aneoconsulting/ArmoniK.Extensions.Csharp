using ArmoniK.DevelopmentKit.WorkerApi.Common.Adaptater;

using NUnit.Framework;

namespace WorkerApiTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestS3Unzip()
        {
          S3Adaptater adapter = new S3Adaptater("https://" + Amazon.RegionEndpoint.EUWest3.GetEndpointForService("s3").ToString(),
                                                "damdou",
                                                "XXXXXXXXXXXXXXXXXXX",
                                                "XXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                                                "");

          adapter.DownloadFile("ArmoniK.Samples.SymphonyPackage-v1.0.0.zip");
        }
    }
}