#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CrmSvcUtil.Client.Exceptions;
using EnvDTE;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Tooling.CrmConnectControl;
using Microsoft.Xrm.Tooling.Ui.Styles;
using static System.String;

namespace CrmSvcUtil.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : IDisposable
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="userName">
        ///     Name of the user.
        /// </param>
        /// <param name="secureString">
        ///     The secure string.
        /// </param>
        /// <param name="applicationUrl">
        ///     The region.
        /// </param>
        public MainWindow(string? userName, SecureString? secureString, Uri? applicationUrl)
        {
            UserName = userName;
            SecureString = secureString;
            Password = new NetworkCredential(UserName, SecureString).Password;
            ApplicationUrl = applicationUrl;
            AuthenticationType = AuthenticationType.OAuth;
            UseSsl = true;
            InitializeComponent();
            AddColumns();
            ConfigureSelectAll();
            // ReSharper disable once UnusedVariable
            _ = new WindowResourceDictionary();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="domain">
        ///     The domain.
        /// </param>
        /// <param name="userName">
        ///     Name of the user.
        /// </param>
        /// <param name="password">
        ///     The password.
        /// </param>
        /// <param name="authenticationType">
        ///     Type of the authentication.
        /// </param>
        /// <param name="server">
        ///     The server.
        /// </param>
        /// <param name="port">
        ///     The port.
        /// </param>
        /// <param name="organizationName">
        ///     The name of the organization.
        /// </param>
        /// <param name="useSsl">
        ///     if set to <c>true</c> [use SSL].
        /// </param>
        /// <param name="organizationDetail">
        ///     The organization detail.
        /// </param>
        [CLSCompliant(false)]
        public MainWindow(string? domain, string? userName, string? password, AuthenticationType authenticationType, string? server,
            string? port, string? organizationName, bool useSsl, OrganizationDetail? organizationDetail)
        {
            Domain = domain;
            UserName = userName;
            Password = password;
            AuthenticationType = authenticationType;
            Server = server;
            Port = port;
            OrganizationName = organizationName;
            UseSsl = useSsl;
            OrganizationDetail = organizationDetail;
            InitializeComponent();
            AddColumns();
            ConfigureSelectAll();
            // ReSharper disable once UnusedVariable
            _ = new WindowResourceDictionary();
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
            $"Username = {UserName}; Password = {Password}; " +
            $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
            $"LoginPrompt = {PromptBehavior.Auto};";

        /// <summary>
        ///     (Immutable) identifier for the client.
        /// </summary>
        private const string ClientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";

        /// <summary>
        ///     (Immutable) URI of the redirect.
        /// </summary>
        private static readonly Uri RedirectUri = new("app://58145B91-0C36-4500-8554-080854F2AC97");

        /// <summary>
        ///     Gets the organization detail.
        /// </summary>
        /// <value>
        ///     The organization detail.
        /// </value>
        private OrganizationDetail? OrganizationDetail { get; }

        /// <summary>
        ///     Gets a value indicating whether [use SSL].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [use SSL]; otherwise, <c>false</c>.
        /// </value>
        private bool UseSsl { get; }

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
        ///     Gets the type of the authentication.
        /// </summary>
        /// <value>
        ///     The type of the authentication.
        /// </value>
        private AuthenticationType AuthenticationType { get; }

        /// <summary>
        ///     Gets the name of the organization.
        /// </summary>
        /// <value>
        ///     The name of the organization.
        /// </value>
        private string? OrganizationName { get; }

        /// <summary>
        ///     Gets URL of the application.
        /// </summary>
        /// <value>
        ///     The application URL.
        /// </value>
        private Uri? ApplicationUrl { get; }

        /// <summary>
        ///     Gets the secure string.
        /// </summary>
        /// <value>
        ///     The secure string.
        /// </value>
        private SecureString? SecureString { get; }

        /// <summary>
        ///     Gets the domain.
        /// </summary>
        /// <value>
        ///     The domain.
        /// </value>
        private string? Domain { get; }

        /// <summary>
        ///     Gets the name of the user.
        /// </summary>
        /// <value>
        ///     The name of the user.
        /// </value>
        private string? UserName { get; }

        /// <summary>
        ///     Gets the password.
        /// </summary>
        /// <value>
        ///     The password.
        /// </value>
        private string? Password { get; }

        /// <summary>
        ///     CRM Connection Manager.
        /// </summary>
        /// <value>
        ///     The crm connection manager.
        /// </value>
        private CrmConnectionManager? CrmConnectionManager { get; set; }

        /// <summary>
        ///     Gets or sets the schema names.
        /// </summary>
        /// <value>
        ///     The schema names.
        /// </value>
        private Collection<string?>? SchemaNames { get; set; }

        /// <summary>
        ///     Gets or sets the entity metadata.
        /// </summary>
        /// <value>
        ///     The entity metadata.
        /// </value>
        private IEnumerable<EntityMetadata>? EntityMetadata { get; set; }

        /// <summary>
        ///     Gets or sets the project.
        /// </summary>
        /// <value>
        ///     The project.
        /// </value>
        private static IEnumerable<Project>? Projects { get; set; }

        #endregion

        #region Events

        /// <summary>
        ///     Handles the Click event of the ButtonCancel control.
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
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Handles the Click event of the ButtonCreate control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <exception cref="CrmSvcUtilClientException">
        ///     Thrown when a Crm Svc Utility Client error condition occurs.
        /// </exception>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                try
                {
                    if (ListViewEntityToCreate.SelectedIndex == -1 && CheckBoxFilterEntities.IsChecked.HasValue && CheckBoxFilterEntities.IsChecked.Value)
                    {
                        MessageBox.Show("Please specify the entities to create.");
                        return;
                    }
                    if (IsNullOrEmpty(TextBoxNamespaceToCreate.Text))
                    {
                        MessageBox.Show("Please specify a valid namespace.");
                        return;
                    }
                    if (ComboProjects.SelectedIndex == -1)
                    {
                        if (IsNullOrEmpty(TextBoxNamespaceToCreate.Text))
                        {
                            MessageBox.Show("Please select a project.");
                            return;
                        }
                    }
                    using (CrmConnectionManager = new CrmConnectionManager())
                    {
                        if (AuthenticationType == AuthenticationType.OAuth)
                        {
                            CrmConnectionManager.CrmSvc = new CrmServiceClient(ConnectionString);
                        }
                        else
                        {
                            CrmConnectionManager.CrmSvc = new CrmServiceClient(
                                    new NetworkCredential(UserName, Password, Domain), AuthenticationType,
                                    Server, Port,
                                    OrganizationName, false, UseSsl, OrganizationDetail);
                        }
                        GenerateFile generateFile = new(ComboProjects.SelectedValue.ToString(),
                            CheckBoxCreateClassPerItem.IsChecked.HasValue && CheckBoxCreateClassPerItem.IsChecked.Value,
                            CheckBoxGenerateActions.IsChecked.HasValue && CheckBoxGenerateActions.IsChecked.Value,
                            CheckBoxGenerateGlobalOptionSets.IsChecked.HasValue && CheckBoxGenerateGlobalOptionSets.IsChecked.Value,
                            TextBoxServiceContextName.Text,
                            TextBoxNamespaceToCreate.Text,
                            CrmConnectionManager,
                            SchemaNames,
                            EntityMetadata,
                            Projects,
                            Domain,
                            UserName,
                            Password,
                            AuthenticationType,
                            CheckBoxFilterEntities.IsChecked.HasValue && CheckBoxFilterEntities.IsChecked.Value);
                        generateFile.Show();
                    }
                    Close();
                }
                catch (Exception exception)
                {
                    throw new CrmSvcUtilClientException(exception.Message, exception);
                }
            }
            catch (CrmSvcUtilClientException crmSvcUtilClientException)
            {
                MessageBox.Show(Format(CultureInfo.CurrentCulture, "An error occurred during execution: {0}",
                    crmSvcUtilClientException.Message));
                ButtonCancel.IsEnabled = true;
            }
        }

        /// <summary>
        ///     Handles the SelectionChanged event of the ListBoxEntityToCreate control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/>
        ///     instance containing the event data.
        /// </param>
        private void ListBoxEntityToCreate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewEntityToCreate.SelectedIndex == -1)
            {
                return;
            }
            SchemaNames = new Collection<string?>();
            foreach (ListViewMetadata listViewMetadata in ListViewEntityToCreate.SelectedItems)
            {
                string? schemaName = listViewMetadata.UniqueName;
                if (EntityMetadata != null && EntityMetadata.All(x => x.SchemaName != schemaName))
                {
                    return;
                }
                if (!SchemaNames.Contains(schemaName))
                {
                    SchemaNames.Add(schemaName);
                }
            }
            ButtonCreate.IsEnabled = ComboProjects.SelectedIndex != -1 &&
                                     !IsNullOrEmpty(TextBoxNamespaceToCreate.Text) &&
                                     (CheckBoxFilterEntities.IsChecked.HasValue &&
                                      !CheckBoxFilterEntities.IsChecked.Value ||
                                      SchemaNames.Any() &&
                                      CheckBoxFilterEntities.IsChecked.HasValue &&
                                      CheckBoxFilterEntities.IsChecked.Value);
        }

        /// <summary>
        ///     Handles the Loaded event of the ListBoxEntityToCreate control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <exception cref="CrmSvcUtilClientException">
        ///     Thrown when a Crm Svc Utility Client error condition occurs.
        /// </exception>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void ListBoxEntityToCreate_Loaded(object sender, RoutedEventArgs e)
        {
            using (CrmConnectionManager = new CrmConnectionManager())
            {
                if (AuthenticationType == AuthenticationType.OAuth)
                {
                    CrmConnectionManager.CrmSvc = new CrmServiceClient(ConnectionString);
                }
                else
                {
                    CrmConnectionManager.CrmSvc = new CrmServiceClient(
                            new NetworkCredential(UserName, Password, Domain), AuthenticationType,
                            Server, Port,
                            OrganizationName, false, UseSsl, OrganizationDetail);
                }
                if (!CrmConnectionManager.CrmSvc.IsReady)
                {
                    return;
                }
                GridView gridView = new();
                GridViewColumn friendlyNameGridViewColumn = new()
                {
                    Header = "Friendly Name",
                    Width = 138.5,
                    DisplayMemberBinding = new Binding("FriendlyName")
                };
                gridView.Columns.Add(friendlyNameGridViewColumn);
                GridViewColumn uniqueNameGridViewColumn = new()
                {
                    Header = "Unique Name",
                    Width = 138.5,
                    DisplayMemberBinding = new Binding("UniqueName")
                };
                gridView.Columns.Add(uniqueNameGridViewColumn);
                ListViewEntityToCreate.View = gridView;
                RetrieveAllEntitiesRequest request = new()
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                };
                RetrieveAllEntitiesResponse response;
                if (CrmConnectionManager.CrmSvc.OrganizationServiceProxy != null)
                {
                    response =
                                (RetrieveAllEntitiesResponse)CrmConnectionManager.CrmSvc.OrganizationServiceProxy.Execute(request);
                }
                else
                {
                    response =
                        (RetrieveAllEntitiesResponse)CrmConnectionManager.CrmSvc.OrganizationWebProxyClient.Execute(request);
                }
                EntityMetadata = response.EntityMetadata.Where(x => x.DisplayName.UserLocalizedLabel != null).OrderBy(x => x.LogicalName).ToList();
                foreach (EntityMetadata entityMetadata in EntityMetadata)
                {
                    ListViewEntityToCreate.Items.Add(new ListViewMetadata
                    {
                        UniqueName = entityMetadata.SchemaName,
                        FriendlyName = entityMetadata.DisplayName?.UserLocalizedLabel?.Label!
                    });
                }
                if (EntityMetadata.Count() != ListViewEntityToCreate.Items.Count)
                {
                    throw new CrmSvcUtilClientException("Mismatch between Entity Metadata count and Combo Box items count.");
                }
                for (int i = 0; i < EntityMetadata.Count(); i++)
                {
                    bool isEqual = false;
                    foreach (ListViewMetadata listItem in ListViewEntityToCreate.Items)
                    {
                        if (EntityMetadata.ToArray()[i].SchemaName.Equals(listItem.UniqueName))
                        {
                            isEqual = true;
                        }
                    }
                    if (!isEqual)
                    {
                        throw new CrmSvcUtilClientException("Mismatch between Entity Metadata index and Combo Box items index.");
                    }
                }
            }
        }

        /// <summary>
        ///     Handles the Loaded event of the ComboProjects control.
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        private void ComboProjects_Loaded(object sender, RoutedEventArgs e)
        {
            Projects = ProjectHelper.Projects;
            if (Projects == null)
            {
                return;
            }
            foreach (Project project in Projects)
            {
                ComboProjects.Items.Add(project.Name);
            }
        }

        /// <summary>
        ///     Handles the SelectionChanged event of the ComboProjects control.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/>
        ///     instance containing the event data.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        private void ComboProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboProjects.SelectedIndex == -1 || IsNullOrEmpty(ComboProjects.SelectedValue?.ToString()))
            {
                return;
            }
            if (Projects != null)
            {
                object value = Projects
                    .FirstOrDefault(project => project.Name == ComboProjects.SelectedValue?.ToString())?.Properties.Item("DefaultNamespace").Value!;
                TextBoxNamespaceToCreate.Text = (string)value;
            }
            if (SchemaNames != null)
            {
                ButtonCreate.IsEnabled = ComboProjects.SelectedIndex != -1 &&
                                      !IsNullOrEmpty(TextBoxNamespaceToCreate.Text) &&
                                      (CheckBoxFilterEntities.IsChecked.HasValue &&
                                       !CheckBoxFilterEntities.IsChecked.Value ||
                                       SchemaNames.Any() &&
                                       CheckBoxFilterEntities.IsChecked.HasValue &&
                                       CheckBoxFilterEntities.IsChecked.Value);
            }
            else
            {
                ButtonCreate.IsEnabled = ComboProjects.SelectedIndex != -1 &&
                                         !IsNullOrEmpty(TextBoxNamespaceToCreate.Text) &&
                                         (CheckBoxFilterEntities.IsChecked.HasValue &&
                                          !CheckBoxFilterEntities.IsChecked.Value);
            }
        }

        /// <summary>
        ///     Event handler. Called by CheckBoxFilterEntities for checked events.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 01/04/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     Routed event information.
        /// </param>
        private void CheckBoxFilterEntities_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxFilterEntities.IsChecked.HasValue && CheckBoxFilterEntities.IsChecked.Value)
            {
                LabelTablesToCreate.Visibility = Visibility.Visible;
                ListViewEntityToCreate.Visibility = Visibility.Visible;
                if (SchemaNames != null)
                {
                    ButtonCreate.IsEnabled = !IsNullOrEmpty(TextBoxNamespaceToCreate.Text) && SchemaNames.Any();
                }
            }
            else
            {
                LabelTablesToCreate.Visibility = Visibility.Hidden;
                ListViewEntityToCreate.Visibility = Visibility.Hidden;
                if (SchemaNames != null)
                {
                    ButtonCreate.IsEnabled = ComboProjects.SelectedIndex != -1 &&
                                          !IsNullOrEmpty(TextBoxNamespaceToCreate.Text) &&
                                          (CheckBoxFilterEntities.IsChecked.HasValue &&
                                           !CheckBoxFilterEntities.IsChecked.Value ||
                                           SchemaNames.Any() &&
                                           CheckBoxFilterEntities.IsChecked.HasValue &&
                                           CheckBoxFilterEntities.IsChecked.Value);
                }
                else
                {
                    ButtonCreate.IsEnabled = ComboProjects.SelectedIndex != -1 &&
                                             !IsNullOrEmpty(TextBoxNamespaceToCreate.Text) &&
                                             (CheckBoxFilterEntities.IsChecked.HasValue &&
                                              !CheckBoxFilterEntities.IsChecked.Value);
                }
            }
        }

        /// <summary>
        ///     Event handler. Called by CheckBoxFilterEntities for unchecked events.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 01/04/2022.
        /// </remarks>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     Routed event information.
        /// </param>
        private void CheckBoxFilterEntities_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxFilterEntities.IsChecked.HasValue && CheckBoxFilterEntities.IsChecked.Value)
            {
                LabelTablesToCreate.Visibility = Visibility.Visible;
                ListViewEntityToCreate.Visibility = Visibility.Visible;
                if (SchemaNames != null)
                {
                    ButtonCreate.IsEnabled = !IsNullOrEmpty(TextBoxNamespaceToCreate.Text) && SchemaNames.Any();
                }
            }
            else
            {
                LabelTablesToCreate.Visibility = Visibility.Hidden;
                ListViewEntityToCreate.Visibility = Visibility.Hidden;
                if (SchemaNames != null)
                {
                    ButtonCreate.IsEnabled = ComboProjects.SelectedIndex != -1 &&
                                          !IsNullOrEmpty(TextBoxNamespaceToCreate.Text) &&
                                          (CheckBoxFilterEntities.IsChecked.HasValue &&
                                           !CheckBoxFilterEntities.IsChecked.Value ||
                                           SchemaNames.Any() &&
                                           CheckBoxFilterEntities.IsChecked.HasValue &&
                                           CheckBoxFilterEntities.IsChecked.Value);
                }
                else
                {
                    ButtonCreate.IsEnabled = ComboProjects.SelectedIndex != -1 &&
                                             !IsNullOrEmpty(TextBoxNamespaceToCreate.Text) &&
                                             (CheckBoxFilterEntities.IsChecked.HasValue &&
                                              !CheckBoxFilterEntities.IsChecked.Value);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Adds the columns.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        private void AddColumns()
        {
            GridView gridView = new();
            GridViewColumn friendlyNameGridViewColumn = new()
            {
                Header = "Friendly Name",
                Width = 123
            };
            gridView.Columns.Add(friendlyNameGridViewColumn);
            GridViewColumn uniqueNameGridViewColumn = new()
            {
                Header = "Unique Name",
                Width = 123
            };
            gridView.Columns.Add(uniqueNameGridViewColumn);
            ListViewEntityToCreate.View = gridView;
        }

        /// <summary>
        ///     Configures the select all.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        private void ConfigureSelectAll()
        {
            ListViewEntityToCreate.InputBindings.Add(new KeyBinding(ApplicationCommands.SelectAll,
                new KeyGesture(Key.A, ModifierKeys.Control)));
            ListViewEntityToCreate.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll,
                (_, _) => { ListViewEntityToCreate.SelectAll(); }));
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
            CrmConnectionManager?.Dispose();
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
        ///     Finalizes an instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        ~MainWindow()
        {
            Dispose(false);
        }

        #endregion
    }
}
