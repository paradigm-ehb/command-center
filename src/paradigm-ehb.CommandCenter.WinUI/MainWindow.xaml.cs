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

        public void LoadServerMenu()
        {
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
                    SelectsOnInvoked = false
                };

                foreach (var server in folder.Servers)
                {
                    var serverItem = new NavigationViewItem
                    {
                        Content = server.Name,
                        Icon = new SymbolIcon(Symbol.World),
                        SelectsOnInvoked = true,
                        Tag = server
                    };
                    serverItem.Tapped += ServerItem_Tapped;
                    folderItem.MenuItems.Add(serverItem);
                }

                nvSample.MenuItems.Add(folderItem);
            }
        }

        private void ServerItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is NavigationViewItem navItem && navItem.Tag is ServerInfo server)
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
                "EU1" => typeof(SettingsPage),
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
    }
}
