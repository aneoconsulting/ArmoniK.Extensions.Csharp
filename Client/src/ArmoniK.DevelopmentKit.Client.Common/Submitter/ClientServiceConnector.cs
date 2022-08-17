using System;

using System.IO;

#if NET5_0_OR_GREATER
using Grpc.Net.Client;
using Grpc.Core;

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
#else
using Grpc.Core;
#endif

using Microsoft.Extensions.Logging;
using System.Net.Http;




namespace ArmoniK.DevelopmentKit.Common.Submitter
{
  /// <summary>
  /// ClientServiceConnector is the class to connection to the control plane with different
  /// like address,port, insecure connection, TLS, and mTLS
  /// </summary>
  public class ClientServiceConnector
  {
    /// <summary>
    /// Open connection with the control plane with or without SSL and no mTLS
    /// </summary>
    /// <param name="endPoint">The address and port of control plane</param>
    /// <param name="sslValidation">Optional : Check if the ssl must have a strong validation (default true)</param>
    /// <param name="loggerFactory">Optional : the logger factory to create the logger</param>
    /// <returns></returns>
    public static Api.gRPC.V1.Submitter.Submitter.SubmitterClient ControlPlaneConnection(string         endPoint,
                                                                                         bool           sslValidation = true,
                                                                                         ILoggerFactory loggerFactory = null)
    {
      return ControlPlaneConnection(endPoint,
                                    "",
                                    "",
                                    sslValidation,
                                    loggerFactory);
    }

    /// <summary>
    /// Open Connection with the control plane with mTLS authentication
    /// </summary>
    /// <param name="endPoint"></param>
    /// <param name="clientCertFilename">The certificate filename in a pem format</param>
    /// <param name="clientKeyFilename">The client key filename in a pem format</param>
    /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
    /// <param name="loggerFactory">Optional logger factory</param>
    /// <returns></returns>
    public static Api.gRPC.V1.Submitter.Submitter.SubmitterClient ControlPlaneConnection(string         endPoint,
                                                                                         string         clientCertFilename,
                                                                                         string         clientKeyFilename,
                                                                                         bool           sslValidation = true,
                                                                                         ILoggerFactory loggerFactory = null)
    {
      var logger = loggerFactory!.CreateLogger<ClientServiceConnector>();
      if ((!string.IsNullOrEmpty(clientCertFilename) && string.IsNullOrEmpty(clientKeyFilename)) ||
          (string.IsNullOrEmpty(clientCertFilename) && !string.IsNullOrEmpty(clientKeyFilename)))
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
          logger!.LogError("Fail to read certificate file",
                           e);
          throw;
        }
      }

      var uri = new Uri(endPoint);
      logger.LogInformation($"Connecting to armoniK  : {uri} port : {uri.Port}");
      logger.LogInformation($"HTTPS Activated: {uri.Scheme == Uri.UriSchemeHttps}");

      if (!string.IsNullOrEmpty(clientCertFilename))
        logger.LogInformation($"mTLS Activated: properties_.ClientCertFilePem");


      return ControlPlaneConnection(endPoint,
                                    clientPem,
                                    sslValidation,
                                    loggerFactory);
    }

    /// <summary>
    /// Open Connection with the control plane with mTLS authentication
    /// </summary>
    /// <param name="endPoint"></param>
    /// <param name="clientPem">The pair certificate + key data in a pem format</param>
    /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
    /// <param name="loggerFactory">Optional logger factory</param>
    /// <returns></returns>
    public static Api.gRPC.V1.Submitter.Submitter.SubmitterClient ControlPlaneConnection(string                endPoint,
                                                                                         Tuple<string, string> clientPem     = null,
                                                                                         bool                  sslValidation = true,
                                                                                         ILoggerFactory        loggerFactory = null)
    {
      var logger = loggerFactory!.CreateLogger<ClientServiceConnector>();
      var uri    = new Uri(endPoint);

      var               credentials       = uri.Scheme == Uri.UriSchemeHttps ? new SslCredentials() : ChannelCredentials.Insecure;
      HttpClientHandler httpClientHandler = new HttpClientHandler();
      if (!sslValidation)
      {
        httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
                             true);
      }

#if NET5_0_OR_GREATER
      if (clientPem != null)
      {
        var cert = X509Certificate2.CreateFromPem(clientPem.Item1,
                                                  clientPem.Item2);

        // Resolve issue with Windows on pem bug with windows
        // https://github.com/dotnet/runtime/issues/23749#issuecomment-388231655

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          var originalCert = cert;
          cert = new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
          originalCert.Dispose();
        }

        httpClientHandler.ClientCertificates.Add(cert);
      }

      var channelOptions = new GrpcChannelOptions()
      {
        Credentials = uri.Scheme == Uri.UriSchemeHttps ? new SslCredentials() : ChannelCredentials.Insecure,
        HttpHandler = httpClientHandler,
        LoggerFactory = loggerFactory,
      };

      var channel = GrpcChannel.ForAddress(endPoint,
                                           channelOptions);

#else
      Environment.SetEnvironmentVariable("GRPC_DNS_RESOLVER",
                                         "native");
      if (clientPem != null)
        credentials = new SslCredentials(clientPem.Item1,
                                         new KeyCertificatePair(clientPem.Item1,
                                                                clientPem.Item2));

      var channel = new Channel($"{uri.Host}:{uri.Port}",
                                credentials);
#endif
      return new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel);
    }
  }
}