using System;

using ArmoniK.Api.Client.Options;
using ArmoniK.Api.Client.Submitter;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   ClientServiceConnector is the class to connection to the control plane with different
///   like address,port, insecure connection, TLS, and mTLS
/// </summary>
public class ClientServiceConnector
{
  /// <summary>
  ///   Create a connection pool to the control plane with mTLS authentication
  /// </summary>
  /// <param name="properties">Configuration Properties</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns>The connection pool</returns>
  public static ChannelPool ControlPlaneConnectionPool(Properties                 properties,
                                                       [CanBeNull] ILoggerFactory loggerFactory = null)
  {
    var options = new GrpcClient
                  {
                    AllowUnsafeConnection = !properties.ConfSSLValidation,
                    CaCert                = properties.CaCertFilePem,
                    CertP12               = properties.ClientP12File,
                    CertPem               = properties.ClientCertFilePem,
                    KeyPem                = properties.ClientKeyFilePem,
                    Endpoint              = properties.ControlPlaneUri.ToString(),
                    OverrideTargetName    = properties.TargetNameOverride,
                  };

    if (properties.ControlPlaneUri.Scheme == Uri.UriSchemeHttps && options.AllowUnsafeConnection && string.IsNullOrEmpty(options.OverrideTargetName))
    {
#if NET5_0_OR_GREATER
      var doOverride = !string.IsNullOrEmpty(options.CaCert);
#else
      var doOverride = true;
#endif
      if (doOverride)
      {
        // Doing it here once to improve performance
        options.OverrideTargetName = GrpcChannelFactory.GetOverrideTargetName(options,
                                                                              GrpcChannelFactory.GetServerCertificate(properties.ControlPlaneUri,
                                                                                                                      options)) ?? "";
      }
    }


    return new ChannelPool(() => GrpcChannelFactory.CreateChannel(options,
                                                                  loggerFactory?.CreateLogger(typeof(ClientServiceConnector))));
  }
}
