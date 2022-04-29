#nullable enable
using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xrm.Tooling.Connector;

namespace CrmSvcUtil.Client.LogOnWindow
{
    /// <summary>
    /// Interaction logic for CrmLogOnForm.xaml
    /// </summary>
    public partial class CrmLogOnForm
    {
        #region Constructors

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        public CrmLogOnForm()
        {
            InitializeComponent();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the selected cloud.
        /// </summary>
        /// <value>
        ///     The selected cloud.
        /// </value>
        private Uri? SelectedDiscoveryUri { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether [use PowerPlatform].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [use PowerPlatform]; otherwise, <c>false</c>.
        /// </value>
        private bool UsePowerPlatform { get; set; }

        #endregion

        #region Events

        /// <summary>
        ///     Handles the KeyUp event of the PowerPlatformUserName control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.
        /// </param>
        private void PowerPlatformUserName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LogIn_Click(sender, e);
            }
        }

        /// <summary>
        ///     Handles the KeyUp event of the PowerPlatformPassword control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="KeyEventArgs"/> instance containing the event data.
        /// </param>
        private void PowerPlatformPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LogIn_Click(sender, e);
            }
        }

        /// <summary>
        ///     Handles the KeyUp event of the Server control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="KeyEventArgs"/> instance containing the event data.
        /// </param>
        private void Server_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LogIn_Click(sender, e);
            }
        }

        /// <summary>
        ///     Handles the KeyUp event of the Port control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="KeyEventArgs"/> instance containing the event data.
        /// </param>
        private void Port_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LogIn_Click(sender, e);
            }
        }

        /// <summary>
        ///     Called when [premise domain key up].
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The <see cref="KeyEventArgs"/> instance containing the event data.
        /// </param>
        private void OnPremiseDomain_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LogIn_Click(sender, e);
            }
        }

        /// <summary>
        ///     Called when [premise user name key up].
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The <see cref="KeyEventArgs"/> instance containing the event data.
        /// </param>
        private void OnPremiseUserName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LogIn_Click(sender, e);
            }
        }

        /// <summary>
        ///     Called when [premise password key up].
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The <see cref="KeyEventArgs"/> instance containing the event data.
        /// </param>
        private void OnPremisePassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LogIn_Click(sender, e);
            }
        }

        /// <summary>
        ///     Handles the Click event of the LogIn control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            if (UsePowerPlatform)
            {
                if (string.IsNullOrEmpty(DiscoveryUrl.SelectionBoxItem.ToString()) ||
                    string.IsNullOrEmpty(PowerPlatformUserName.Text) ||
                    string.IsNullOrEmpty(PowerPlatformPassword.Password))
                {
                    MessageBox.Show("Please specify all the required parameters.");
                    return;
                }
                SetSelectedDiscoveryUri();
                using (Organizations organizations = new(PowerPlatformUserName.Text, PowerPlatformPassword.Password, SelectedDiscoveryUri))
                {
                    organizations.Show();
                }
                Close();
            }
            else
            {
                if (string.IsNullOrEmpty(OnPremiseDomain.Text) ||
                    string.IsNullOrEmpty(OnPremiseUserName.Text) ||
                    string.IsNullOrEmpty(OnPremisePassword.Password) ||
                    string.IsNullOrEmpty(Server.Text) ||
                    string.IsNullOrEmpty(AuthenticationSource.SelectionBoxItem.ToString()))
                {
                    MessageBox.Show("Please specify all the required parameters.");
                    return;
                }
                SetOrganizations();
            }
        }

        /// <summary>
        ///     Handles the Click event of the Cancel control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Called when [premise on checked].
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void OnPremise_OnChecked(object sender, RoutedEventArgs e)
        {
            UsePowerPlatform = false;
            OnPremiseGroupBox.Visibility = Visibility.Visible;
            ServerLabel.Visibility = Visibility.Visible;
            Server.Visibility = Visibility.Visible;
            PortLabel.Visibility = Visibility.Visible;
            Port.Visibility = Visibility.Visible;
            SslLabel.Visibility = Visibility.Visible;
            Ssl.Visibility = Visibility.Visible;
            AuthenticationSourceLabel.Visibility = Visibility.Visible;
            AuthenticationSource.Visibility = Visibility.Visible;
            OnPremiseDomainLabel.Visibility = Visibility.Visible;
            OnPremiseDomain.Visibility = Visibility.Visible;
            OnPremiseUserNameLabel.Visibility = Visibility.Visible;
            OnPremiseUserName.Visibility = Visibility.Visible;
            OnPremisePasswordLabel.Visibility = Visibility.Visible;
            OnPremisePassword.Visibility = Visibility.Visible;
            LogIn.Visibility = Visibility.Visible;
            PowerPlatformGroupBox.Visibility = Visibility.Collapsed;
            DiscoveryUrlLabel.Visibility = Visibility.Collapsed;
            DiscoveryUrl.Visibility = Visibility.Collapsed;
            PowerPlatformUserNameLabel.Visibility = Visibility.Collapsed;
            PowerPlatformUserName.Visibility = Visibility.Collapsed;
            PowerPlatformPasswordLabel.Visibility = Visibility.Collapsed;
            PowerPlatformPassword.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        ///     Handles the OnChecked event of the PowerPlatform control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void PowerPlatform_OnChecked(object sender, RoutedEventArgs e)
        {
            UsePowerPlatform = true;
            PowerPlatformGroupBox.Visibility = Visibility.Visible;
            DiscoveryUrlLabel.Visibility = Visibility.Visible;
            DiscoveryUrl.Visibility = Visibility.Visible;
            PowerPlatformUserNameLabel.Visibility = Visibility.Visible;
            PowerPlatformUserName.Visibility = Visibility.Visible;
            PowerPlatformPasswordLabel.Visibility = Visibility.Visible;
            PowerPlatformPassword.Visibility = Visibility.Visible;
            LogIn.Visibility = Visibility.Visible;
            OnPremiseGroupBox.Visibility = Visibility.Collapsed;
            ServerLabel.Visibility = Visibility.Collapsed;
            Server.Visibility = Visibility.Collapsed;
            PortLabel.Visibility = Visibility.Collapsed;
            Port.Visibility = Visibility.Collapsed;
            SslLabel.Visibility = Visibility.Collapsed;
            Ssl.Visibility = Visibility.Collapsed;
            AuthenticationSourceLabel.Visibility = Visibility.Collapsed;
            AuthenticationSource.Visibility = Visibility.Collapsed;
            OnPremiseDomainLabel.Visibility = Visibility.Collapsed;
            OnPremiseDomain.Visibility = Visibility.Collapsed;
            OnPremiseUserNameLabel.Visibility = Visibility.Collapsed;
            OnPremiseUserName.Visibility = Visibility.Collapsed;
            OnPremisePasswordLabel.Visibility = Visibility.Collapsed;
            OnPremisePassword.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Sets the organizations.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        private void SetOrganizations()
        {
            string port = !string.IsNullOrEmpty(Port.Text) ? Port.Text :
                Ssl.IsChecked.HasValue && Ssl.IsChecked.Value ? "443" : "80";
            switch (AuthenticationSource.SelectionBoxItem.ToString())
            {
                case "Active Directory":
                    using (Organizations organizations = new(OnPremiseDomain.Text, OnPremiseUserName.Text, OnPremisePassword.Password, Server.Text, port, Ssl.IsChecked.HasValue && Ssl.IsChecked.Value, AuthenticationType.AD))
                    {
                        organizations.Show();
                    }
                    Close();
                    break;
                case "Internet Facing Deployment (IFD)":
                    using (Organizations organizations = new(OnPremiseDomain.Text, OnPremiseUserName.Text, OnPremisePassword.Password, Server.Text, port, Ssl.IsChecked.HasValue && Ssl.IsChecked.Value, AuthenticationType.IFD))
                    {
                        organizations.Show();
                    }
                    Close();
                    break;
                case "Open Authorization (OAuth)":
                    using (Organizations organizations = new(OnPremiseDomain.Text, OnPremiseUserName.Text, OnPremisePassword.Password, Server.Text, port, Ssl.IsChecked.HasValue && Ssl.IsChecked.Value, AuthenticationType.OAuth))
                    {
                        organizations.Show();
                    }
                    Close();
                    break;
                default:
                    using (Organizations organizations = new(OnPremiseDomain.Text, OnPremiseUserName.Text, OnPremisePassword.Password, Server.Text, port, Ssl.IsChecked.HasValue && Ssl.IsChecked.Value, AuthenticationType.AD))
                    {
                        organizations.Show();
                    }
                    Close();
                    break;
            }
        }

        /// <summary>
        ///     Sets selected cloud.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 04/03/2022.
        /// </remarks>
        private void SetSelectedDiscoveryUri()
        {
            SelectedDiscoveryUri = DiscoveryUrl.SelectionBoxItem.ToString() switch
            {
                "Commercial" => new Uri("https://globaldisco.crm.dynamics.com/api/discovery/v2.0/Instances"),
                "North America 2 (GCC)" =>
                    new Uri("https://globaldisco.crm9.dynamics.com/api/discovery/v2.0/Instances"),
                "US Government L5 (DOD)" => new Uri(
                    "https://globaldisco.crm.microsoftdynamics.us/api/discovery/v2.0/Instances"),
                "US Government L4 (USG)" => new Uri(
                    "https://globaldisco.crm.appsplatform.us/api/discovery/v2.0/Instances"),
                "China operated by 21Vianet" => new Uri(
                    "https://globaldisco.crm.dynamics.cn/api/discovery/v2.0/Instances"),
                _ => SelectedDiscoveryUri
            };
        }

        #endregion

    }
}
