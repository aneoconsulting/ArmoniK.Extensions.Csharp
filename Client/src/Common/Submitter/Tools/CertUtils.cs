// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#else
using System;
using System.IO;
using System.Text;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
#endif
using System.Security.Cryptography.X509Certificates;

using ArmoniK.DevelopmentKit.Common.Exceptions;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter.Tools;

/// <summary>
/// </summary>
public static class CertUtils
{
  /// <summary>
  ///   Generates pfx from client configuration
  /// </summary>
  /// <returns>Generated Pfx Path</returns>
  public static X509Certificate2 GeneratePfx(string clientPemData,
                                             string clientKeyData)
  {
#if NET5_0_OR_GREATER
    string keyData = null;
    string certData = null;

    if (!string.IsNullOrWhiteSpace(clientKeyData))
    {
      keyData = clientKeyData;
    }

    if (keyData == null)
    {
      throw new ClientApiException("clientKeyData is null or empty");
    }

    if (!string.IsNullOrWhiteSpace(clientPemData))
    {
      certData = clientPemData;
    }
    else
    {
      throw new ClientApiException("clientPemData is null or empty");
    }


    var cert = X509Certificate2.CreateFromPem(certData,
                                              keyData);

    // see https://github.com/dotnet/runtime/issues/45680
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      cert = new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
    }

    return cert;
#else
    byte[] certData;

    if (!string.IsNullOrWhiteSpace(clientPemData))
    {
      certData = Encoding.UTF8.GetBytes(clientPemData);
    }
    else
    {
      throw new ClientApiException("clientPemData is null or empty");
    }

    var cert = new X509CertificateParser().ReadCertificate(certData);
    // key usage is a bit string, zero-th bit is 'digitalSignature'
    // See https://www.alvestrand.no/objectid/2.5.29.15.html for more details.
    if (cert?.GetKeyUsage() != null && !cert.GetKeyUsage()[0])
    {
      throw new Exception("Client certificates must be digital signed : See https://www.alvestrand.no/objectid/2.5.29.15.html");
    }

    var keyData = Encoding.UTF8.GetBytes(clientKeyData);

    object obj;
    using (var reader = new StreamReader(new MemoryStream(keyData)))
    {
      obj = new PemReader(reader).ReadObject();
      if (obj is AsymmetricCipherKeyPair key)
      {
        var cipherKey = key;
        obj = cipherKey.Private;
      }
    }

    var keyParams = (AsymmetricKeyParameter)obj;

    var store = new Pkcs12StoreBuilder().Build();
    store.SetKeyEntry("test123",
                      new AsymmetricKeyEntry(keyParams),
                      new[]
                      {
                        new X509CertificateEntry(cert),
                      });

    using var pkcs = new MemoryStream();

    store.Save(pkcs,
               Array.Empty<char>(),
               new SecureRandom());

    return new X509Certificate2(pkcs.ToArray(),
                                string.Empty,
                                (X509KeyStorageFlags)36);
#endif
  }

  /// <summary>
  ///   Retrieves Client Certificate PFX from configuration
  /// </summary>
  /// <returns>Client certificate PFX</returns>
  public static X509Certificate2 GetClientCertFromPem(string clientPemData,
                                                      string clientKeyData)
    => GeneratePfx(clientPemData,
                   clientKeyData);
}
