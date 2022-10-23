using System;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Client.Common.Submitter.Tools;

using Grpc.Core;
using Grpc.Net.Client;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

#if NET5_0_OR_GREATER

#else
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Net;
#endif

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

public class Http2CustomHandler : WinHttpHandler
{
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                         CancellationToken  cancellationToken)
  {
    request.Version = new Version("2.0");
    return base.SendAsync(request,
                          cancellationToken);
  }
}

/// <summary>
///   ClientServiceConnector is the class to connection to the control plane with different
///   like address,port, insecure connection, TLS, and mTLS
/// </summary>
public class ClientServiceConnector
{
  /// <summary>
  ///   Open connection with the control plane with or without SSL and no mTLS
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="sslValidation">Optional : Check if the ssl must have a strong validation (default true)</param>
  /// <param name="loggerFactory">Optional : the logger factory to create the logger</param>
  /// <returns></returns>
  private static ChannelBase ControlPlaneConnection(string         endPoint,
                                                    bool           sslValidation = true,
                                                    ILoggerFactory loggerFactory = null)
    => ControlPlaneConnection(endPoint,
                              "",
                              "",
                              sslValidation,
                              loggerFactory);

  /// <summary>
  ///   Open Connection with the control plane with mTLS authentication
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="clientCertFilename">The certificate filename in a pem format</param>
  /// <param name="clientKeyFilename">The client key filename in a pem format</param>
  /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns></returns>
  private static ChannelBase ControlPlaneConnection(string                     endPoint,
                                                    string                     clientCertFilename,
                                                    string                     clientKeyFilename,
                                                    bool                       sslValidation = true,
                                                    [CanBeNull] ILoggerFactory loggerFactory = null)
  {
    var logger = loggerFactory?.CreateLogger<ClientServiceConnector>();
    if ((!string.IsNullOrEmpty(clientCertFilename) && string.IsNullOrEmpty(clientKeyFilename)) ||
        (string.IsNullOrEmpty(clientCertFilename)  && !string.IsNullOrEmpty(clientKeyFilename)))
    {
      throw new ArgumentException("Missing path to one of certificate file. Please the check path to files");
    }

    Tuple<string, string> clientPem = null;

    if (!string.IsNullOrEmpty(clientCertFilename) && !string.IsNullOrEmpty(clientKeyFilename))
    {
      try
      {
        var clientCertPem = File.ReadAllText(clientCertFilename);
        var clientKeyPem  = File.ReadAllText(clientKeyFilename);
        clientPem = Tuple.Create(clientCertPem,
                                 clientKeyPem);
      }
      catch (Exception e)
      {
        logger?.LogError(e,
                         "Fail to read certificate file");
        throw;
      }
    }

    var uri = new Uri(endPoint);
    logger?.LogInformation($"Connecting to armoniK  : {uri} port : {uri.Port}");
    logger?.LogInformation($"HTTPS Activated: {uri.Scheme == Uri.UriSchemeHttps}");

    if (!string.IsNullOrEmpty(clientCertFilename))
    {
      logger?.LogInformation("mTLS Activated: properties_.ClientCertFilePem");
    }


    return ControlPlaneConnection(endPoint,
                                  clientPem,
                                  sslValidation,
                                  loggerFactory);
  }

  /// <summary>
  ///   Open Connection with the control plane with mTLS authentication
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="clientPem">The pair certificate + key data in a pem format</param>
  /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns></returns>
  private static ChannelBase ControlPlaneConnection(string                     endPoint,
                                                    Tuple<string, string>      clientPem     = null,
                                                    bool                       sslValidation = true,
                                                    [CanBeNull] ILoggerFactory loggerFactory = null)
  {
    var uri = new Uri(endPoint);

#if NET5_0_OR_GREATER
    var httpClientHandler = new SocketsHttpHandler
                            {
                              PooledConnectionIdleTimeout    = Timeout.InfiniteTimeSpan,
                              KeepAlivePingDelay             = TimeSpan.FromSeconds(60),
                              KeepAlivePingTimeout           = TimeSpan.FromSeconds(30),
                              EnableMultipleHttp2Connections = true,
                              MaxConnectionsPerServer        = 100,
                            };

    if (!sslValidation)
    {
      //To activate unSecured certificate
      //https://dev.to/tswiftma/switching-from-httpclienthandler-to-socketshttphandler-17h3
      httpClientHandler.SslOptions = new SslClientAuthenticationOptions
                                     {
                                       // Leave certs unvalidated for debugging
                                       RemoteCertificateValidationCallback = delegate
                                                                             {
                                                                               return true;
                                                                             },
                                       ClientCertificates = new X509CertificateCollection(),
                                     };
      AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
                           true);
    }

    if (clientPem != null)
    {
      var cert = CertUtils.GetClientCertFromPem(clientPem.Item1,
                                                clientPem.Item2);

      httpClientHandler.SslOptions.ClientCertificates = new X509CertificateCollection
                                                        {
                                                          cert,
                                                        };
    }
#else
    //Since netstandard2.0 doesn't have SocketHttpHandler for performance.
    //HttpClientHandler will be used instead

    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      throw new InvalidOperationException($"What configuration we missed ? : {RuntimeInformation.OSDescription} : {RuntimeInformation.FrameworkDescription}");
    }


    var innerHttpClientHandler = new WinHttpHandler();


    if (!sslValidation)
    {
      innerHttpClientHandler.ServerCertificateValidationCallback += (httpRequestMessage,
                                                                     cert,
                                                                     cetChain,
                                                                     policyErrors) =>
                                                                    {
                                                                      return true;
                                                                    };

      AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
                           true);
    }


    if (clientPem != null)
    {
      var cert = CertUtils.GetClientCertFromPem(clientPem.Item1,
                                                clientPem.Item2);
      var tmpCert = new X509Certificate2(cert.Export(X509ContentType.Pfx));
      cert.Dispose();
      innerHttpClientHandler.ClientCertificates.Add(tmpCert);
      innerHttpClientHandler.ClientCertificateOption = ClientCertificateOption.Manual;
      ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
      innerHttpClientHandler.SslProtocols = SslProtocols.Tls12         | SslProtocols.Tls11         | SslProtocols.Tls;
    }

    var httpClientHandler = innerHttpClientHandler;

#endif

    var channelOptions = new GrpcChannelOptions
                         {
                           Credentials = uri.Scheme == Uri.UriSchemeHttps
                                           ? new SslCredentials()
                                           : ChannelCredentials.Insecure,
                           HttpHandler   = httpClientHandler,
                           LoggerFactory = loggerFactory,
                         };
    return GrpcChannel.ForAddress(endPoint,
                                  channelOptions);
  }

  /// <summary>
  ///   Create a connection pool to the control plane with or without SSL and no mTLS
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="sslValidation">Optional : Check if the ssl must have a strong validation (default true)</param>
  /// <param name="loggerFactory">Optional : the logger factory to create the logger</param>
  /// <returns></returns>
  public static ChannelPool ControlPlaneConnectionPool(string         endPoint,
                                                       bool           sslValidation = true,
                                                       ILoggerFactory loggerFactory = null)
    => new(() => ControlPlaneConnection(endPoint,
                                        sslValidation,
                                        loggerFactory),
           loggerFactory);


  /// <summary>
  ///   Create a connection pool to the control plane with mTLS authentication
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="clientCertFilename">The certificate filename in a pem format</param>
  /// <param name="clientKeyFilename">The client key filename in a pem format</param>
  /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns></returns>
  public static ChannelPool ControlPlaneConnectionPool(string                     endPoint,
                                                       string                     clientCertFilename,
                                                       string                     clientKeyFilename,
                                                       bool                       sslValidation = true,
                                                       [CanBeNull] ILoggerFactory loggerFactory = null)
    => new(() => ControlPlaneConnection(endPoint,
                                        clientCertFilename,
                                        clientKeyFilename,
                                        sslValidation,
                                        loggerFactory),
           loggerFactory);

  /// <summary>
  ///   Create a connection pool to the control plane with mTLS authentication
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="clientPem">The pair certificate + key data in a pem format</param>
  /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns>The connection pool</returns>
  public static ChannelPool ControlPlaneConnectionPool(string                     endPoint,
                                                       Tuple<string, string>      clientPem     = null,
                                                       bool                       sslValidation = true,
                                                       [CanBeNull] ILoggerFactory loggerFactory = null)
    => new(() => ControlPlaneConnection(endPoint,
                                        clientPem,
                                        sslValidation,
                                        loggerFactory),
           loggerFactory);
}
