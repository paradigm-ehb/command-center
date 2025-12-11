using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using paradigm_ehb.CommandCenter.WinUI.Components;
using paradigm_ehb.CommandCenter.WinUI.Components.Reusable;

namespace paradigm_ehb.CommandCenter.WinUI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    public List<ServerFolder> ServerFolders { get; set; }

    public HomePage()
    {
        InitializeComponent();
        LoadServers();
    }

    private void LoadServers()
    {
        ServerFolders = CoreMethods.getAllServers();
        BuildHomescreen();
    }

    private void BuildHomescreen()
    {
        AllServers.Children.Clear();
        foreach (var folder in ServerFolders)
        {
            var Title = new TextBlock
            {
                Text = folder.FolderName,
                FontSize = 32,
                FontWeight = Microsoft.UI.Text.FontWeights.Thin,
                Margin = new Thickness(0, 15, 0, 5)
            };

            AllServers.Children.Add(Title);

            InsertServersInView(folder.Servers);
        }
    }

    private void InsertServersInView(List<ServerInfo> servers)
    {
        var newStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 0),
            Spacing = 10
        };

        AllServers.Children.Add(newStack);


        foreach (var server in servers)
        {
            var serverView = new Home_ServerOverview
            {
                ServerName = server.Name,
                ServerStatus = 0
            };

            newStack.Children.Add(serverView);
        }
    }

    private async void CtrlN_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;

        var serverCreation = new ServerCreation();

        ContentDialog serverCreationDialog = new ContentDialog
        {
            Title = "Add a new server",
            Content = serverCreation,
            CloseButtonText = "OK",
            PrimaryButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        serverCreationDialog.Closing += (dialog, closingArgs) => {
            if (closingArgs.Result == ContentDialogResult.None)
            {
                bool isValid = serverCreation.ValidateAndProcess();

                // Cancel the close if validation fails
                if (!isValid)
                {
                    closingArgs.Cancel = true;
                }
                else
                {
                    // Reload servers after adding a new one
                    LoadServers();
                }
            }
        };

        ContentDialogResult result = await serverCreationDialog.ShowAsync();
    }

}
