using Grpc.Core;
using Resources.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.VoiceCommands;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProcessesPage : Page
    {
        AgentClient? client = null;

        public ObservableCollection<ProcessInfo> processes { get; } = new();

        private Collection<ProcessInfo> allProcesses { get; } = new();

        public ProcessesPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Fire-and-forget; exceptions observed inside the task
            _ = InitializeAsync(e);
        }

        private void OnFilterChanged(object sender, RoutedEventArgs args)
        {
            IEnumerable<ProcessInfo> filtered = allProcesses.Where(process => Filter(process));
            Remove_NonMatching(filtered);
            AddBack_Processes(filtered);
            Order_services();
        }

        private bool Filter(ProcessInfo process)
        {
            if (string.IsNullOrWhiteSpace(FilterByName.Text))
            {
                return true;
            }
            return process.ProcessName.Contains(FilterByName.Text, StringComparison.OrdinalIgnoreCase)
                || process.ProcessId.ToString().Contains(FilterByName.Text, StringComparison.OrdinalIgnoreCase);
        }

        private void Remove_NonMatching(IEnumerable<ProcessInfo> filteredData)
        {
            for (int i = processes.Count - 1; i >= 0; i--)
            {
                ProcessInfo process = processes[i];
                if (!filteredData.Contains(process))
                    processes.Remove(process);
            }

        }

        private void AddBack_Processes(IEnumerable<ProcessInfo> filteredData)
        {
            foreach (ProcessInfo process in filteredData)
            {
                if (!processes.Contains(process))
                    processes.Add(process);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await ClearAllProcesses();
            await LoadAllProcesses();
        }

        private void ProcessStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: implement process start
        }

        private async void ProcessKillMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: implement process kill
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = "Are you sure you want to Kill this process?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Kill",
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Kill the Process
            } else
            {
                // Cancel, do nothing
            }
        }

        private void ProcessRestartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: implement process restart
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

                await LoadAllProcesses();
            }
            catch (Exception ex)
            {
                // best-effort: show error in UI list
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    processes.Clear();
                    processes.Add(new ProcessInfo
                    {
                        ProcessId = 0,
                        ProcessName = ex.Message,
                        State = ProcessState.Unspecified,
                        Uptime = 0,
                        NumThreads = 0
                    });
                }).ConfigureAwait(false);
            }
        }

        private async Task ClearAllProcesses()
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                allProcesses.Clear();
                processes.Clear();
            });
        }

        private async Task LoadAllProcesses()
        {
            if (client is null || client.Service is null)
            {
                await ShowErrorInfoBarAsync("No valid AgentClient available.");
                return;
            }
            GetSystemResourcesResponse? response = await client.Resources.GetSystemResourcesAsync(new GetSystemResourcesRequest());
            if (response is null)
            {
                throw new InvalidOperationException("Received null response from ActionAsync");
            }
            foreach (Process? process in response.Resources.Processes)
            {
                SolidColorBrush brush = process.State switch
                {
                    ProcessState.Unspecified => (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"],
                    ProcessState.Running => (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"],
                    ProcessState.Sleeping => (SolidColorBrush)Application.Current.Resources["SystemFillColorCautionBrush"],
                    ProcessState.Stopped => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                    _ => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"],
                };
                ProcessInfo processInfo = new()
                {
                    Fill = brush,
                    ProcessId = (int)process.Pid,
                    ProcessName = process.Name,
                    State = process.State,
                    Uptime = process.Utime,
                    NumThreads = (int)process.NumThreads
                };
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    allProcesses.Add(processInfo);
                    processes.Add(processInfo);
                });
            }
            await DispatcherQueue.EnqueueAsync(() =>
            {
                Order_services();
            });
        }

        public void OnOrderChanged(object sender, SelectionChangedEventArgs args)
        {
            Order_services();
        }

        private void Order_services()
        {
            string order = OrderByCombo?.SelectedValue as string ?? "State";

            List<ProcessInfo> ordered = order switch
            {
                "Name" => processes.OrderBy(p => p.ProcessName).ThenBy(p => p.ProcessId).ToList(),
                "Pid" => processes.OrderBy(p => p.ProcessId).ToList(),
                "Uptime" => processes.OrderByDescending(p => p.Uptime).ToList(),
                "State" => processes.OrderBy(p => p.State).ThenBy(p => p.ProcessName).ToList(),
                _ => processes
                    .OrderBy(p =>
                    {
                        if (p.State.Equals(ProcessState.Running))
                            return 0;
                        if (p.State.Equals(ProcessState.Sleeping))
                            return 1;
                        return 2;
                    })
                    .ThenBy(p => p.State)
                    .ThenBy(p => p.ProcessName)
                    .ToList()
            };

            processes.Clear();
            foreach (ProcessInfo process in ordered)
            {
                processes.Add(process);
            }
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

    public class ProcessInfo
    {
        // Fill
        public SolidColorBrush Fill { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        // PID
        public int ProcessId { get; set; }

        // Process display name
        public string ProcessName { get; set; }

        // Process state as defined in the proto (Resources.V1.ProcessState)
        public ProcessState State { get; set; }

        // Uptime / user-time as provided by the agent (proto: utime). Kept as ulong to match proto uint64
        public ulong Uptime { get; set; }

        // Number of threads for the process
        public int NumThreads { get; set; }

        // Existing fields kept for compatibility with UI that may bind to them
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
    }
}
