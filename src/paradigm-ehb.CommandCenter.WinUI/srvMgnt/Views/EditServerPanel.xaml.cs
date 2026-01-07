using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.WinUI.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditServerPanel : Page
    {
        public EditServerPanel()
        {
            InitializeComponent();
        }

        // Validates inputs and applies the changes - returns true if successful
        public async Task<bool> ValidateAndApplyChangesAsync()
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(ServerNameTextBox.Text))
            {
                return false; // Invalid name
            }

            if (string.IsNullOrWhiteSpace(ServerIpTextBox.Text))
            {
                return false; // Invalid IP
            }

            if (!int.TryParse(ServerPortTextBox.Text, out int port) || port <= 0 || port > 65535)
            {
                return false; // Invalid port
            }

            // Apply the changes
            var result = await CoreMethods.modifyServer(
                folderName: serverObj.FolderName,
                name: serverObj.DisplayName,  // Original name to find the server
                ip: ServerIpTextBox.Text,
                port: Int32.Parse(ServerPortTextBox.Text),
                newName: ServerNameTextBox.Text  // New name
            );

            IAgentEndpointRegistry registry = App.Services.GetRequiredService<IAgentEndpointRegistry>();
            var endpoint = await registry.GetAsync(serverObj.Id);
            endpoint.DisplayName = ServerNameTextBox.Text;
            endpoint.IpAddress = ServerIpTextBox.Text;
            endpoint.Port = Int32.Parse(ServerPortTextBox.Text);


            return result.success;
        }

        public EditServerPanel(AgentEndpoint server) : this()
        {
            serverObj = server;

            ServerNameTextBox.Text = server.DisplayName;
            ServerIpTextBox.Text = server.IpAddress;
            ServerPortTextBox.Text = server.Port.ToString();
            ServerUseTLS.IsChecked = server.UseTls;

            if (!string.IsNullOrEmpty(server.FolderName))
            {
                FolderComboBox.Text = server.FolderName;
            }
        }

        public AgentEndpoint serverObj
        {
            get => (AgentEndpoint)GetValue(serverIP_Property);
            set => SetValue(serverIP_Property, value);
        }

        public static readonly DependencyProperty serverIP_Property =
        DependencyProperty.Register(
            nameof(serverObj),
            typeof(AgentEndpoint),
            typeof(EditServerPanel),
            new PropertyMetadata(null));
    }
}
