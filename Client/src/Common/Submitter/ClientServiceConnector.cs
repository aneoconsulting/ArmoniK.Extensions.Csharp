using System;

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
                              sslValidation,
                              loggerFactory);

  /// <summary>
  ///   Open Connection with the control plane with mTLS authentication
  /// </summary>
  /// <param name="endPoint">The address and port of control plane</param>
  /// <param name="clientPem">The pair certificate + key data in a pem format</param>
  /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns></returns>
  private static ChannelBase ControlPlaneConnection(string                            endPoint,
                                                    [CanBeNull] Tuple<string, string> clientCertKeyPemPair = null,
                                                    [CanBeNull] string                clientP12            = null,
                                                    bool                              sslValidation        = true,
                                                    [CanBeNull] ILoggerFactory        loggerFactory        = null)
  {
#if !NET5_0_OR_GREATER
    Environment.SetEnvironmentVariable("GRPC_DNS_RESOLVER",
                                       "native");
#endif
    return GrpcChannelFactory.CreateChannel(new GrpcClient
                                            {
                                              AllowUnsafeConnection = !sslValidation,
                                              CertP12               = clientP12                   ?? "",
                                              CertPem               = clientCertKeyPemPair?.Item1 ?? "",
                                              KeyPem                = clientCertKeyPemPair?.Item2 ?? "",
                                              Endpoint              = endPoint,
                                            });
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
  /// <param name="sslValidation">Check if the ssl must have a strong validation</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns>The connection pool</returns>
  public static ChannelPool ControlPlaneConnectionPool(string                            endPoint,
                                                       [CanBeNull] Tuple<string, string> clientPem     = null,
                                                       [CanBeNull] string                clientP12     = null,
                                                       bool                              sslValidation = true,
                                                       [CanBeNull] ILoggerFactory        loggerFactory = null)
    => new(() => ControlPlaneConnection(endPoint,
                                        clientPem,
                                        clientP12,
                                        sslValidation,
                                        loggerFactory),
           loggerFactory);
}
