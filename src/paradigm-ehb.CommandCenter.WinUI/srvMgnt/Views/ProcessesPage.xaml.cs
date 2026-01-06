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

        private void ServiceEnableMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ServiceDisableMenuItem_Click(object sender, RoutedEventArgs e)
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
                    processes.Add(new ProcessInfo { ProcessId = 0, ProcessName = ex.Message });
                }).ConfigureAwait(false);
            }
        }
    }

    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
    }
}
