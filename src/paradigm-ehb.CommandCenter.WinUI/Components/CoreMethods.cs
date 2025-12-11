using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;

namespace paradigm_ehb.CommandCenter.WinUI.Components
{
    internal class CoreMethods
    {
        public static List<ServerFolder> getAllServers()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var folderList = new List<ServerFolder>();

            if (localSettings.Containers.ContainsKey("serverStorage"))
            {
                var serverStorage = localSettings.Containers["serverStorage"];
                
                // Gets all the folders
                foreach (var folderName in serverStorage.Containers.Keys)
                {
                    var folder = serverStorage.Containers[folderName];
                    var serverFolder = new ServerFolder
                    {
                        FolderName = folderName,
                        Servers = new List<ServerInfo>()
                    };
                    
                    // Gets all the servers in a given folder
                    foreach (var serverKey in folder.Values.Keys)
                    {
                        var server = (ApplicationDataCompositeValue)folder.Values[serverKey];
                        
                        serverFolder.Servers.Add(new ServerInfo
                        {
                            Name = server["name"]?.ToString() ?? "",
                            Ip = server["ip"]?.ToString() ?? "",
                            Port = server["port"] != null ? (int)server["port"] : 0
                        });
                    }
                    
                    folderList.Add(serverFolder);
                }
            }

            return folderList;
        }
    }

    public class ServerFolder
    {
        public string FolderName { get; set; }
        public List<ServerInfo> Servers { get; set; }
    }

    public class ServerInfo
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
    }
}
