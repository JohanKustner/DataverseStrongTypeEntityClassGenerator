#nullable enable

using CrmSvcUtil.Client.LogOnWindow;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Tooling.CrmConnectControl;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security;
using System.ServiceModel.Description;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CrmSvcUtil.Client
{
    /// <summary>
    /// Interaction logic for Organizations.xaml
    /// </summary>
    public sealed partial class Organizations : IDisposable
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Organizations" /> class.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 04/03/2022.
        /// </remarks>
        /// <param name="powerPlatformUserName">
        ///     Name of the PowerPlatform user.
        /// </param>
        /// <param name="powerPlatformPassword">
        ///     The PowerPlatform password.
        /// </param>
        /// <param name="discoveryUrl">
        ///     The discoveryUrl.
        /// </param>
        public Organizations(string? powerPlatformUserName, string? powerPlatformPassword, Uri? discoveryUrl)
        {
            IsPowerPlatform = true;
            CrmConnectionManager = new CrmConnectionManager();
            PowerPlatformUserName = powerPlatformUserName;
            PowerPlatformPassword = powerPlatformPassword;
            DiscoveryUrl = discoveryUrl;
            AuthenticationType = AuthenticationType.OAuth;
            ClientCredentials = new ClientCredentials
            {
                UserName =
                {
                    UserName = powerPlatformUserName,
                    Password = powerPlatformPassword
                }
            };
            InitializeComponent();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Organizations" /> class.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="onPremiseDomain">
        ///     The on premise domain.
        /// </param>
        /// <param name="onPremiseUserName">
        ///     Name of the on premise user.
        /// </param>
        /// <param name="onPremisePassword">
        ///     The on premise password.
        /// </param>
        /// <param name="server">
        ///     The server.
        /// </param>
        /// <param name="port">
        ///     The port.
        /// </param>
        /// <param name="useSsl">
        ///     if set to <c>true</c> [use SSL].
        /// </param>
        /// <param name="authenticationType">
        ///     Type of the authentication.
        /// </param>
        [CLSCompliant(false)]
        public Organizations(string? onPremiseDomain, string? onPremiseUserName, string? onPremisePassword, string? server, string? port, bool useSsl, AuthenticationType authenticationType)
        {
            IsAuthenticationType = true;
            CrmConnectionManager = new CrmConnectionManager();
            OnPremiseDomain = onPremiseDomain;
            OnPremiseUserName = onPremiseUserName;
            OnPremisePassword = onPremisePassword;
            Server = server;
            Port = port;
            UseSsl = useSsl;
            AuthenticationType = authenticationType;
            DiscoveryUrl = new Uri(NonPowerPlatformDiscoveryUrl());
            ClientCredentials = new ClientCredentials
            {
                UserName =
                {
                    UserName = string.Format(CultureInfo.CurrentCulture, "{0}\\{1}", OnPremiseDomain, OnPremiseUserName),
                    Password = OnPremisePassword
                }
            };
            InitializeComponent();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the connection string.
        /// </summary>
        /// <value>
        ///     The connection string.
        /// </value>
        private string ConnectionString =>
            $"AuthType={AuthenticationType}; ClientId = {ClientId}; " +
            $"Username = {PowerPlatformUserName}; Password = {PowerPlatformPassword}; " +
            $"Url = {OrganizationInformation?.WebApplicationEndpoint}; RedirectUri = {RedirectUri}; " +
            $"LoginPrompt = {PromptBehavior.Auto};";

        /// <summary>
        ///     Gets a value indicating whether this instance is PowerPlatform.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is PowerPlatform; otherwise, <c>false</c>.
        /// </value>
        private bool IsPowerPlatform { get; }

        /// <summary>
        ///     Gets a value indicating whether this instance is authentication type.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is authentication type; otherwise, <c>false</c>.
        /// </value>
        private bool IsAuthenticationType { get; }

        /// <summary>
        ///     CRM Connection Manager.
        /// </summary>
        /// <value>
        ///     The crm connection manager.
        /// </value>
        private CrmConnectionManager CrmConnectionManager { get; set; }

        /// <summary>
        ///     (Immutable) identifier for the client.
        /// </summary>
        private const string ClientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";

        /// <summary>
        ///     (Immutable) URI of the redirect.
        /// </summary>
        private static readonly Uri RedirectUri = new("app://58145B91-0C36-4500-8554-080854F2AC97");

        /// <summary>
        ///     Gets the client credentials.
        /// </summary>
        /// <value>
        ///     The client credentials.
        /// </value>
        private ClientCredentials ClientCredentials { get; }

        /// <summary>
        ///     Gets the type of the authentication.
        /// </summary>
        /// <value>
        ///     The type of the authentication.
        /// </value>
        private AuthenticationType AuthenticationType { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether [use SSL].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [use SSL]; otherwise, <c>false</c>.
        /// </value>
        private bool UseSsl { get; set; }

        /// <summary>
        ///     Gets the port.
        /// </summary>
        /// <value>
        ///     The port.
        /// </value>
        private string? Port { get; }

        /// <summary>
        ///     Gets the server.
        /// </summary>
        /// <value>
        ///     The server.
        /// </value>
        private string? Server { get; }

        /// <summary>
        ///     Gets the on premise domain.
        /// </summary>
        /// <value>
        ///     The on premise domain.
        /// </value>
        private string? OnPremiseDomain { get; }

        /// <summary>
        ///     Gets the on premise password.
        /// </summary>
        /// <value>
        ///     The on premise password.
        /// </value>
        private string? OnPremisePassword { get; }

        /// <summary>
        ///     Gets the name of the on premise user.
        /// </summary>
        /// <value>
        ///     The name of the on premise user.
        /// </value>
        private string? OnPremiseUserName { get; }

        /// <summary>
        ///     Gets or sets the organization detail.
        /// </summary>
        /// <value>
        ///     The organization detail.
        /// </value>
        private OrganizationDetail? OrganizationDetail { get; set; }

        /// <summary>
        ///     Gets or sets information describing the organization.
        /// </summary>
        /// <value>
        ///     Information describing the organization.
        /// </value>
        private OrganizationInformation? OrganizationInformation { get; set; }

        /// <summary>
        ///     Gets the discoveryUrl.
        /// </summary>
        /// <value>
        ///     The discoveryUrl.
        /// </value>
        private Uri? DiscoveryUrl { get; }

        /// <summary>
        ///     Gets the PowerPlatform password.
        /// </summary>
        /// <value>
        ///     The PowerPlatform password.
        /// </value>
        private string? PowerPlatformPassword { get; }

        /// <summary>
        ///     Gets the name of the PowerPlatform user.
        /// </summary>
        /// <value>
        ///     The name of the PowerPlatform user.
        /// </value>
        private string? PowerPlatformUserName { get; }

        #endregion

        #region Events

        /// <summary>
        ///     Handles the SelectionChanged event of the OrganizationListView control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="SelectionChangedEventArgs"/> instance containing the event data.
        /// </param>
        private void OrganizationListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OrganizationInformation = (OrganizationInformation)OrganizationListView.SelectedItem;
            OrganizationDetail = new OrganizationDetail
            {
                Geo = OrganizationInformation.Geo,
                UrlName = OrganizationInformation.UrlName,
                DatacenterId = OrganizationInformation.DatacenterId,
                EnvironmentId = OrganizationInformation.EnvironmentId,
                FriendlyName = OrganizationInformation.FriendlyName,
                OrganizationId = OrganizationInformation.OrganizationId,
                OrganizationVersion = OrganizationInformation.OrganizationVersion,
                State = OrganizationInformation.State,
                TenantId = OrganizationInformation.TenantId,
                UniqueName = OrganizationInformation.UniqueName
            };
            OrganizationDetail.Endpoints.Add(new KeyValuePair<EndpointType, string?>(EndpointType.OrganizationDataService, OrganizationInformation.OrganizationDataService));
            OrganizationDetail.Endpoints.Add(new KeyValuePair<EndpointType, string?>(EndpointType.OrganizationService, OrganizationInformation.OrganizationService));
            OrganizationDetail.Endpoints.Add(new KeyValuePair<EndpointType, string?>(EndpointType.WebApplication, OrganizationInformation.WebApplicationEndpoint));
            LogIn.Visibility = Visibility.Visible;
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
        ///     The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            if (IsPowerPlatform)
            {
                UsePowerPlatform();
            }
            if (IsAuthenticationType)
            {
                UseAuthenticationType();
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
        ///     The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Handles the Loaded event of the OrganizationListView control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void OrganizationListView_Loaded(object sender, RoutedEventArgs e)
        {
            GetOrganizationDetails();
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Non Power Platform discovery URL.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <returns>
        ///     A string.
        /// </returns>
        private string NonPowerPlatformDiscoveryUrl()
        {
            if (!string.IsNullOrEmpty(Port))
            {
                return AuthenticationType switch
                {
                    AuthenticationType.AD => string.Format(CultureInfo.CurrentCulture,
                        "https://{0}:{1}/XRMServices/2011/Discovery.svc", Server, Port),
                    AuthenticationType.IFD => string.Format(CultureInfo.CurrentCulture,
                        "https://dev.{0}:{1}/XRMServices/2011/Discovery.svc", Server, Port),
                    AuthenticationType.OAuth => string.Format(CultureInfo.CurrentCulture,
                        "https://{0}:{1}/XRMServices/2011/Discovery.svc", Server, Port),
                    _ => string.Format(CultureInfo.CurrentCulture, "https://{0}:{1}/XRMServices/2011/Discovery.svc",
                        Server, Port)
                };
            }

            return AuthenticationType switch
            {
                AuthenticationType.AD => string.Format(CultureInfo.CurrentCulture,
                    "https://{0}/XRMServices/2011/Discovery.svc", Server),
                AuthenticationType.IFD => string.Format(CultureInfo.CurrentCulture,
                    "https://dev.{0}/XRMServices/2011/Discovery.svc", Server),
                AuthenticationType.OAuth => string.Format(CultureInfo.CurrentCulture,
                    "https://{0}/XRMServices/2011/Discovery.svc", Server),
                _ => string.Format(CultureInfo.CurrentCulture, "https://{0}/XRMServices/2011/Discovery.svc", Server)
            };
        }

        /// <summary>
        ///     Uses Power Platform.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        private void UsePowerPlatform()
        {
            UseSsl = true;
            using SecureString secureString = new();
            if (PowerPlatformPassword != null)
            {
                foreach (char c in PowerPlatformPassword)
                {
                    secureString.AppendChar(c);
                }
            }
            secureString.MakeReadOnly();
            using (CrmConnectionManager = new CrmConnectionManager())
            {
                CrmConnectionManager.CrmSvc = new CrmServiceClient(ConnectionString);
                if (CrmConnectionManager.CrmSvc is { IsReady: true })
                {
                    string? organizationInformationWebApplicationEndpoint = OrganizationInformation?.WebApplicationEndpoint;
                    if (organizationInformationWebApplicationEndpoint != null)
                    {
                        using MainWindow mainWindow = new(PowerPlatformUserName, secureString,
                            new Uri(organizationInformationWebApplicationEndpoint));
                        mainWindow.Show();
                    }
                    Close();
                }
                else
                {
                    MessageBox.Show("Connection Unsuccessful!");
                }
            }
        }

        /// <summary>
        ///     Uses the type of the authentication.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        private void UseAuthenticationType()
        {
            using (CrmConnectionManager = new CrmConnectionManager())
            {
                string? organizationInformationUrlName = OrganizationInformation?.UrlName;
                CrmConnectionManager.CrmSvc = new CrmServiceClient(
                        new NetworkCredential(OnPremiseUserName, OnPremisePassword, OnPremiseDomain), AuthenticationType, Server, Port,
                        organizationInformationUrlName, false, UseSsl, OrganizationDetail);
                if (CrmConnectionManager.CrmSvc is { IsReady: true })
                {
                    if (organizationInformationUrlName != null)
                    {
                        using MainWindow mainWindow = new(OnPremiseDomain, OnPremiseUserName, OnPremisePassword,
                            AuthenticationType, Server, Port, organizationInformationUrlName, UseSsl,
                            OrganizationDetail);
                        mainWindow.Show();
                    }
                    Close();
                }
                else
                {
                    MessageBox.Show("Connection Unsuccessful!");
                }
            }
        }

        /// <summary>
        ///     Gets the organization details.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        private void GetOrganizationDetails()
        {
            try
            {
                OrganizationDetailCollection organizations;
                if (IsPowerPlatform)
                {
                    // Call to get organizations from global discovery
                    organizations = CrmServiceClient.DiscoverGlobalOrganizations(
                        DiscoveryUrl,
                        ClientCredentials,
                        null,
                        ClientId,
                        RedirectUri,
                        "",
                        false,
                        string.Empty);
                }
                else
                {
                    // Call to get organizations from global discovery
                    using DiscoveryServiceProxy discoveryServiceProxy = new(DiscoveryUrl, null, ClientCredentials, null);
                    RetrieveOrganizationsRequest retrieveOrganizationsRequest = new()
                    {
                        AccessType = EndpointAccessType.Default,
                        Release = OrganizationRelease.Current
                    };
                    RetrieveOrganizationsResponse retrieveOrganizationsResponse =
                        (RetrieveOrganizationsResponse)discoveryServiceProxy.Execute(
                            retrieveOrganizationsRequest);
                    organizations = retrieveOrganizationsResponse.Details;
                }
                GridView gridView = new();
                GridViewColumn friendlyNameGridViewColumn = new()
                {
                    Header = "Friendly Name",
                    Width = 123,
                    DisplayMemberBinding = new Binding("FriendlyName")
                };
                gridView.Columns.Add(friendlyNameGridViewColumn);
                GridViewColumn uniqueNameGridViewColumn = new()
                {
                    Header = "Unique Name",
                    Width = 123,
                    DisplayMemberBinding = new Binding("UniqueName")
                };
                gridView.Columns.Add(uniqueNameGridViewColumn);
                GridViewColumn versionGridViewColumn = new()
                {
                    Header = "Version",
                    Width = 123,
                    DisplayMemberBinding = new Binding("OrganizationVersion")
                };
                gridView.Columns.Add(versionGridViewColumn);
                GridViewColumn urlNameGridViewColumn = new()
                {
                    Header = "URL Name",
                    Width = 123,
                    DisplayMemberBinding = new Binding("UrlName")
                };
                gridView.Columns.Add(urlNameGridViewColumn);
                GridViewColumn regionGridViewColumn = new()
                {
                    Header = "Region",
                    Width = 123,
                    DisplayMemberBinding = new Binding("Geo")
                };
                gridView.Columns.Add(regionGridViewColumn);
                GridViewColumn urlGridViewColumn = new()
                {
                    Header = "Web Application Endpoint",
                    Width = 123,
                    DisplayMemberBinding = new Binding("WebApplicationEndpoint")
                };
                gridView.Columns.Add(urlGridViewColumn);
                GridViewColumn organizationDataServiceGridViewColumn = new()
                {
                    Header = "Organization Data Service",
                    Width = 123,
                    DisplayMemberBinding = new Binding("OrganizationDataService")
                };
                gridView.Columns.Add(organizationDataServiceGridViewColumn);
                GridViewColumn organizationServiceGridViewColumn = new()
                {
                    Header = "Organization Service",
                    Width = 123,
                    DisplayMemberBinding = new Binding("OrganizationService")
                };
                gridView.Columns.Add(organizationServiceGridViewColumn);
                GridViewColumn datacenterIdGridViewColumn = new()
                {
                    Header = "Data Centre",
                    Width = 123,
                    DisplayMemberBinding = new Binding("DatacenterId")
                };
                gridView.Columns.Add(datacenterIdGridViewColumn);
                GridViewColumn environmentIdGridViewColumn = new()
                {
                    Header = "Environment",
                    Width = 123,
                    DisplayMemberBinding = new Binding("EnvironmentId")
                };
                gridView.Columns.Add(environmentIdGridViewColumn);
                GridViewColumn organizationIdGridViewColumn = new()
                {
                    Header = "Organization",
                    Width = 123,
                    DisplayMemberBinding = new Binding("OrganizationId")
                };
                gridView.Columns.Add(organizationIdGridViewColumn);
                GridViewColumn stateGridViewColumn = new()
                {
                    Header = "State",
                    Width = 123,
                    DisplayMemberBinding = new Binding("State")
                };
                gridView.Columns.Add(stateGridViewColumn);
                GridViewColumn tenantIdGridViewColumn = new()
                {
                    Header = "Tenant",
                    Width = 123,
                    DisplayMemberBinding = new Binding("TenantId")
                };
                gridView.Columns.Add(tenantIdGridViewColumn);
                OrganizationListView.View = gridView;
                foreach (OrganizationDetail organizationDetail in organizations)
                {
                    OrganizationInformation organizationInformation = new()
                    {
                        Geo = organizationDetail.Geo,
                        UrlName = organizationDetail.UrlName,
                        DatacenterId = organizationDetail.DatacenterId,
                        EnvironmentId = organizationDetail.EnvironmentId,
                        FriendlyName = organizationDetail.FriendlyName,
                        OrganizationDataService = organizationDetail.Endpoints.FirstOrDefault(x => x.Key == EndpointType.OrganizationDataService).Value,
                        OrganizationId = organizationDetail.OrganizationId,
                        OrganizationService = organizationDetail.Endpoints.FirstOrDefault(x => x.Key == EndpointType.OrganizationService).Value,
                        OrganizationVersion = organizationDetail.OrganizationVersion,
                        State = organizationDetail.State,
                        TenantId = organizationDetail.TenantId,
                        UniqueName = organizationDetail.UniqueName,
                        WebApplicationEndpoint = organizationDetail.Endpoints.FirstOrDefault(x => x.Key == EndpointType.WebApplication).Value
                    };
                    OrganizationListView.Items.Add(organizationInformation);
                }
            }
            catch (UriFormatException)
            {
                MessageBox.Show("Please check the details specified as no Organizations could be loaded!",
                    Properties.Resources.Organizations_OrganizationListView_Loaded_Loading_Organizations, MessageBoxButton.OK, MessageBoxImage.Error);
                new CrmLogOnForm().Show();
                Close();
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Please check the details specified as no Organizations could be loaded!",
                    Properties.Resources.Organizations_OrganizationListView_Loaded_Loading_Organizations, MessageBoxButton.OK, MessageBoxImage.Error);
                new CrmLogOnForm().Show();
                Close();
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources;
        ///                                 <c>false</c> to release only unmanaged resources.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            CrmConnectionManager.Dispose();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting
        ///     unmanaged resources.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Finalizes an instance of the <see cref="Organizations"/> class.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        ~Organizations()
        {
            Dispose(false);
        }

        #endregion
    }
}
