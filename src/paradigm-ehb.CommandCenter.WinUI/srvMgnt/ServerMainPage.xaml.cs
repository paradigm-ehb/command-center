using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.WinUI.Components;

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt
{
    public sealed partial class ServerMainPage : Page
    {
        public ServerMainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is AgentEndpoint ip)
            {
                serverObj = ip;
                ServerInfo.Text = ip.DisplayName + "\n" + ip.IpAddress + "\n" + ip.Port ;
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
    }
}
