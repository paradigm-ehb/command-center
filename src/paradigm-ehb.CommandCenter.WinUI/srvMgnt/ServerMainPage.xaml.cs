using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.WinUI.Components;
using paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views;
using System;
using System.Threading.Tasks;

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt
{
    public sealed partial class ServerMainPage : Page
    {
        private readonly IAgentClientFactory _agentClientFactory;
        private readonly IAgentClientRegistry _agentClientRegistry;

        public ServerMainPage()
        {
            // Dependency Injection
            _agentClientFactory = App.Services.GetRequiredService<IAgentClientFactory>();
            _agentClientRegistry = App.Services.GetRequiredService<IAgentClientRegistry>();

            InitializeComponent();
            
            SelectorBar.SelectedItem = SelectorBar.Items[0];
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Fire-and-forget the async initialization. Exceptions are observed inside the task.
            _ = InitializeForNavigationAsync(e);
        }

        private async Task InitializeForNavigationAsync(NavigationEventArgs e)
        {
            try
            {
                if (e.Parameter is AgentEndpoint ip)
                {
                    // Ensure the agent client is created and registered (use await instead of blocking)
                    bool registered = await _agentClientRegistry.IsRegisteredAsync(ip.Id).ConfigureAwait(false);
                    if (!registered)
                    {
                        await _agentClientFactory.CreateAndRegisterClientAsync(ip).ConfigureAwait(false);
                    }

                    // UI updates must run on the UI thread — marshal back if needed.
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        serverObj = ip;
                        serverName.Text = ip.DisplayName;
                        serverIP.Text = ip.IpAddress + ":" + ip.Port.ToString();
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // Log the exception using the logging framework
                ILogger logger = App.Services.GetRequiredService<ILogger<ServerMainPage>>();
                logger.LogError(ex, "Error during ServerMainPage initialization.");
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

    internal static class DispatcherQueueExtensions
    {
        // Small helper to marshal an action to the UI thread as a Task.
        public static Task EnqueueAsync(this Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue, Action action)
        {
            var tcs = new TaskCompletionSource<object?>();
            bool posted = dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            if (!posted)
            {
                tcs.SetException(new InvalidOperationException("Failed to post to DispatcherQueue."));
            }

            return tcs.Task;
        }
    }
}
