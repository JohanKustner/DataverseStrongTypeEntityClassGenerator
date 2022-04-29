#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using CrmSvcUtil.Client.Exceptions;
using EnvDTE;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Microsoft.Xrm.Tooling.CrmConnectControl;
using AuthenticationType = Microsoft.Xrm.Tooling.Connector.AuthenticationType;
using Process = System.Diagnostics.Process;

namespace CrmSvcUtil.Client
{
    /// <summary>
    ///     A generate file.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 17/03/2022.
    /// </remarks>
    /// <seealso cref="EnvDTE.Window"/>
    /// <seealso cref="System.Windows.Markup.IComponentConnector"/>
    public partial class GenerateFile
    {
        #region Constructors

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="comboProjectsSelectedValue">
        ///     The combo projects selected value.
        /// </param>
        /// <param name="createClassPerItem">
        ///     True to create class per item.
        /// </param>
        /// <param name="generateActions">
        ///     True to generate actions.
        /// </param>
        /// <param name="generateGlobalOptionSets">
        ///     .
        /// </param>
        /// <param name="serviceContextName">
        ///     Name of the service context.
        /// </param>
        /// <param name="namespaceToCreate">
        ///     The namespace to create.
        /// </param>
        /// <param name="crmConnectionManager">
        ///     Manager for crm connection.
        /// </param>
        /// <param name="schemaNames">
        ///     A list of names of the schemas.
        /// </param>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="projects">
        ///     The projects.
        /// </param>
        /// <param name="domain">
        ///     The domain.
        /// </param>
        /// <param name="userName">
        ///     The name of the user.
        /// </param>
        /// <param name="password">
        ///     The password.
        /// </param>
        /// <param name="authenticationType">
        ///     .
        /// </param>
        /// <param name="filterEntities">
        ///     .
        /// </param>
        [CLSCompliant(false)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        public GenerateFile(string? comboProjectsSelectedValue, bool createClassPerItem, bool generateActions,
            bool generateGlobalOptionSets,
            string serviceContextName,
            string namespaceToCreate, CrmConnectionManager? crmConnectionManager,
            Collection<string?>? schemaNames, IEnumerable<EntityMetadata>? entityMetadata, IEnumerable<Project>? projects,
            string? domain, string? userName, string? password, AuthenticationType authenticationType, bool filterEntities)
        {
            Domain = domain;
            UserName = userName;
            Password = password;
            Projects = projects;
            EntityMetadata = entityMetadata;
            SchemaNames = schemaNames;
            AuthenticationType = authenticationType;
            ServiceContextName = serviceContextName;
            InitializeComponent();
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            if (string.IsNullOrEmpty(assemblyPath))
            {
                MessageBox.Show("Something went wrong and the assembly path was not found.");
                return;
            }
            CopyFiles(assemblyPath, comboProjectsSelectedValue);
            InvokeServiceUtilityDelegate invokeServiceUtilityDelegate = InvokeServiceUtility;
            ProjectSelectedValue = comboProjectsSelectedValue;
            if (EntityMetadata == null)
            {
                return;
            }
            string entityFilter = (filterEntities ? string.Join(";", EntityMetadata.Where(x => SchemaNames != null && SchemaNames.Contains(x.SchemaName)).Select(x => x.LogicalName)) : null)!;
            if (Projects == null || crmConnectionManager == null)
            {
                return;
            }
            if (ProjectSelectedValue != null)
            {
                invokeServiceUtilityDelegate.BeginInvoke(GetArguments(entityFilter,
                        serviceContextName, ProjectPath, FileName, namespaceToCreate, crmConnectionManager,
                        createClassPerItem, generateActions, generateGlobalOptionSets, filterEntities),
                    Path.Combine(TempDir, "CrmSvcUtil.exe"),
                    Projects.FirstOrDefault(x => x.Name == ProjectSelectedValue)!,
                    ProjectSelectedValue, ProjectPath, crmConnectionManager, InvokeServiceUtilityCallback,
                    ProjectSelectedValue);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the name of the service context.
        /// </summary>
        /// <value>
        ///     The name of the service context.
        /// </value>
        private string ServiceContextName { get; }

        /// <summary>
        ///     Gets the connection string.
        /// </summary>
        /// <value>
        ///     The connection string.
        /// </value>
        private string ConnectionString
        {
            get
            {
                return AuthenticationType switch
                {
                    AuthenticationType.AD => $"AuthType={AuthenticationType}; Domain = {Domain}; " +
                                             $"Username = {UserName}; Password = {Password}; " +
                                             $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                                             $"LoginPrompt = {PromptBehavior.Auto};",
                    AuthenticationType.OAuth => $"AuthType={AuthenticationType}; ClientId = {ClientId}; " +
                                                $"Username = {UserName}; Password = {Password}; " +
                                                $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                                                $"LoginPrompt = {PromptBehavior.Auto};",
                    AuthenticationType.Live => $"AuthType={AuthenticationType.OAuth}; ClientId = {ClientId}; " +
                                               $"Username = {UserName}; Password = {Password}; " +
                                               $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                                               $"LoginPrompt = {PromptBehavior.Auto};",
                    AuthenticationType.IFD => $"AuthType={AuthenticationType.OAuth}; ClientId = {ClientId}; " +
                                              $"Username = {UserName}; Password = {Password}; " +
                                              $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                                              $"LoginPrompt = {PromptBehavior.Auto};",
                    AuthenticationType.Claims => $"AuthType={AuthenticationType.OAuth}; ClientId = {ClientId}; " +
                                                 $"Username = {UserName}; Password = {Password}; " +
                                                 $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                                                 $"LoginPrompt = {PromptBehavior.Auto};",
                    AuthenticationType.Office365 => $"AuthType={AuthenticationType.OAuth}; ClientId = {ClientId}; " +
                                                    $"Username = {UserName}; Password = {Password}; " +
                                                    $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                                                    $"LoginPrompt = {PromptBehavior.Auto};",
                    AuthenticationType.Certificate => $"AuthType={AuthenticationType.OAuth}; ClientId = {ClientId}; " +
                                                      $"Username = {UserName}; Password = {Password}; " +
                                                      $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                                                      $"LoginPrompt = {PromptBehavior.Auto};",
                    AuthenticationType.ClientSecret => $"AuthType={AuthenticationType.OAuth}; ClientId = {ClientId}; " +
                                                       $"Username = {UserName}; Password = {Password}; " +
                                                       $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                                                       $"LoginPrompt = {PromptBehavior.Auto};",
                    AuthenticationType.ExternalTokenManagement =>
                        $"AuthType={AuthenticationType.OAuth}; ClientId = {ClientId}; " +
                        $"Username = {UserName}; Password = {Password}; " +
                        $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                        $"LoginPrompt = {PromptBehavior.Auto};",
                    AuthenticationType.InvalidConnection =>
                        $"AuthType={AuthenticationType.OAuth}; ClientId = {ClientId}; " +
                        $"Username = {UserName}; Password = {Password}; " +
                        $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                        $"LoginPrompt = {PromptBehavior.Auto};",
                    _ => $"AuthType={AuthenticationType.OAuth}; ClientId = {ClientId}; " +
                         $"Username = {UserName}; Password = {Password}; " +
                         $"Url = {ApplicationUrl}; RedirectUri = {RedirectUri}; " +
                         $"LoginPrompt = {PromptBehavior.Auto};"
                };
            }
        }

        /// <summary>
        ///     (Immutable) identifier for the client.
        /// </summary>
        private const string ClientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";

        /// <summary>
        ///     (Immutable) URI of the redirect.
        /// </summary>
        private static readonly Uri RedirectUri = new("app://58145B91-0C36-4500-8554-080854F2AC97");

        /// <summary>
        ///     Gets URL of the application.
        /// </summary>
        /// <value>
        ///     The application URL.
        /// </value>
        private Uri? ApplicationUrl { get; set; }

        /// <summary>
        ///     Gets the type of the authentication.
        /// </summary>
        /// <value>
        ///     The type of the authentication.
        /// </value>
        private AuthenticationType AuthenticationType { get; }

        /// <summary>
        ///     The customization prefixes.
        /// </summary>
        private static Collection<string> CustomizationPrefixes => new();

        /// <summary>
        ///     Gets the temporary dir.
        /// </summary>
        /// <value>
        ///     The temporary dir.
        /// </value>
        private static string TempDir
        {
            get
            {
                _tempDir = Path.Combine(ProjectPath, "CrmSvcUtilExtender");
                if (!Directory.Exists(_tempDir))
                {
                    Directory.CreateDirectory(_tempDir);
                }
                return _tempDir;
            }
        }

        /// <summary>
        ///     Gets the full pathname of the project file.
        /// </summary>
        /// <value>
        ///     The full pathname of the project file.
        /// </value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        private static string ProjectPath
        {
            get
            {
                if (Projects != null && Projects.Any(x => x.Name == ProjectSelectedValue))
                {
                    return
                        Path.GetDirectoryName(
                            Projects.FirstOrDefault(x => x.Name == ProjectSelectedValue)?.FileName)!;
                }
                return string.Empty;
            }
        }

        /// <summary>
        ///     Gets or sets the projects.
        /// </summary>
        /// <value>
        ///     The projects.
        /// </value>
        private static IEnumerable<Project>? Projects { get; set; }

        /// <summary>
        ///     Gets a list of names of the schemas.
        /// </summary>
        /// <value>
        ///     A list of names of the schemas.
        /// </value>
        private Collection<string?>? SchemaNames { get; }

        /// <summary>
        ///     Gets the entity metadata.
        /// </summary>
        /// <value>
        ///     The entity metadata.
        /// </value>
        private IEnumerable<EntityMetadata>? EntityMetadata { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether this object is first item.
        /// </summary>
        /// <value>
        ///     True if this object is first item, false if not.
        /// </value>
        private bool IsFirstItem { get; set; }

        /// <summary>
        ///     Gets the filename of the file.
        /// </summary>
        /// <value>
        ///     The name of the file.
        /// </value>
        private string? FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                {
                    _fileName = "Entities.cs";
                }
                return _fileName;
            }
        }

        /// <summary>
        ///     Gets or sets the project selected value.
        /// </summary>
        /// <value>
        ///     The project selected value.
        /// </value>
        private static string? ProjectSelectedValue { get; set; }

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

        #endregion

        #region Fields

        /// <summary>
        ///     The temporary dir.
        /// </summary>
        private static string? _tempDir;

        /// <summary>
        ///     Filename of the file.
        /// </summary>
        private string? _fileName;

        #endregion

        #region Methods

        /// <summary>
        ///     Gets the arguments.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="entityFilter">
        ///     A filter specifying the entity.
        /// </param>
        /// <param name="serviceContextName">
        ///     Name of the service context.
        /// </param>
        /// <param name="projectPath">
        ///     Full pathname of the project file.
        /// </param>
        /// <param name="fileName">
        ///     Filename of the file.
        /// </param>
        /// <param name="namespaceToCreate">
        ///     The namespace to create.
        /// </param>
        /// <param name="crmConnectionManager">
        ///     Manager for crm connection.
        /// </param>
        /// <param name="createClassPerItem">
        ///     True to create class per item.
        /// </param>
        /// <param name="generateActions">
        ///     True to generate actions.
        /// </param>
        /// <param name="generateGlobalOptionSets">
        ///     True to generate global option sets.
        /// </param>
        /// <param name="filterEntities">
        ///     True to filter entities.
        /// </param>
        /// <returns>
        ///     The arguments.
        /// </returns>
        private string GetArguments(string entityFilter, string serviceContextName, string projectPath, string? fileName,
            string namespaceToCreate, CrmConnectionManager? crmConnectionManager, bool createClassPerItem, bool generateActions,
            bool generateGlobalOptionSets, bool filterEntities)
        {
            StringBuilder stringBuilder = new();
            const string str = "/{0}:\"{1}\" ";
            object[] codeCustomization = { "codeCustomization", "CrmDeveloperToolkitExtender.CrmSvcUtil.CodeCustomizationService, CrmDeveloperToolkitExtender.CrmSvcUtil" };
            stringBuilder.Append(string.Format(CultureInfo.CurrentCulture, str, codeCustomization));
            object[] namingService = { "namingService", "CrmDeveloperToolkitExtender.CrmSvcUtil.NamingService, CrmDeveloperToolkitExtender.CrmSvcUtil" };
            stringBuilder.Append(string.Format(CultureInfo.CurrentCulture, str, namingService));
            if (generateActions)
            {
                stringBuilder.Append("/generateActions ");
            }
            if (generateGlobalOptionSets)
            {
                stringBuilder.Append("/generateGlobalOptionSets ");
            }
            if (!string.IsNullOrEmpty(serviceContextName))
            {
                object[] serviceContextNameArray = { "serviceContextName", serviceContextName };
                stringBuilder.Append(string.Format(CultureInfo.CurrentCulture, str, serviceContextNameArray));
            }
            if (!string.IsNullOrEmpty(fileName) && !createClassPerItem)
            {
                object[] outArray = { "out", Path.Combine(projectPath, fileName) };
                stringBuilder.Append(string.Format(CultureInfo.CurrentCulture, str, outArray));
            }
            if (createClassPerItem)
            {
                object[] outDirectoryArray = { "outdirectory", projectPath };
                stringBuilder.Append(string.Format(CultureInfo.CurrentCulture, str, outDirectoryArray));
                stringBuilder.Append("/splitfiles ");
            }
            if (!string.IsNullOrEmpty(namespaceToCreate))
            {
                object[] namespaceArray = { "namespace", namespaceToCreate };
                stringBuilder.Append(string.Format(CultureInfo.CurrentCulture, str, namespaceArray));
            }

            if (crmConnectionManager?.CrmSvc.CrmConnectOrgUriActual.AbsoluteUri != null)
            {
                ApplicationUrl = new Uri(crmConnectionManager.CrmSvc.CrmConnectOrgUriActual.AbsoluteUri);
            }
            object[] connectionStringArray = { "connectionstring", ConnectionString };
            stringBuilder.Append(string.Format(CultureInfo.CurrentCulture, str, connectionStringArray));
            if (!string.IsNullOrEmpty(entityFilter) && filterEntities)
            {
                object[] messageNamesFilterArray = { "entitynamesfilter", entityFilter };
                stringBuilder.Append(string.Format(CultureInfo.CurrentCulture, str, messageNamesFilterArray));
            }
            ConfigHelper configHelper = new(Path.Combine(TempDir, "CrmSvcUtil.exe.config"));
            configHelper.AddKeyValuePairToConfig("useDisplayName", true.ToString(CultureInfo.CurrentCulture));
            configHelper.AddKeyValuePairToConfig("projectPath", projectPath);
            configHelper.AddKeyValuePairToConfig("createClassPerItem", createClassPerItem.ToString());

            return stringBuilder.ToString();
        }

        /// <summary>
        ///     Clean up.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        private static void CleanUp()
        {
            if (!Directory.Exists(TempDir))
            {
                return;
            }
            foreach (string file in Directory.GetFiles(TempDir))
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            Directory.Delete(TempDir);
        }

        /// <summary>
        ///     Async callback, called on completion of invoke service utility callback.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="asyncResult">
        ///     The result of the asynchronous operation.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs", Justification = "<Pending>")]
        private void InvokeServiceUtilityCallback(IAsyncResult asyncResult)
        {
            ProjectSelectedValue = (asyncResult.AsyncState as string)!;
            CleanUp();
            Dispatcher?.Invoke(DispatcherPriority.Normal, new Action(Close));
        }

        /// <summary>
        ///     Executes the service utility delegate on a different thread, and waits for the result.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="arguments">
        ///     The arguments.
        /// </param>
        /// <param name="crmSvcUtilFileName">
        ///     Filename of the crm svc utility file.
        /// </param>
        /// <param name="project">
        ///     The project.
        /// </param>
        /// <param name="projectName">
        ///     Name of the project.
        /// </param>
        /// <param name="projectPath">
        ///     Full pathname of the project file.
        /// </param>
        /// <param name="crmConnectionManager">
        ///     Filename of the file.
        /// </param>
        private delegate void InvokeServiceUtilityDelegate(string arguments, string crmSvcUtilFileName, Project project, string projectName, string projectPath, CrmConnectionManager crmConnectionManager);

        /// <summary>
        ///     Executes the service utility on a different thread, and waits for the result.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <exception cref="CrmSvcUtilClientException">
        ///     Thrown when a Crm Svc Utility Client error condition occurs.
        /// </exception>
        /// <param name="arguments">
        ///     The arguments.
        /// </param>
        /// <param name="crmSvcUtilFileName">
        ///     Filename of the crm svc utility file.
        /// </param>
        /// <param name="project">
        ///     The project.
        /// </param>
        /// <param name="projectName">
        ///     Name of the project.
        /// </param>
        /// <param name="projectPath">
        ///     Full pathname of the project file.
        /// </param>
        /// <param name="crmConnectionManager">
        ///     Manager for crm connection.
        /// </param>
        private void InvokeServiceUtility(string arguments, string crmSvcUtilFileName, Project project, string projectName, string projectPath, CrmConnectionManager crmConnectionManager)
        {
            try
            {
                try
                {
                    using (Process process = new())
                    {
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.FileName = crmSvcUtilFileName;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.Arguments = arguments;
                        process.Start();
                        string standardError = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        if (!string.IsNullOrEmpty(standardError))
                        {
                            throw new CrmSvcUtilClientException(standardError);
                        }
                    }
                    AddFilesToProjects(project, projectPath, crmConnectionManager);
                }
                catch (Exception exception)
                {
                    throw new CrmSvcUtilClientException(exception.Message, exception);
                }
            }
            catch (CrmSvcUtilClientException crmSvcUtilClientException)
            {
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "An error occurred during execution: {0}",
                    crmSvcUtilClientException.Message));
            }
        }

        /// <summary>
        ///     Copies the files.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="assemblyPath">
        ///     Full pathname of the assembly file.
        /// </param>
        /// <param name="comboProjectsSelectedValue">
        ///     The combo projects selected value.
        /// </param>
        private static void CopyFiles(string assemblyPath, string? comboProjectsSelectedValue)
        {
            ProjectSelectedValue = comboProjectsSelectedValue;
            CleanUp();
            foreach (string file in Directory.GetFiles(Path.Combine(assemblyPath, "CrmSvcUtilFiles")))
            {
                FileInfo fileInfo = new(file);
                if (!fileInfo.Exists)
                {
                    continue;
                }
                if (TempDir != null)
                {
                    File.Copy(file,
                        Path.Combine(TempDir, fileInfo.Name));
                }
            }
            if (TempDir != null)
            {
                File.Copy(Path.Combine(assemblyPath, "CrmDeveloperToolkitExtender.CrmSvcUtil.dll"),
                    Path.Combine(TempDir, "CrmDeveloperToolkitExtender.CrmSvcUtil.dll"));
            }
        }

        /// <summary>
        ///     Adds the files to projects to 'projectPath'.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="project">
        ///     The project.
        /// </param>
        /// <param name="projectPath">
        ///     Full pathname of the project file.
        /// </param>
        /// <param name="crmConnectionManager">
        ///     Manager for crm connection.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        private void AddFilesToProjects(Project project, string projectPath, CrmConnectionManager crmConnectionManager)
        {
            IsFirstItem = true;
            DirectoryInfo directoryInfo = new(projectPath);
            foreach (FileInfo fileInfo in directoryInfo.GetFiles())
            {
                if (fileInfo.Name != FileName &&
                    fileInfo.Name != string.Format(CultureInfo.CurrentCulture, "{0}.cs", ServiceContextName) &&
                    fileInfo.Name != "EntityOptionSetEnum.cs")
                {
                    continue;
                }
                AddFileToProject(project, fileInfo, crmConnectionManager);
                IsFirstItem = false;
            }
            foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
            {
                if (directory.Name != "Entities" && directory.Name != "Messages" &&
                    directory.Name != "OptionSets")
                {
                    continue;
                }
                foreach (FileInfo fileInfo in directory.GetFiles())
                {
                    AddFileToProject(project, fileInfo, crmConnectionManager);
                }
            }
            project.Save();
        }

        /// <summary>
        ///     Adds a file to project to 'filePath'.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 28/03/2022.
        /// </remarks>
        /// <param name="project">
        ///     The project.
        /// </param>
        /// <param name="fileInfo">
        ///     Full pathname of the file.
        /// </param>
        /// <param name="crmConnectionManager">
        ///     Manager for crm connection.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        private void AddFileToProject(Project project, FileSystemInfo fileInfo, CrmConnectionManager crmConnectionManager)
        {
            if (!File.Exists(fileInfo.FullName))
            {
                return;
            }
            if (fileInfo.Name != FileName && fileInfo.Name != string.Format(CultureInfo.CurrentCulture, "{0}.cs", ServiceContextName) &&
                fileInfo.Name != "EntityOptionSetEnum.cs" && !IsFirstItem)
            {
                string fileContents = File.ReadAllText(fileInfo.FullName);
                if (fileContents.Contains("[assembly: Microsoft.Xrm.Sdk.Client.ProxyTypesAssemblyAttribute()]"))
                {
                    fileContents =
                        fileContents.Replace("[assembly: Microsoft.Xrm.Sdk.Client.ProxyTypesAssemblyAttribute()]",
                            string.Empty);
                    File.AppendAllText(fileInfo.FullName, fileContents);
                }
            }
            string directoryName = Path.GetDirectoryName(fileInfo.FullName)!;
            string fileName = fileInfo.Name.Replace(fileInfo.Extension, string.Empty);
            string fileNameFixed = RemovePublisherCustomizationPrefix(crmConnectionManager, fileName);
            string destFileName = Path.Combine(directoryName, string.Format(CultureInfo.CurrentCulture, "{0}.{1}", fileNameFixed, fileInfo.Extension));
            if (fileName != fileNameFixed)
            {
                if (File.Exists(destFileName))
                {
                    File.Delete(destFileName);
                }
                File.Move(fileInfo.FullName, destFileName);
                FileInfo destFileInfo = new(destFileName);
                if (project.ProjectItems.Cast<ProjectItem>().All(projectItem => projectItem.Name != destFileInfo.Name))
                {
                    project.ProjectItems.AddFromFile(destFileName);
                }
            }
            else
            {
                if (project.ProjectItems.Cast<ProjectItem>().All(projectItem => projectItem.Name != fileInfo.Name))
                {
                    project.ProjectItems.AddFromFile(fileInfo.FullName);
                }
            }
        }

        /// <summary>
        ///     Removes the publisher customization prefix.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="crmConnectionManager">
        ///     Manager for crm connection.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private static string RemovePublisherCustomizationPrefix(CrmConnectionManager crmConnectionManager, string name)
        {
            if (CustomizationPrefixes.Count == 0 && crmConnectionManager.CrmSvc.IsReady)
            {
                if (crmConnectionManager.CrmSvc?.OrganizationWebProxyClient != null)
                {
                    using OrganizationWebProxyClient service = crmConnectionManager.CrmSvc.OrganizationWebProxyClient;
                    GetPublishers(service);
                }
                else
                {
                    if (crmConnectionManager.CrmSvc?.OrganizationServiceProxy != null)
                    {
                        using OrganizationServiceProxy service = crmConnectionManager.CrmSvc.OrganizationServiceProxy;
                        GetPublishers(service);
                    }
                }
            }
            name = name.Contains("_") ? CustomizationPrefixes.Aggregate(name, (current, prefix) => current.Replace(prefix, string.Empty)) : name;
            string tempName = name.Split('_').Where(word => !string.IsNullOrEmpty(word)).Aggregate(string.Empty, (current, word) => current + (string.Format(CultureInfo.CurrentCulture, "{0}{1}", word.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture), word.Substring(1, word.Length - 1))));
            name = tempName;
            return name;
        }

        /// <summary>   Gets the entities.  </summary>
        /// <remarks>   Johan Küstner, 17/03/2022.  </remarks>
        /// <param name="service">
        ///     The service.
        /// </param>
        private static void GetPublishers(IOrganizationService service)
        {
            QueryExpression query = new("publisher")
            {
                ColumnSet = new ColumnSet(true)
            };
            DataCollection<Entity> dataCollection = service.RetrieveMultiple(query).Entities!;
            if (dataCollection == null)
            {
                return;
            }
            foreach (Entity entity in dataCollection)
            {
                string customizationPrefix = entity["customizationprefix"].ToString();
                if (!CustomizationPrefixes.Contains(customizationPrefix))
                {
                    CustomizationPrefixes.Add(string.Format(CultureInfo.CurrentCulture, "{0}_",
                        customizationPrefix));
                }
            }
        }
        #endregion
    }
}
