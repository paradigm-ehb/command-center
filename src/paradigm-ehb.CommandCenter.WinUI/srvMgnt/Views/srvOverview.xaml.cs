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
using Resources.V2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    public sealed partial class srvOverview : Page
    {
        AgentClient? client = null;

        serverResources serverResources = new();

        public srvOverview()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);

            // Fire and forget
            _ = InitializeAsync(args);
        }

        private async Task InitializeAsync(NavigationEventArgs args)
        {
            try
            {
                // Determine the AgentClient to use based on navigation parameter
                if (args.Parameter is AgentClient passedClient)
                {
                    client = passedClient;
                }
                else if (args.Parameter is AgentEndpoint endpoint)
                {
                    // Try to obtain an existing registered client only.
                    IAgentClientRegistry clientRegistry = App.Services.GetRequiredService<IAgentClientRegistry>();
                    client = await clientRegistry.GetAsync(endpoint.Id).ConfigureAwait(false);
                }

                await GetResourcesAsync();
            }
            catch (Exception ex)
            {
            }
        }

        private async Task GetResourcesAsync()
        {
            if (client is null || client.Resources is null)
                return;

            GetSystemResourcesResponse? response = await client.Resources.GetSystemResourcesAsync(new GetSystemResourcesRequest());

            if (response is not null)
            {
                double.TryParse(response.Resources.Memory.Total, out double totalMem);
                double.TryParse(response.Resources.Memory.Free, out double freeMem);
                serverResources.MemoryPercent = (freeMem / totalMem) * 100;
                this.DataContext = serverResources;
            }
        }
    }

    public sealed class serverResources : INotifyPropertyChanged
    {
        private double _memoryPercent = 42;
        public double MemoryPercent
        {
            get => _memoryPercent;
            set
            {
                if (_memoryPercent != value)
                {
                    _memoryPercent = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MemoryPercent)));
                }
            }
        }
        public string MemoryUsageText
        {
            get => $"{Math.Round(_memoryPercent, 2)}%";
        }

        private double _cpuPercent = 15;
        public double CpuPercent
        {
            get => _cpuPercent;
            set
            {
                if (_cpuPercent != value)
                {
                    _cpuPercent = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpuPercent)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
