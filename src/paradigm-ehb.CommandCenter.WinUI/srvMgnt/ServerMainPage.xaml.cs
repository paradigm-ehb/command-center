using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.WinUI.Components;
using paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views;
using System;

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt
{
    public sealed partial class ServerMainPage : Page
    {
        public ServerMainPage()
        {
            InitializeComponent();
            
            SelectorBar.SelectedItem = SelectorBar.Items[0];
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is AgentEndpoint ip)
            {
                serverObj = ip;
                serverName.Text = serverObj.DisplayName;
                serverIP.Text = serverObj.IpAddress + ":" + serverObj.Port.ToString();
                
                ContentFrame.Navigate(typeof(srvOverview), serverObj);
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
            typeof(ServerMainPage),
            new PropertyMetadata(null));

        private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
            System.Type pageType;

            switch (currentSelectedIndex)
            {
                case 0:
                    pageType = typeof(srvOverview);
                    break;
                case 1:
                    pageType = typeof(srvServices);
                    break;
                case 2:
                    pageType = typeof(srvProcesses);
                    break;
                default:
                    pageType = typeof(srvOverview);
                    break;
            }
            
            ContentFrame.Navigate(pageType, serverObj);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog confirmationDialog = new ContentDialog()
            {
                Title = "Are you sure you want to delete this server?",
                Content = "This server will be removed from the app and cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };

            confirmationDialog.PrimaryButtonClick += delegate
            {
                var content = CoreMethods.deleteServer(serverObj.FolderName, serverObj.DisplayName);
            };

            confirmationDialog.XamlRoot = this.Content.XamlRoot;

            await confirmationDialog.ShowAsync();
        }
    }
}
