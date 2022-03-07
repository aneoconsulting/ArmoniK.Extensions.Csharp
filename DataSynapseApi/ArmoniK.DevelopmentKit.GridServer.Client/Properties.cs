using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using System;

//TODO : remove pragma
#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  [MarkDownDoc]
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
    };

    public string ConnectionString
    {
      get { return $"{Protocol}{ConnectionAddress}:{ConnectionPort}"; }
      set
      {
        var uri = new Uri(value);

        Protocol = uri.Scheme;

        ConnectionAddress = uri.Host;
        try
        {
          ConnectionPort = uri.Port;
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