using System;
using System.Collections;
using System.Collections.Generic;

using ArmoniK.Core.gRPC.V1;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  public class Properties
  {
    public Properties(IConfiguration configuration, TaskOptions options)
    {
      TaskOptions   = options;
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; set; }

    public Properties(string connectionAddress, int connectionPort, TaskOptions options)
    {
      ConnectionPort    = connectionPort;
      ConnectionAddress = connectionAddress;
      TaskOptions       = options;
    }

    public static TaskOptions DefaultTaskOptions = new()
    {
      MaxDuration = new Duration
      {
        Seconds = 300,
      },
      MaxRetries = 5,
      Priority   = 1,
      IdTag      = "ArmonikTag",
    };

    public string ConnectionString
    {
      get { return $"{Protocol}{ConnectionAddress}:{ConnectionPort}"; }
      set
      {
        string[] composedConnectionString = value.Split("//");
        if (composedConnectionString == null || composedConnectionString.Length <= 1)
          throw new ArgumentNullException($"Protocol not found in the ConnectionString");

        Protocol = composedConnectionString[0];

        string[] addressAndPort = composedConnectionString[1].Split(":");

        if (addressAndPort == null || addressAndPort.Length <= 1)
          throw new ArgumentException("Address and Port should be present in the connectionString");

        ConnectionAddress = addressAndPort[0];
        try
        {
          ConnectionPort = int.Parse(addressAndPort[1]);
        }
        catch (FormatException e)
        {
          Console.WriteLine(e);
          throw;
        }
      }
    }

    public string Protocol { get; set; }

    public string ConnectionAddress { get; set; }

    public int ConnectionPort { get; set; } = 5001;

    public TaskOptions TaskOptions { get; set; }
  }
}