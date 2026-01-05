using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.WinUI;
using System.Threading.Tasks;

namespace paradigm_ehb.CommandCenter.WinUI.Components
{
    internal interface ICoreMethods
    {
        Task LoadRegistryAsync();
        Task<List<ServerFolder>> GetAllServersAsync();
    }

    internal class CoreMethods : ICoreMethods
    {
        private readonly IAgentEndpointFactory _endpointFactory;
        private readonly IAgentEndpointRegistry _agentEndpointRegistry;
        private readonly IAgentMonitor _agentMonitor;

        public CoreMethods(IAgentEndpointFactory endpointFactory, IAgentEndpointRegistry agentEndpointRegistry, IAgentMonitor agentMonitor)
        {
            _endpointFactory = endpointFactory ?? throw new ArgumentNullException(nameof(endpointFactory));
            _agentEndpointRegistry = agentEndpointRegistry ?? throw new ArgumentNullException(nameof(agentEndpointRegistry));
            _agentMonitor = agentMonitor ?? throw new ArgumentNullException(nameof(agentMonitor));
        }

        public async Task LoadRegistryAsync()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            List<ServerFolder> folderList = new List<ServerFolder>();

            if (localSettings.Containers.ContainsKey("serverStorage"))
            {
                ApplicationDataContainer serverStorage = localSettings.Containers["serverStorage"];

                // Gets all the folders
                foreach (string? folderName in serverStorage.Containers.Keys)
                {
                    ApplicationDataContainer folder = serverStorage.Containers[folderName];

                    // Gets all the servers in a given folder
                    foreach (string? serverKey in folder.Values.Keys)
                    {
                        ApplicationDataCompositeValue server = (ApplicationDataCompositeValue)folder.Values[serverKey];

                        string ip = server["ip"]?.ToString() ?? "localhost";
                        string name = server["name"]?.ToString();
                        int port = 0;

                        if (server["port"] != null)
                        {
                            // ApplicationDataCompositeValue stores boxed values - be defensive
                            try
                            {
                                port = Convert.ToInt32(server["port"]);
                            }
                            catch
                            {
                                port = 0;
                            }
                        }

                        int portToUse = port > 0 ? port : 50051;

                        bool? tls = (bool)server["tls"];

                        Dictionary<string, string> metadata = new Dictionary<string, string>
                        {
                            { "folder", folderName   }
                        };

                        // Use the injected factory to create a properly-initialized AgentEndpoint
                        AgentEndpoint endpoint = _endpointFactory.Create(ipAddress: ip, port: portToUse, useTls: tls ?? true, displayName: string.IsNullOrWhiteSpace(name) ? null : name, metadata: metadata);    // TODO implement correct TLS selection in UI and parse

                        // Register the endpoint in the registry
                        AgentEndpointRegistrationResult result = await _agentEndpointRegistry.RegisterAsync(endpoint);
                    }
                }
            }

            // Start monitoring the registered agents
            _agentMonitor.StartAsync(_agentEndpointRegistry, new TimeSpan(0, 0, 10)); // Fire-and-forget
        }

        // Instance implementation using injected factory
        public async Task<List<ServerFolder>> GetAllServersAsync()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            List<ServerFolder> folderList = new List<ServerFolder>();

            if (localSettings.Containers.ContainsKey("serverStorage"))
            {
                ApplicationDataContainer serverStorage = localSettings.Containers["serverStorage"];

                // Gets all the folders
                foreach (string? folderName in serverStorage.Containers.Keys)
                {
                    ApplicationDataContainer folder = serverStorage.Containers[folderName];
                    ServerFolder serverFolder = new ServerFolder
                    {
                        FolderName = folderName,
                        Servers = new List<AgentEndpoint>()
                    };

                    IReadOnlyCollection<AgentEndpoint> endpoints = await _agentEndpointRegistry.ListAsync();

                    // Each endpoint with matching folder metadata is added to the folder's server list
                    foreach (AgentEndpoint endpoint in endpoints)
                    {
                        if (endpoint.Metadata != null &&
                            endpoint.Metadata.ContainsKey("folder") &&
                            endpoint.Metadata["folder"] == folderName)
                        {
                            serverFolder.Servers.Add(endpoint);
                        }
                    }

                    folderList.Add(serverFolder);   // Add the folder to the list
                }
            }

            return folderList;
        }

        // Backwards-compatible static wrapper that resolves the service from the global App service provider.
        public static List<ServerFolder> getAllServers()
        {
            ICoreMethods coreMethods = App.Services.GetRequiredService<ICoreMethods>();
            return coreMethods.GetAllServersAsync().Result;
        }
    }

    public class ServerFolder
    {
        public string FolderName { get; set; }
        public List<AgentEndpoint> Servers { get; set; }
    }
}
