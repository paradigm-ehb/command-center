using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Services.V2;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    public sealed partial class ServicesPage : Page
    {
        AgentClient? client = null;

        // Observable collection used by x:Bind in XAML
        public ObservableCollection<ServiceInfo> services { get; } = new();

        public ServicesPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Fire-and-forget; exceptions observed inside the task
            _ = InitializeAsync(e);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void ServiceStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ServiceActionReply response = await client.Service.PerformActionAsync(new ServiceActionRequest
            {
                ServiceName = "mariadb.service",
                UnitAction = ServiceActionRequest.Types.UnitAction.Start
            });

            InfoBar infoBar = new();

            infoBar.Title = "Service Action Result";
            infoBar.Message = response.Success ? $"Successfully started the service!" : $"Error: {response.ErrorMessage}";
            infoBar.Severity = response.Success ? InfoBarSeverity.Success : InfoBarSeverity.Error;

            infoBar.IsOpen = true;
        }

        private async void ServiceStopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ServiceActionReply response = await client.Service.PerformActionAsync(new ServiceActionRequest
            {
                ServiceName = "mariadb.service",
                UnitAction = ServiceActionRequest.Types.UnitAction.Stop
            });

            InfoBar infoBar = new();

            infoBar.Title = "Service Action Result";
            infoBar.Message = response.Success ? $"Successfully stopped the service!" : $"Error: {response.ErrorMessage}";
            infoBar.Severity = response.Success ? InfoBarSeverity.Success : InfoBarSeverity.Error;

            infoBar.IsOpen = true;
        }

        private async void ServiceRestartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ServiceActionReply response = await client.Service.PerformActionAsync(new ServiceActionRequest
            {
                ServiceName = "mariadb.service",
                UnitAction = ServiceActionRequest.Types.UnitAction.Restart
            });

            InfoBar infoBar = new();

            infoBar.Title = "Service Action Result";
            infoBar.Message = response.Success ? $"Successfully restarted the service!" : $"Error: {response.ErrorMessage}";
            infoBar.Severity = response.Success ? InfoBarSeverity.Success : InfoBarSeverity.Error;

            infoBar.IsOpen = true;
        }

        private async void ServiceEnableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ServiceActionReply response = await client.Service.PerformActionAsync(new ServiceActionRequest
            {
                ServiceName = "mariadb.service",
                UnitFileAction = ServiceActionRequest.Types.UnitFileAction.Enable
            });

            InfoBar infoBar = new();

            infoBar.Title = "Service Action Result";
            infoBar.Message = response.Success ? $"Successfully enabled the service!" : $"Error: {response.ErrorMessage}";
            infoBar.Severity = response.Success ? InfoBarSeverity.Success : InfoBarSeverity.Error;

            infoBar.IsOpen = true;
        }

        private async void ServiceDisableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ServiceActionReply response = await client.Service.PerformActionAsync(new ServiceActionRequest
            {
                ServiceName = "mariadb.service",
                UnitFileAction = ServiceActionRequest.Types.UnitFileAction.Disable
            });

            InfoBar infoBar = new();

            infoBar.Title = "Service Action Result";
            infoBar.Message = response.Success ? $"Successfully disabled the service!" : $"Error: {response.ErrorMessage}";
            infoBar.Severity = response.Success ? InfoBarSeverity.Success : InfoBarSeverity.Error;

            infoBar.IsOpen = true;
        }

        private void ServiceViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private async Task InitializeAsync(NavigationEventArgs e)
        {
            try
            {
                // Determine the AgentClient to use based on navigation parameter

                if (e.Parameter is AgentClient passedClient)
                {
                    client = passedClient;
                }
                else if (e.Parameter is AgentEndpoint endpoint)
                {
                    // Try to obtain an existing registered client only.
                    IAgentClientRegistry clientRegistry = App.Services.GetRequiredService<IAgentClientRegistry>();
                    client = await clientRegistry.GetAsync(endpoint.Id).ConfigureAwait(false);
                }

                if (client is null || client.Service is null)
                {
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        services.Clear();
                        services.Add(new ServiceInfo
                        {
                            Name = "(no client)",
                            Description = "No AgentClient was passed and no registered client exists. Navigate with an AgentClient instance."
                        });
                    }).ConfigureAwait(false);
                    return;
                }

                // Example unary call using the passed client (keep as example; adapt to your RPCs).
                GetUnitsReply? response = await client.Service.GetAllUnitsAsync(new GetUnitsRequest());

                if (response is null)
                {
                    throw new InvalidOperationException("Received null response from ActionAsync.");
                }

                await DispatcherQueue.EnqueueAsync(() =>
                {
                    services.Add(new ServiceInfo
                    {
                        Name = "Response!",
                        Description = $"Status: {response.UnitsData.ToStringUtf8()}"
                    });
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // best-effort: show error in UI list
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    services.Clear();
                    services.Add(new ServiceInfo { Name = "(error)", Description = ex.Message });
                }).ConfigureAwait(false);
            }
        }
    }

    // Small view-model used by the DataTemplate in XAML.
    public sealed class ServiceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
