using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
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
        public static ServerMainPage Instance;

        private readonly IAgentClientFactory _agentClientFactory;
        private readonly IAgentClientRegistry _agentClientRegistry;
        private int previousSelectedIndex = 0;

        public ServerMainPage()
        {
            Instance = this;
            // Dependency Injection
            _agentClientFactory = App.Services.GetRequiredService<IAgentClientFactory>();
            _agentClientRegistry = App.Services.GetRequiredService<IAgentClientRegistry>();

            InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Fire-and-forget the async initialization. Exceptions are observed inside the task.
            _ = InitializeForNavigationAsync(e);
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            AgentClient? agentClient = await _agentClientRegistry.GetAsync(serverObj.Id);
            if (agentClient is not null) await agentClient.DisposeAsync();
            await _agentClientRegistry.DeregisterAsync(serverObj.Id);
        }

        private async Task InitializeForNavigationAsync(NavigationEventArgs e)
        {
            bool connected = false;
            try
            {
                if (e.Parameter is AgentEndpoint ip)
                {
                    // Ensure the agent client is created and registered (use await instead of blocking)
                    bool registered = await _agentClientRegistry.IsRegisteredAsync(ip.Id).ConfigureAwait(false);
                    if (!registered)
                    {
                        if (ip.Reachability == Core.Enums.AgentReachability.Offline)
                        {
                            // Create ContentDialog to warn user that the agent is offline and do not give the ability to change SelectorBar
                            await DispatcherQueue.EnqueueAsync(async () =>
                            {
                                // Disable SelectorBar to prevent navigation
                                SelectorBar.IsEnabled = false;
                                TextBlock textBlock = new TextBlock
                                {
                                    Text = "The agent is currently offline. Please ensure the agent is online to access its features.",
                                    TextWrapping = TextWrapping.Wrap,
                                    Margin = new Thickness(20),
                                    Style = Application.Current.Resources["TitleTextBlockStyle"] as Style,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center
                                };

                                ContentFrame.Content = textBlock;

                                ContentDialog contentDialog = new ContentDialog
                                {
                                    Title = "Agent Offline",
                                    Content = "Could not connect to the agent.",
                                    CloseButtonText = "OK",
                                    XamlRoot = MainWindow.Instance.Content.XamlRoot // Set the XamlRoot to the main window's XamlRoot
                                };
                                ContentDialogResult result = await contentDialog.ShowAsync();
                            });
                        }
                        else
                        {
                            await _agentClientFactory.CreateAndRegisterClientAsync(ip).ConfigureAwait(false);
                            connected = true;
                        }
                    }
                    else
                    {
                        connected = true;
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
            finally
            {
                if (connected)
                {
                    // Navigate to the default page (Overview) after initialization
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        SelectorBar.SelectedItem = SelectorBar.Items[0];
                    });
                }
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

        // Make the handler async so we can await client resolution/creation before navigating.
        private async void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
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
                    pageType = typeof(ServicesPage);
                    break;
                case 2:
                    pageType = typeof(ProcessesPage);
                    break;
                default:
                    pageType = typeof(srvOverview);
                    break;
            }

            // Pass serverObj as the navigation parameter
            AgentEndpoint navigationParameter = serverObj;

            SlideNavigationTransitionEffect slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

            // Navigate with the resolved parameter
            ContentFrame.Navigate(pageType, navigationParameter, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

            previousSelectedIndex = currentSelectedIndex;
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

            serverCreationDialog.Closing += async (dialog, closingArgs) =>
            {
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


                        IAgentEndpointRegistry registry = App.Services.GetRequiredService<IAgentEndpointRegistry>();
                        var endpoint = await registry.GetAsync(serverObj.Id);

                        serverIP.Text = endpoint.IpAddress + ":" + endpoint.Port.ToString();
                        serverName.Text = endpoint.DisplayName;
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

                if (content == (true, "Server Deleted"))
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

        private async void ShutdownServer_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog confirmationDialog = new ContentDialog()
            {
                Title = "Are you sure you want to shutdown this server?",
                Content = "This will shutdown the remote server.",
                PrimaryButtonText = "Shutdown",
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await confirmationDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                AgentClient? agentClient = await _agentClientRegistry.GetAsync(serverObj.Id);
                if (agentClient is not null)
                {
                    try
                    {
                        await agentClient.DeviceActions.ActionAsync(new Actions.V1.ActionRequest()
                        {
                            DeviceAction = Actions.V1.DeviceAction.Shutdown
                        });

                        Frame.Navigate(typeof(HomePage));
                    }
                    catch (Exception ex)
                    {
                        ILogger logger = App.Services.GetRequiredService<ILogger<ServerMainPage>>();
                        logger.LogError(ex, "Error shutting down the server.");
                    }
                }
            }
        }

        private async void RebootServer_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog confirmationDialog = new ContentDialog()
            {
                Title = "Are you sure you want to restart this server?",
                Content = "This will restart the remote server.",
                PrimaryButtonText = "Restart",
                CloseButtonText = "Cancel"
            };
            ContentDialogResult result = await confirmationDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                AgentClient? agentClient = await _agentClientRegistry.GetAsync(serverObj.Id);
                if (agentClient is not null)
                {
                    try
                    {
                        await agentClient.DeviceActions.ActionAsync(new Actions.V1.ActionRequest()
                        {
                            DeviceAction = Actions.V1.DeviceAction.Reboot
                        });

                        Frame.Navigate(typeof(HomePage));
                    }
                    catch (Exception ex)
                    {
                        ILogger logger = App.Services.GetRequiredService<ILogger<ServerMainPage>>();
                        logger.LogError(ex, "Error restarting the server.");
                    }
                }
            }
        }

        private async void SuspendServer_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog confirmationDialog = new ContentDialog()
            {
                Title = "Are you sure you want to suspend this server?",
                Content = "This will suspend the remote server.",
                PrimaryButtonText = "Suspend",
                CloseButtonText = "Cancel"
            };
            ContentDialogResult result = await confirmationDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                AgentClient? agentClient = await _agentClientRegistry.GetAsync(serverObj.Id);
                if (agentClient is not null)
                {
                    try
                    {
                        await agentClient.DeviceActions.ActionAsync(new Actions.V1.ActionRequest()
                        {
                            DeviceAction = Actions.V1.DeviceAction.Suspend
                        });

                        Frame.Navigate(typeof(HomePage));
                    }
                    catch (Exception ex)
                    {
                        ILogger logger = App.Services.GetRequiredService<ILogger<ServerMainPage>>();
                        logger.LogError(ex, "Error suspending the server.");
                    }
                }
            }
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
