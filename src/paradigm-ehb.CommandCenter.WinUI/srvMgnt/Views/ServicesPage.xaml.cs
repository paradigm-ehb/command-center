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
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    public sealed partial class ServicesPage : Page
    {
        AgentClient? client = null;

        // Observable collection used by x:Bind in XAML
        public ObservableCollection<ServiceInfo> services { get; } = new();

        private Collection<ServiceInfo> allServices { get; } = new();

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
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;

            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            UnitActionReply response = await client.Service.PerformUnitActionAsync(new UnitActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitActionRequest.Types.UnitAction.Start
            });

            if (response.Success)
            {
                InfoBar infoBar = new();

                infoBar.XamlRoot = this.XamlRoot;
                infoBar.Title = "Service Action Result";
                infoBar.Message = $"Successfully started the service!";
                infoBar.Severity = InfoBarSeverity.Success;

                infoBar.IsOpen = true;

                await UpdateServiceVisualStateAsync(serviceInfo, "started");
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async void ServiceStopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;

            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            UnitActionReply response = await client.Service.PerformUnitActionAsync(new UnitActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitActionRequest.Types.UnitAction.Stop
            });

            if (response.Success)
            {
                InfoBar infoBar = new();

                infoBar.XamlRoot = this.XamlRoot;
                infoBar.Title = "Service Action Result";
                infoBar.Message = $"Successfully stopped the service!";
                infoBar.Severity = InfoBarSeverity.Success;

                infoBar.IsOpen = true;

                await UpdateServiceVisualStateAsync(serviceInfo, "stopped");
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async void ServiceRestartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;

            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            UnitActionReply response = await client.Service.PerformUnitActionAsync(new UnitActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitActionRequest.Types.UnitAction.Restart
            });

            if (response.Success)
            {
                InfoBar infoBar = new();

                infoBar.XamlRoot = this.XamlRoot;
                infoBar.Title = "Service Action Result";
                infoBar.Message = $"Successfully restarted the service!";
                infoBar.Severity = InfoBarSeverity.Success;

                infoBar.IsOpen = true;

                await UpdateServiceVisualStateAsync(serviceInfo, "restarted");
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async void ServiceEnableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;

            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            UnitFileActionReply response = await client.Service.PerformUnitFileActionAsync(new UnitFileActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitFileActionRequest.Types.UnitFileAction.Enable
            });

            if (response.Success)
            {
                InfoBar infoBar = new();

                infoBar.XamlRoot = GridRoot.XamlRoot;
                infoBar.Title = "Service Action Result";
                infoBar.Message = $"Successfully enabled the service!";
                infoBar.Severity = InfoBarSeverity.Success;

                infoBar.IsOpen = true;

                await UpdateServiceVisualStateAsync(serviceInfo, "enabled");
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async void ServiceDisableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;

            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            UnitFileActionReply response = await client.Service.PerformUnitFileActionAsync(new UnitFileActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitFileActionRequest.Types.UnitFileAction.Disable
            });

            if (response.Success)
            {
                InfoBar infoBar = new();

                infoBar.XamlRoot = GridRoot.XamlRoot;
                infoBar.Title = "Service Action Result";
                infoBar.Message = $"Successfully disabled the service!";
                infoBar.Severity = InfoBarSeverity.Success;

                infoBar.IsOpen = true;

                await UpdateServiceVisualStateAsync(serviceInfo, "disabled");
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
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
                            Description = "No AgentClient was passed and no registered client exists. Navigate with an AgentClient instance.",
                            Fill = new SolidColorBrush(Colors.Gray)
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

                foreach (LoadedUnit? unit in response.Units)
                {
                    string unitName = ExtractShortUnitName(unit.Name);

                    // Choose a color/brush based on unit state
                    string state = (unit.LoadState ?? string.Empty).ToLowerInvariant();
                    SolidColorBrush brush = state switch
                    {
                        "enabled" => (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"],
                        "static" => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                        "disabled" => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                        _ => new SolidColorBrush(Colors.Goldenrod)
                    };

                    ServiceInfo serviceInfo = new ServiceInfo
                    {
                        Name = unitName,
                        Description = $"State: {unit.LoadState}",
                        Fill = brush
                    };
                    allServices.Add(serviceInfo);
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        services.Add(serviceInfo);
                    });
                }



            }
            catch (Exception ex)
            {
                // best-effort: show error in UI list
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    services.Clear();
                    services.Add(new ServiceInfo { Name = "(error)", Description = ex.Message, Fill = new SolidColorBrush(Colors.Red) });
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns only the last path segment from a unit name.
        /// Handles both '/' and '\' as separators and null/empty inputs.
        /// Examples:
        ///  - "/path/to/foo.service" -> "foo.service"
        ///  - "C:\path\to\bar.service" -> "bar.service"
        ///  - "simple.service" -> "simple.service"
        /// </summary>
        private static string ExtractShortUnitName(string? fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;

            int lastSlash = fullName.LastIndexOf('/');
            int lastBackslash = fullName.LastIndexOf('\\');
            int lastSep = Math.Max(lastSlash, lastBackslash);

            return lastSep >= 0 && lastSep < fullName.Length - 1
                ? fullName.Substring(lastSep + 1)
                : fullName;
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs args)
        {
            IEnumerable<ServiceInfo> filtered = allServices.Where(service => Filter(service));
            Remove_NonMatching(filtered);
            AddBack_Services(filtered);
        }

        private bool Filter(ServiceInfo serviceInfo)
        {
            return serviceInfo.Name.Contains(FilterByName.Text, StringComparison.InvariantCultureIgnoreCase);
        }

        private void Remove_NonMatching(IEnumerable<ServiceInfo> filteredData)
        {
            for (int i = services.Count - 1; i >= 0; i--)
            {
                ServiceInfo item = services[i];
                if (!filteredData.Contains(item)) services.Remove(item);
            }
        }

        private void AddBack_Services(IEnumerable<ServiceInfo> filteredData)
        {
            foreach (ServiceInfo item in filteredData)
            {
                if (!services.Contains(item)) services.Add(item);
            }
        }

        private async Task UpdateServiceVisualStateAsync(ServiceInfo serviceInfo, string action)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                string desc = action switch
                {
                    "started" => "State: running",
                    "stopped" => "State: stopped",
                    "restarted" => "State: running",
                    "enabled" => "State: enabled",
                    "disabled" => "State: disabled",
                    _ => serviceInfo.Description
                };

                SolidColorBrush brush = action switch
                {
                    "started" => new SolidColorBrush(Colors.Green),
                    "stopped" => new SolidColorBrush(Colors.Gray),
                    "restarted" => new SolidColorBrush(Colors.Green),
                    "enabled" => (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"],
                    "disabled" => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                    _ => serviceInfo.Fill
                };

                serviceInfo.Description = desc;
                serviceInfo.Fill = brush;
            });
        }

        private async Task ShowErrorInfoBarAsync(string message)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                InfoBar infoBar = new();

                infoBar.XamlRoot = this.XamlRoot;
                infoBar.Title = "Service Action Result";
                infoBar.Message = $"Error: {message}";
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.IsOpen = true;
            });
        }
    }

    // Small view-model used by the DataTemplate in XAML.
    public sealed class ServiceInfo : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
                }
            }
        }

        private SolidColorBrush _fill = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush Fill
        {
            get => _fill;
            set
            {
                if (_fill != value)
                {
                    _fill = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fill)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
