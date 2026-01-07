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
        public ObservableCollection<ServiceInfo> services { get; set; } = new();

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

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await ClearAllServices();
            await LoadAllServices();
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
                await ShowInfoBarAsync("Service Action Result", $"Successfully started the service!", InfoBarSeverity.Success);
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
                await ShowInfoBarAsync("Service Action Result", $"Successfully stopped the service!", InfoBarSeverity.Success);
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
                await ShowInfoBarAsync("Service Action Result", $"Successfully restarted the service!", InfoBarSeverity.Success);
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
                await ShowInfoBarAsync("Service Action Result", $"Successfully enabled the service!", InfoBarSeverity.Success);
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
                await ShowInfoBarAsync("Service Action Result", $"Successfully disabled the service!", InfoBarSeverity.Success);
                await UpdateServiceVisualStateAsync(serviceInfo, "disabled");
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async void ServiceViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Initializes the view model state based on the specified navigation event arguments. Loads service
        /// information using the provided AgentClient or AgentEndpoint parameter.
        /// </summary>
        /// <remarks>If the navigation parameter is an AgentClient, it is used directly. If it is an
        /// AgentEndpoint, the method attempts to retrieve a registered AgentClient for that endpoint. If no valid
        /// client is found, the service list is cleared and an informational message is displayed. Any errors
        /// encountered during initialization are surfaced in the UI as error messages.</remarks>
        /// <param name="e">The navigation event arguments containing the parameter used to determine the AgentClient or AgentEndpoint
        /// for initialization. Must not be null.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
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

                await LoadAllServices();
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

        private async Task ClearAllServices()
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                services.Clear();
                allServices.Clear();
            });
        }
        
        /// <summary>
        /// Asynchronously loads all available services from the connected agent client and updates the internal service
        /// collections.
        /// </summary>
        /// <remarks>This method clears the existing service lists before loading new data. If the agent
        /// client or its service is not available, an error message is displayed and no services are loaded.</remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the agent client returns a null response when requesting the list of services.</exception>
        private async Task LoadAllServices()
        {
            if (client is null || client.Service is null)
            {
                await ShowErrorInfoBarAsync("No valid AgentClient available for refreshing services.");
                return;
            }
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
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    allServices.Add(serviceInfo);
                    services.Add(serviceInfo);
                });
            }
            Order_Services();
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
            Order_Services();
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

        private void Order_Services()
        {
            // Order: enabled first, then disabled, then the rest. Within each group order by Name (case-insensitive).
            List<ServiceInfo> ordered = services
                .OrderBy(s =>
                {
                    if (!string.IsNullOrEmpty(s.Description) && s.Description.Contains("State: enabled", StringComparison.InvariantCultureIgnoreCase))
                        return 0;
                    if (!string.IsNullOrEmpty(s.Description) && s.Description.Contains("State: disabled", StringComparison.InvariantCultureIgnoreCase))
                        return 1;
                    return 2;
                })
                .ThenBy(s => s.Description, StringComparer.InvariantCultureIgnoreCase)
                .ThenBy(s => s.Name, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            services.Clear();
            foreach (ServiceInfo item in ordered)
            {
                services.Add(item);
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

        private async Task ShowInfoBarAsync(string title, string message, InfoBarSeverity severity)
        {
            // Ensure UI thread
            await DispatcherQueue.EnqueueAsync(() =>
            {
                if (GridRoot == null)
                {
                    // fallback to page XamlRoot; but InfoBar must be in visual tree to be visible
                    // if GridRoot is not available the InfoBar won't be shown persistently.
                    return;
                }

                InfoBar infoBar = new()
                {
                    Title = title,
                    Message = message,
                    Severity = severity,
                    IsOpen = true,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, 0, 0)
                };

                // Add to visual tree so it is visible
                GridRoot.Children.Add(infoBar);

                // Remove from visual tree when closed
                void OnClosed(object? s, InfoBarClosedEventArgs args)
                {
                    infoBar.Closed -= OnClosed;
                    if (GridRoot.Children.Contains(infoBar))
                    {
                        GridRoot.Children.Remove(infoBar);
                    }
                }

                infoBar.Closed += OnClosed;
            });
        }

        private async Task ShowErrorInfoBarAsync(string message)
        {
            await ShowInfoBarAsync("Service Action Result", $"Error: {message}", InfoBarSeverity.Error);
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
