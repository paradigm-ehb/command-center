using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.WinUI;

namespace paradigm_ehb.CommandCenter.WinUI.Components
{
    internal interface ICoreMethods
    {
        List<ServerFolder> GetAllServers();
    }

    internal class CoreMethods : ICoreMethods
    {
        private readonly IAgentEndpointFactory _endpointFactory;

        public CoreMethods(IAgentEndpointFactory endpointFactory)
        {
            _endpointFactory = endpointFactory ?? throw new ArgumentNullException(nameof(endpointFactory));
        }

        // Instance implementation using injected factory
        public List<ServerFolder> GetAllServers()
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

                        // Use the injected factory to create a properly-initialized AgentEndpoint
                        AgentEndpoint endpoint = _endpointFactory.Create(ipAddress: ip, port: portToUse, useTls: true, displayName: string.IsNullOrWhiteSpace(name) ? null : name);

                        serverFolder.Servers.Add(endpoint);
                    }

                    folderList.Add(serverFolder);
                }
            }

            return folderList;
        }

        // Backwards-compatible static wrapper that resolves the service from the global App service provider.
        public static List<ServerFolder> getAllServers()
        {
            var coreMethods = App.Services.GetRequiredService<ICoreMethods>();
            return coreMethods.GetAllServers();
        }
    }

    public class ServerFolder
    {
        public string FolderName { get; set; }
        public List<AgentEndpoint> Servers { get; set; }
    }
}
