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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProcessesPage : Page
    {
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ServiceStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ServiceStopMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ServiceRestartMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ServiceViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private async Task InitializeAsync(NavigationEventArgs e)
        {
            try
            {
                // Determine the AgentClient to use based on navigation parameter
                AgentClient? client = null;

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
    }

    public class ProcessInfo
    {
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
