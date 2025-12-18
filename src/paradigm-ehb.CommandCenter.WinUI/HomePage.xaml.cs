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
using System.ComponentModel;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.WinUI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    public List<ServerFolder> ServerFolders { get; set; }
    public static HomePage Instance;
    public HomePage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        Instance = this;
        LoadServers();
    }

    public void LoadServers()
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

    private void InsertServersInView(List<AgentEndpoint> servers)
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
                ServerObject = server
            };

            newStack.Children.Add(serverView);
        }
    }
}
