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
                default:
                    pageType = typeof(srvOverview);
                    break;
            }
            
            ContentFrame.Navigate(pageType, serverObj);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            var serverCreation = new EditServerPanel(serverObj);

            ContentDialog serverCreationDialog = new ContentDialog
            {
                Title = "Edit this server",
                Content = serverCreation,
                CloseButtonText = "OK",
                PrimaryButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            serverCreationDialog.Closing += async (dialog, closingArgs) => {
                if (closingArgs.Result == ContentDialogResult.None)
                {
                    bool isValid = await serverCreation.ValidateAndApplyChangesAsync();

                    // Cancel the close if validation fails
                    if (!isValid)
                    {
                        closingArgs.Cancel = true;
                    }
                    else
                    {
                        MainWindow.Instance.LoadServerMenu();
                        HomePage.Instance.LoadServers();
                    }
                }
            };

            ContentDialogResult result = await serverCreationDialog.ShowAsync();

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

            confirmationDialog.PrimaryButtonClick += async (s, args) =>
            {
                var content = await CoreMethods.deleteServer(serverObj.FolderName, serverObj.DisplayName, serverObj.Id);

                if(content == (true, "Server Deleted"))
                {
                    MainWindow.Instance.LoadServerMenu();
                    HomePage.Instance.LoadServers();
                }
                else
                {
                    ContentDialog confirmationDialog = new ContentDialog()
                    {
                        Title = "Unable to delete the server.",
                        Content = "Rason: Unknown",
                        PrimaryButtonText = "OK",
                    };
                }
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
