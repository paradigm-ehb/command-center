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

        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ProcessStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ProcessKillMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ProcessRestartMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ProcessViewMenuItem_Click(object sender, RoutedEventArgs e)
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
                        processes.Clear();
                        processes.Add(new ProcessInfo
                        {
                            ProcessId = 0,
                            ProcessName = "(no client)",
                            State = ProcessState.Unspecified,
                            Uptime = 0,
                            NumThreads = 0
                        });
                    }).ConfigureAwait(false);
                    return;
                }

                GetSystemResourcesResponse response = await client.Resources.GetSystemResourcesAsync(new GetSystemResourcesRequest());

                foreach (Process process in response.Resources.Processes)
                {
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        processes.Add(new ProcessInfo
                        {
                            ProcessId = (int)process.Pid,
                            ProcessName = process.Name,
                            State = process.State,
                            Uptime = process.Utime,
                            NumThreads = (int)process.NumThreads
                        });
                    }).ConfigureAwait(false);
                }
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

        private async Task LoadAllProcesses()
        {
            if (client is null || client.Service is null)
            {
                return;
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
