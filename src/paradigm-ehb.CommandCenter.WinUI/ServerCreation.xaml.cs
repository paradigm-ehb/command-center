using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.Storage.Pickers;

namespace paradigm_ehb.CommandCenter.WinUI;

public sealed partial class ServerCreation : Page
{
    public ServerCreation()
    {
        InitializeComponent();
        LoadFolders();
    }

    /// <summary>
    /// Loads all existing folders into the ComboBox
    /// </summary>
    private void LoadFolders()
    {
        FolderComboBox.Items.Clear();

        string[] folders = fetchFolders();
        foreach (string folder in folders)
        {
            FolderComboBox.Items.Add(folder);
        }

        // Select first folder if available
        if (FolderComboBox.Items.Count > 0)
        {
            FolderComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Handles when user types and submits a new folder name
    /// </summary>
    private void FolderComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
    {
        string newFolderName = args.Text.Trim();

        // Check if folder name is not empty
        if (string.IsNullOrWhiteSpace(newFolderName))
        {
            return;
        }

        // Check if folder already exists in the list
        bool exists = false;
        foreach (var item in sender.Items)
        {
            if (item.ToString().Equals(newFolderName, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                sender.SelectedItem = item;
                break;
            }
        }

        // Create new folder if it doesn't exist
        if (!exists)
        {
            bool created = createFolder(newFolderName);
            if (created)
            {
                sender.Items.Add(newFolderName);
                sender.SelectedItem = newFolderName;
            }
        }
    }

    private async void CertFilePicker_Click(object sender, RoutedEventArgs e)
    {
        // Disable button to prevent multiple clicks
        CertFilePicker.IsEnabled = false;

        FileOpenPicker openPicker = new FileOpenPicker(CertFilePicker.XamlRoot.ContentIslandEnvironment.AppWindowId);

        openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        openPicker.FileTypeFilter.Add(".crt");
        openPicker.FileTypeFilter.Add(".pem");
        openPicker.FileTypeFilter.Add(".der");

        openPicker.CommitButtonText = "Select Certificate File";

        openPicker.ViewMode = PickerViewMode.List;

        PickFileResult certFile = await openPicker.PickSingleFileAsync();
        if (certFile is not null)
        {
            SelectedCertFileTextBlock.Text = certFile.Path;
            RemoveCert.Visibility = Visibility.Visible;
        }

        // Re-enable button
        CertFilePicker.IsEnabled = true;
    }

    /// <summary>
    /// Gets all information & validates it.
    /// </summary>
    public bool ValidateAndProcess()
    {
        // Validate folder selection
        if (FolderComboBox.SelectedItem == null && string.IsNullOrWhiteSpace(FolderComboBox.Text))
        {
            // Show error: folder is required
            return false;
        }

        // Get or create folder name
        string folderName = FolderComboBox.SelectedItem?.ToString() ?? FolderComboBox.Text.Trim();

        // Ensure folder exists
        createFolder(folderName);

        // Parse port from ServerPortTextBox
        int port;
        if (!int.TryParse(ServerPortTextBox.Text, out port))
        {
            return false;
        }

        // Add the server
        addServer(folderName, ServerNameTextBox.Text, ServerIpTextBox.Text, port, ServerUseTLS.IsChecked ?? true, SelectedCertFileTextBlock.Text);

        SendNotification("Server added successfully!");

        return true;
    }

    private void SendNotification(string message, string title = "Command Center")
    {
        var builder = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message);

        var notification = builder.BuildNotification();
        AppNotificationManager.Default.Show(notification);
    }

    /// <summary>
    /// Fetches all folder names from serverStorage
    /// </summary>
    public string[] fetchFolders()
    {
        var localSettings = ApplicationData.Current.LocalSettings;

        // Try to get existing serverStorage container
        ApplicationDataContainer serverStorage;
        if (localSettings.Containers.ContainsKey("serverStorage"))
        {
            serverStorage = localSettings.Containers["serverStorage"];

            // Return all folder (container) names
            return serverStorage.Containers.Keys.ToArray();
        }

        // Return empty array if serverStorage doesn't exist yet
        return Array.Empty<string>();
    }

    /// <summary>
    /// Creates a new folder in serverStorage
    /// </summary>
    /// <param name="folderName">Name of the folder to create</param>
    /// <returns>True if created successfully, false if folder already exists</returns>
    public bool createFolder(string folderName)
    {
        var localSettings = ApplicationData.Current.LocalSettings;

        var serverStorage = localSettings.CreateContainer(
            "serverStorage",
            ApplicationDataCreateDisposition.Always
        );

        // Check if folder already exists
        if (serverStorage.Containers.ContainsKey(folderName))
        {
            return false; // Folder already exists
        }

        // Create the new folder
        serverStorage.CreateContainer(
            folderName,
            ApplicationDataCreateDisposition.Always
        );

        return true;
    }

    public async Task addServer(string folderName, string name, string ip, int port, bool tls, string clientCertPath)
    {
        var localSettings = ApplicationData.Current.LocalSettings;

        var serverStorage = localSettings.CreateContainer(
            "serverStorage",
            ApplicationDataCreateDisposition.Always
        );

        // Create or get the requested the folders
        var folder = serverStorage.CreateContainer(
            folderName,
            ApplicationDataCreateDisposition.Always
        );

        // Determine next available index inside the folder
        int nextIndex = folder.Values.Count;

        // Create the server object
        var server = new ApplicationDataCompositeValue();
        server["name"] = name;
        server["ip"] = ip;
        server["port"] = port;
        server["tls"] = tls;

        // Save it inside the folder
        folder.Values[nextIndex.ToString()] = server;

        // Also register the server in the AgentEndpointRegistry
        try
        {
            // Resolve required services from the global service provider
            IAgentEndpointFactory endpointFactory = App.Services.GetRequiredService<IAgentEndpointFactory>();
            IAgentEndpointRegistry endpointRegistry = App.Services.GetRequiredService<IAgentEndpointRegistry>();

            Dictionary<string, string> metadata = new Dictionary<string, string>
            {
                { "folder", folderName },
            };

            AgentEndpoint endpoint = endpointFactory.Create(
                ipAddress: string.IsNullOrWhiteSpace(ip) ? "localhost" : ip,
                port: port > 0 ? port : 50051,
                useTls: tls,
                displayName: string.IsNullOrWhiteSpace(name) ? null : name,
                CertPath: clientCertPath != "No file selected" ? clientCertPath : null,
                metadata: metadata
            );

            // Register synchronously to ensure subsequent UI refreshes see the new endpoint
            await endpointRegistry.RegisterAsync(endpoint);
        }
        catch
        {
            ILogger<ServerCreation> logger = App.Services.GetRequiredService<ILogger<ServerCreation>>();
            logger.LogError("Failed to register new AgentEndpoint in AgentEndpointRegistry.");
        }
    }

    private void RemoveCert_Click(object sender, RoutedEventArgs e)
    {
        SelectedCertFileTextBlock.Text = "No file selected";
        RemoveCert.Visibility = Visibility.Collapsed;
    }
}
