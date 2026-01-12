using Grpc.Net.Client;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace paradigm_ehb.CommandCenter.Core.Factories
{
    internal class GrpcChannelFactory : IGrpcChannelFactory
    {
        public GrpcChannel CreateChannel(AgentEndpoint endpoint)
        {
            string scheme = endpoint.UseTls ? "https" : "http";
            string address = $"{scheme}://{endpoint.IpAddress}:{endpoint.Port}";

            // If no custom validation / client certs are required, use default channel creation.
            if (!endpoint.UseTls || endpoint.Metadata == null || (!endpoint.Metadata.ContainsKey("clientCertPath") && !endpoint.Metadata.ContainsKey("trustThumbprint")))
            {
                return GrpcChannel.ForAddress(address);
            }

            var handler = new HttpClientHandler();

            // Trust server by Thumbprint (safer than allowing any cert)
            if (endpoint.Metadata.TryGetValue("trustThumbprint", out string? thumbprint) && !string.IsNullOrWhiteSpace(thumbprint))
            {
                string normalizedThumbprint = thumbprint.Replace(":", "").Replace(" ", "").ToUpperInvariant();

                handler.ServerCertificateCustomValidationCallback = (HttpRequestMessage req, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors) =>
                {
                    if (cert == null) return false;
                    try
                    {
                        string serverCertThumbprint = cert.GetCertHashString().ToUpperInvariant();
                        return string.Equals(serverCertThumbprint, normalizedThumbprint, StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                };
            }

            // Client Certificate for mutual TLS
            if (endpoint.Metadata.TryGetValue("clientCertPath", out string? certPath) && !string.IsNullOrWhiteSpace(certPath))
            {
                try
                {
                    endpoint.Metadata.TryGetValue("clientCertPassword", out string? certPassword);

                    X509Certificate2 clientCert;
                    if (string.IsNullOrEmpty(certPassword))
                    {
                        clientCert = X509CertificateLoader.LoadCertificateFromFile(certPath);
                    }
                    else
                    {
                        clientCert = X509CertificateLoader.LoadCertificateFromFile(certPath);
                    }

                    handler.ClientCertificates.Add(clientCert);
                }
                catch
                {
                    // Ignore certificate load errors to avoid crashing the caller; channel will likely fail to connect.
                }
            }

            return GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = handler });
        }
    }
}
