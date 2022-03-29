using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;

using System;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  [MarkDownDoc]
  public class Properties
  {
    public Uri ControlPlaneUri { get; set; }

    /// <summary>
    /// Returns the section key Grpc from appSettings.json
    /// </summary>
    private static string SectionControlPlan { get; } = "Grpc";

    private static string SectionEndPoint { get; } = "Endpoint";

    public Properties(TaskOptions options,
                      string      connectionAddress = null,
                      int         connectionPort    = 0,
                      string      protocol          = null) : this(new ConfigurationBuilder().Build(),
                                                                   options,
                                                                   connectionAddress,
                                                                   connectionPort,
                                                                   protocol)
    {
    }

    public Properties(IConfiguration configuration,
                      TaskOptions    options,
                      string         connectionAddress = null,
                      int            connectionPort    = 0,
                      string         protocol          = null)
    {
      TaskOptions   = options;
      Configuration = configuration;

      try
      {
        ConnectionString = configuration.GetSection(SectionControlPlan)[SectionEndPoint];
      }
      catch (Exception)
      {
        ConnectionString = $"err://NoEndPoint:0";
      }

      if (connectionAddress != null)
      {
        ConnectionAddress = connectionAddress;
      }

      if (connectionPort != 0) ConnectionPort = connectionPort;
      if (protocol != null) Protocol          = protocol;

      //Check if Uri is correct
      if (Protocol == "err://" || ConnectionAddress == "NoEndPoint" || ConnectionPort == 0)
        throw new ArgumentException($"Issue with the connection point : {ConnectionString}");

      ControlPlaneUri = new Uri(ConnectionString);
    }

    public IConfiguration Configuration { get; set; }

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
      get { return $"{Protocol}://{ConnectionAddress}:{ConnectionPort}"; }
      set
      {
        try
        {
          var uri = new Uri(value);

          Protocol = uri.Scheme;

          ConnectionAddress = uri.Host;
          ConnectionPort    = uri.Port;
        }
        catch (FormatException e)
        {
          Console.WriteLine(e);
          throw;
        }
      }
    }

    public string Protocol { get; set; } = "http";

    public string ConnectionAddress { get; set; }

    public int ConnectionPort { get; set; } = 5001;

    public TaskOptions TaskOptions { get; set; }
  }
}