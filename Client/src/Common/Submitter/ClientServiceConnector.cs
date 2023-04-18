using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using ArmoniK.Api.Client.Options;
using ArmoniK.Api.Client.Submitter;

using Grpc.Core;

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
  ///   Open connection with the control plane with or without SSL and no mTLS
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="sslValidation">Optional : Check if the ssl must have a strong validation (default true)</param>
  /// <param name="loggerFactory">Optional : the logger factory to create the logger</param>
  /// <returns></returns>
  private static ChannelBase ControlPlaneConnection(string                     endPoint,
                                                    bool                       sslValidation = true,
                                                    [CanBeNull] ILoggerFactory loggerFactory = null)
    => ControlPlaneConnection(endPoint,
                              null,
                              null,
                              null,
                              false,
                              sslValidation,
                              loggerFactory);

  /// <summary>
  ///   Open Connection with the control plane with mTLS authentication
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="clientCertKeyPemPair">The pair certificate + key data in a pem format</param>
  /// <param name="clientP12">The certificate and key in P12/Pkcs12/PFX format</param>
  /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
  /// <param name="mTLS">Activate mTLS support</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns></returns>
  private static ChannelBase ControlPlaneConnection(string                            endPoint,
                                                    string                            capem                = null,
                                                    [CanBeNull] Tuple<string, string> clientCertKeyPemPair = null,
                                                    [CanBeNull] string                clientP12            = null,
                                                    bool                              sslValidation        = true,
                                                    bool                              mTLS                 = false,
                                                    [CanBeNull] ILoggerFactory        loggerFactory        = null)
  {
    var logger = loggerFactory?.CreateLogger<ClientServiceConnector>();
    var options = new GrpcClient
                  {
                    AllowUnsafeConnection = !sslValidation,
                    CertP12               = clientP12                   ?? "",
                    CertPem               = clientCertKeyPemPair?.Item1 ?? "",
                    KeyPem                = clientCertKeyPemPair?.Item2 ?? "",
                    Endpoint              = endPoint,
                    mTLS                  = mTLS,
                  };
#if NET5_0_OR_GREATER
    return GrpcChannelFactory.CreateChannel(options);
#else

    Environment.SetEnvironmentVariable("GRPC_DNS_RESOLVER",
                                       "native");
    var uri         = new Uri(endPoint);
    var credentials = ChannelCredentials.Insecure;

    var channelOptions = new List<ChannelOption>();
    if (uri.Scheme == Uri.UriSchemeHttps)
    {
      var ca = capem != null
                 ? File.ReadAllText(capem)
                 : null;
      string certname;
      if (mTLS)
      {
        var certKeyPair = GrpcChannelFactory.GetKeyCertificatePair(options);
        certname = new X509Certificate2(Encoding.ASCII.GetBytes(certKeyPair.CertificateChain)).GetNameInfo(X509NameType.SimpleName,
                                                                                                           false);
        credentials = new SslCredentials(ca ?? certKeyPair.CertificateChain,
                                         certKeyPair,
                                         !sslValidation
                                           ? _ => true
                                           : null);
      }
      else
      {
        credentials = new SslCredentials(ca,
                                         null,
                                         !sslValidation
                                           ? _ => true
                                           : null);
      }

      if (!sslValidation)
      {
        ServicePointManager.ServerCertificateValidationCallback = (_,
                                                                   _,
                                                                   _,
                                                                   _) => true;
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
                             true);

        //Retrieve the server certificate to override the target name (Grpc.Core doesn't allow overriding the certificate validation during handshake)
        var request = (HttpWebRequest)WebRequest.Create(uri);
        if (mTLS)
        {
          request.ClientCertificates.Add(GrpcChannelFactory.GetCertificate(options));
        }

        var response = (HttpWebResponse)request.GetResponse();
        response.Close();

        var serverCert = request.ServicePoint.Certificate;

        if (serverCert != null)
        {
          var cert2 = new X509Certificate2(serverCert);

          var cn = cert2.GetNameInfo(X509NameType.SimpleName,
                                     false);

          channelOptions.Add(new ChannelOption("grpc.ssl_target_name_override",
                                               cn));
        }
      }
    }

    logger?.LogInformation($"Connecting to armoniK : {uri} port : {uri.Port}");
    logger?.LogInformation($"HTTPS Activated: {uri.Scheme == Uri.UriSchemeHttps}");
    var channel = new Channel(uri.Host,
                              uri.Port,
                              credentials,
                              channelOptions);
    return channel;
#endif
  }

  /// <summary>
  ///   Create a connection pool to the control plane with or without SSL and no mTLS
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="sslValidation">Optional : Check if the ssl must have a strong validation (default true)</param>
  /// <param name="loggerFactory">Optional : the logger factory to create the logger</param>
  /// <returns></returns>
  public static ChannelPool ControlPlaneConnectionPool(string                     endPoint,
                                                       bool                       sslValidation = true,
                                                       [CanBeNull] ILoggerFactory loggerFactory = null)
    => new(() => ControlPlaneConnection(endPoint,
                                        sslValidation,
                                        loggerFactory),
           loggerFactory);


  /// <summary>
  ///   Create a connection pool to the control plane with mTLS authentication
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="clientPem">The pair certificate + key data in a pem format</param>
  /// <param name="clientP12">The certificate and key in P12/Pkcs12/PFX format</param>
  /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns>The connection pool</returns>
  public static ChannelPool ControlPlaneConnectionPool(string                            endPoint,
                                                       [CanBeNull] string                caPem         = null,
                                                       [CanBeNull] Tuple<string, string> clientPem     = null,
                                                       [CanBeNull] string                clientP12     = null,
                                                       bool                              sslValidation = true,
                                                       bool                              mTLS          = false,
                                                       [CanBeNull] ILoggerFactory        loggerFactory = null)

  {
    if (caPem != null || clientPem != null)
    {
      var store = new X509Store(StoreName.My,
                                StoreLocation.CurrentUser);
      store.Open(OpenFlags.ReadWrite);
      if (caPem != null)
      {
        store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(caPem)));
      }

      if (mTLS && clientPem != null)
      {
        var options = new GrpcClient
                      {
                        AllowUnsafeConnection = true,
                        CertP12               = clientP12       ?? "",
                        CertPem               = clientPem.Item1 ?? "",
                        KeyPem                = clientPem.Item2 ?? "",
                        Endpoint              = endPoint,
                        mTLS                  = true,
                      };
        store.Add(GrpcChannelFactory.GetCertificate(options));
      }

      store.Close();
    }

    return new ChannelPool(() => ControlPlaneConnection(endPoint,
                                                        caPem,
                                                        clientPem,
                                                        clientP12,
                                                        sslValidation,
                                                        mTLS,
                                                        loggerFactory),
                           loggerFactory);
  }
}
