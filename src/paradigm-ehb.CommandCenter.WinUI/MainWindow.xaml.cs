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

namespace paradigm_ehb.CommandCenter.WinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var appWindow = this.AppWindow;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            this.SetTitleBar(SimpleTitleBar);
            contentFrame.Navigate(typeof(HomePage));
        }

        public async void Button_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Hello from WinUI!",
                Content = Client.DependencyMethod(),
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
        }

        private void windowSizeChanged(object sender, WindowSizeChangedEventArgs args)
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
    }
}
