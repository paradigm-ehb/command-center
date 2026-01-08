using Microsoft.UI;
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
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using paradigm_ehb.CommandCenter.WinUI.Components;
using System.Diagnostics;
using paradigm_ehb.CommandCenter.WinUI.srvMgnt;
using System.ComponentModel;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.WinUI
{
    public sealed partial class MainWindow : Window
    {
        public static MainWindow Instance;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            var appWindow = this.AppWindow;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            this.SetTitleBar(SimpleTitleBar);
            LoadServerMenu();
        }

        public void NavigateToServerPage(Type pageType, object parameter)
        {
            contentFrame.Navigate(pageType, parameter);

            if (parameter is AgentEndpoint server)
            {
                var (serverItem, folderItem) = FindNavigationViewItemByServer(server);
                if (serverItem != null)
                {
                    if (folderItem != null)
                    {
                        folderItem.IsExpanded = true;
                    }
                    
                    nvSample.SelectedItem = serverItem;
                }
            }
        }

        private (NavigationViewItem serverItem, NavigationViewItem folderItem) FindNavigationViewItemByServer(AgentEndpoint server)
        {
            foreach (var item in nvSample.MenuItems.Skip(3)) //Skipt de eerste 3 items ("Home", "Settings" en "Your Servers")
            {
                if (item is NavigationViewItem folderItem)
                {
                    foreach (var child in folderItem.MenuItems)
                    {
                        if (child is NavigationViewItem serverItem && serverItem.Tag is AgentEndpoint existingServer)
                        {
                            if (existingServer.DisplayName == server.DisplayName && existingServer.IpAddress == server.IpAddress)
                            {
                                return (serverItem, folderItem);
                            }
                        }
                    }
                }
            }
            return (null, null);
        }

        public void LoadServerMenu()
        {
            // Store the expanded state of folders before clearing
            var expandedFolders = new HashSet<string>();
            foreach (var item in nvSample.MenuItems.Skip(3))
            {
                if (item is NavigationViewItem folderItem && folderItem.IsExpanded)
                {
                    expandedFolders.Add(folderItem.Content?.ToString() ?? "");
                }
            }

            // Store the currently selected server to restore selection
            AgentEndpoint selectedServer = null;
            if (nvSample.SelectedItem is NavigationViewItem selectedItem && selectedItem.Tag is AgentEndpoint server)
            {
                selectedServer = server;
            }

            // Clear existing server items (keep Home and Settings, remove everything after)
            while (nvSample.MenuItems.Count > 2)
            {
                nvSample.MenuItems.RemoveAt(2);
            }

            // Add "Your Servers" header
            var serverHeader = new NavigationViewItemHeader
            {
                Content = "Your Servers"
            };
            nvSample.MenuItems.Add(serverHeader);

            // Load servers from storage
            var serverFolders = CoreMethods.getAllServers();

            foreach (var folder in serverFolders)
            {
                var folderItem = new NavigationViewItem
                {
                    Content = folder.FolderName,
                    Icon = new SymbolIcon(Symbol.Folder),
                    SelectsOnInvoked = false,
                    IsExpanded = expandedFolders.Contains(folder.FolderName) // Restore expanded state
                };

                foreach (var agent in folder.Servers)
                {
                    var serverItem = new NavigationViewItem
                    {
                        Content = agent.DisplayName,
                        Icon = new SymbolIcon(Symbol.World),
                        SelectsOnInvoked = true,
                        Tag = agent
                    };
                    serverItem.Tapped += ServerItem_Tapped;
                    folderItem.MenuItems.Add(serverItem);

                    // Restore selection if this was the selected server
                    if (selectedServer != null && 
                        agent.DisplayName == selectedServer.DisplayName && 
                        agent.IpAddress == selectedServer.IpAddress)
                    {
                        nvSample.SelectedItem = serverItem;
                    }
                }

                nvSample.MenuItems.Add(folderItem);
            }
        }

        private void ServerItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is NavigationViewItem navItem && navItem.Tag is AgentEndpoint server)
            {
                contentFrame.Navigate(typeof(ServerMainPage), server);
                nvSample.SelectedItem = navItem;
            }
        }
        
        private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs args)
        {

        }

        private void NvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            // resolve the container and/or invoked item and use Tag to pick a page type
            if (args.InvokedItemContainer is NavigationViewItem invoked)
            {
                // if the item is a parent with children, toggle expansion
                if (invoked.MenuItems?.Count > 0)
                {
                    invoked.IsExpanded = !invoked.IsExpanded;
                    return;
                }

                // leaf item -> navigate by Tag
                var tag = invoked.Tag as string;
                var pageType = TypeForTag(tag);
                if (pageType != null)
                {
                    contentFrame.Navigate(pageType);
                    sender.SelectedItem = invoked;
                }
            }
        }

        private Type? TypeForTag(string? tag)
        {
            return tag switch
            {
                "HomePage" => typeof(HomePage),
                "SettingsPage" => typeof(SettingsPage),
                _ => null
            };
        }

        private void nvSample_Loaded(object sender, RoutedEventArgs e)
        {
            var homeItem = nvSample.MenuItems
                      .OfType<NavigationViewItem>()
                      .First(item => item.Tag.ToString() == "HomePage");

            nvSample.SelectedItem = homeItem;

            contentFrame.Navigate(typeof(HomePage));

        }
        
        private async void AddServerButton_Clicked(object sender, RoutedEventArgs args)
        {
            var serverCreation = new ServerCreation();

            ContentDialog serverCreationDialog = new ContentDialog
            {
                Title = "Add a new server",
                Content = serverCreation,
                CloseButtonText = "OK",
                PrimaryButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
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
                        HomePage.Instance.LoadServers();
                        LoadServerMenu();
                    }
                }
            };

            ContentDialogResult result = await serverCreationDialog.ShowAsync();
        }
    }
}
