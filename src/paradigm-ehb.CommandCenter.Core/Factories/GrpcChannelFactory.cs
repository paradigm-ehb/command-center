using Grpc.Net.Client;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security;

namespace paradigm_ehb.CommandCenter.Core.Factories
{
    internal class GrpcChannelFactory : IGrpcChannelFactory
    {
        public GrpcChannel CreateChannel(AgentEndpoint endpoint, SecureString? certPassword)
        {
            string scheme = endpoint.UseTls ? "https" : "http";
            string address = $"{scheme}://{endpoint.IpAddress}:{endpoint.Port}";

            // If no custom validation / client certs are required, use default channel creation.
            if (!endpoint.UseTls || endpoint.Metadata == null || (endpoint.CertPath is null && !endpoint.Metadata.ContainsKey("trustThumbprint")))
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
            if (!string.IsNullOrWhiteSpace(endpoint.CertPath))
            {
                try
                {
                    X509Certificate2 clientCert = X509CertificateLoader.LoadCertificateFromFile(endpoint.CertPath);

                    handler.ClientCertificates.Add(clientCert);
                }
                catch
                {
                    // Ignore certificate load errors to avoid crashing the caller; channel will likely fail to connect.
                }
            }

            return GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = handler });
        }

        public async Task<string?> GetServerThumbprintAsync(string ipAddress, int port)
        {
            string? capturedThumbprint = null;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (HttpRequestMessage req, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors) =>
                {
                    if (cert != null)
                    {
                        capturedThumbprint = cert.GetCertHashString().ToUpperInvariant();
                    }
                    // Accept any certificate to capture the thumbprint
                    return true;
                }
            };

            using var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            try
            {
                // Make a simple request to trigger the TLS handshake
                await httpClient.GetAsync($"https://{ipAddress}:{port}");
            }
            catch
            {
                // Connection might fail, but we may have captured the cert during TLS handshake
            }

            return capturedThumbprint;
        }
    }
}
