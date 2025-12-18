using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using paradigm_ehb.CommandCenter.WinUI.Components;
using paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views;

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

            if (e.Parameter is ServerInfo ip)
            {
                serverObj = ip;
                serverName.Text = ip.Name;
                serverIP.Text = ip.Ip + ":" + ip.Port.ToString();
            }
        }

        public ServerInfo serverObj
        {
            get => (ServerInfo)GetValue(serverIP_Property);
            set => SetValue(serverIP_Property, value);
        }

        public static readonly DependencyProperty serverIP_Property =
        DependencyProperty.Register(
            nameof(serverObj),
            typeof(ServerInfo),
            typeof(ServerMainPage),
            new PropertyMetadata(null));

        private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
            System.Type pageType;

            switch (currentSelectedIndex)
            {
                case 0:
                    pageType = typeof(srvOverview);
                    break;
                default:
                    pageType = typeof(srvOverview);
                    break;
            }

        }
    }
}
