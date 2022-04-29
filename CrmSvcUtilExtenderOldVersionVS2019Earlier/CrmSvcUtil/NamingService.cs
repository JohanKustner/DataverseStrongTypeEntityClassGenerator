#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Crm.Services.Utility;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Microsoft.Xrm.Tooling.Connector;

namespace CrmDeveloperToolkitExtender.CrmSvcUtil
{
    /// <summary>
    ///     A service for accessing namings information. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 17/03/2022.
    /// </remarks>
    /// <seealso cref="INamingService"/>
    // ReSharper disable once UnusedType.Global
    public sealed class NamingService : INamingService
    {
        #region Properties and Fields

        /// <summary>
        ///     Gets the default naming service.
        /// </summary>
        /// <value>
        ///     The default naming service.
        /// </value>
        private INamingService DefaultNamingService { get; }

        /// <summary>
        ///     (Immutable) list of names of the options.
        /// </summary>
        private readonly Dictionary<OptionSetMetadataBase, Dictionary<string, int>> _optionNames;

        /// <summary>
        ///     The metadata.
        /// </summary>
        private IOrganizationMetadata? _metadata;

        /// <summary>
        ///     The metadata provider service.
        /// </summary>
        private IMetadataProviderService? _metadataProviderService;

        /// <summary>
        ///     (Immutable) true to use display name.
        /// </summary>
        private readonly bool _useDisplayName;

        /// <summary>
        ///     The customization prefixes.
        /// </summary>
        private static Collection<string>? _customizationPrefixes;

        /// <summary>
        ///     (Immutable) list of names of the entities.
        /// </summary>
        private readonly Dictionary<Guid, string> _entityNames;

        /// <summary>
        ///     (Immutable) list of names of the entity sets.
        /// </summary>
        private readonly Dictionary<Guid, string> _entitySetNames;

        /// <summary>
        ///     (Immutable) list of names of the attributes.
        /// </summary>
        private readonly Collection<PropertyName> _attributeNames;

        /// <summary>
        ///     (Immutable) list of names of the option sets.
        /// </summary>
        private readonly Collection<PropertyName> _optionSetNames;

        /// <summary>
        ///     (Immutable) list of names of the relationships.
        /// </summary>
        private readonly Collection<PropertyName> _relationshipNames;

        #endregion

        #region Constructors

        // <summary>

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="namingService">
        ///     The naming service.
        /// </param>
        public NamingService(INamingService namingService)
        {
            DefaultNamingService = namingService;
            _optionNames = new Dictionary<OptionSetMetadataBase, Dictionary<string, int>>();
            _useDisplayName = bool.TryParse(ConfigurationManager.AppSettings["useDisplayName"], out _useDisplayName) && bool.Parse(ConfigurationManager.AppSettings["useDisplayName"]);
            _customizationPrefixes = new Collection<string>();
            _entityNames = new Dictionary<Guid, string>();
            _entitySetNames = new Dictionary<Guid, string>();
            _attributeNames = new Collection<PropertyName>();
            _optionSetNames = new Collection<PropertyName>();
            _relationshipNames = new Collection<PropertyName>();
        }

        #endregion

        #region INamingService Methods

        /// <summary>
        ///     Returns a name for the OptionSet being generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for option set.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForOptionSet(EntityMetadata,OptionSetMetadataBase,IServiceProvider)"/>
        public string GetNameForOptionSet(EntityMetadata entityMetadata, OptionSetMetadataBase optionSetMetadata, IServiceProvider services)
        {
            string nameForOptionSet;
            if (_useDisplayName)
            {
                // Return option set name if no label is found.
                if (string.IsNullOrEmpty(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label))
                {
                    nameForOptionSet = GetOptionSetName(optionSetMetadata.Name, services, entityMetadata, optionSetMetadata);
                    nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
                    return nameForOptionSet;
                }
                // Return default name for option set.
                nameForOptionSet =
                    EnsureValidIdentifier(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label!);
                nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
                // Return option set name if no option set type is specified.
                if (!optionSetMetadata.OptionSetType.HasValue)
                {
                    return nameForOptionSet;
                }
                // Check option set type if there is entity metadata.
                switch (optionSetMetadata.OptionSetType.Value)
                {
                    // There is a mismatch of display name for state or status fields when they belong to an activity type entity.
                    // Therefore, we force the explicit naming of state and status fields instead of determining it from the display name.
                    case OptionSetType.State:
                        // Return name for status attribute.
                        nameForOptionSet =
                            optionSetMetadata.Name.ToLower(CultureInfo.CurrentCulture).Equals("statecode")
                                ? string.Format(CultureInfo.CurrentCulture, "{0}StatusCode",
                                    GetNameForEntity(entityMetadata, services))
                                : GetOptionSetName(optionSetMetadata.Name, services, entityMetadata, optionSetMetadata);
                        nameForOptionSet = nameForOptionSet.Equals("Status")
                            ? string.Format(CultureInfo.CurrentCulture, "{0}StatusCode",
                                GetNameForEntity(entityMetadata, services))
                            : nameForOptionSet;
                        nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
                        return nameForOptionSet;
                    case OptionSetType.Status:
                        // Return name for status reason attribute.
                        nameForOptionSet =
                            optionSetMetadata.Name.ToLower(CultureInfo.CurrentCulture).Equals("statuscode")
                                ? string.Format(CultureInfo.CurrentCulture, "{0}StatusReasonCode",
                                    GetNameForEntity(entityMetadata, services))
                                : GetOptionSetName(optionSetMetadata.Name, services, entityMetadata, optionSetMetadata);
                        nameForOptionSet =
                            nameForOptionSet.Equals("StatusReason")
                                ? string.Format(CultureInfo.CurrentCulture, "{0}StatusReasonCode",
                                    GetNameForEntity(entityMetadata, services))
                                : nameForOptionSet;
                        nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
                        return nameForOptionSet;
                    case OptionSetType.Picklist:
                        // Break to get pick list attribute name.
                        break;
                    case OptionSetType.Boolean:
                        // Return name for Boolean attribute.
                        nameForOptionSet = EnsureValidIdentifier(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label!);
                        nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
                        return nameForOptionSet;
                    default:
                        // Return default option set name.
                        nameForOptionSet = EnsureValidIdentifier(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label!);
                        nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
                        return nameForOptionSet;
                }
                // Return global option set name.
                if (optionSetMetadata.IsGlobal.HasValue && optionSetMetadata.IsGlobal.Value)
                {
                    nameForOptionSet = EnsureValidIdentifier(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label!);
                    nameForOptionSet = CheckEnumNotSameAsEntity(services, nameForOptionSet);
                    nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
                    return nameForOptionSet;
                }
                // Return option set name if not a global option set.
                if (optionSetMetadata.IsGlobal == null)
                {
                    nameForOptionSet = EnsureValidIdentifier(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label!);
                    nameForOptionSet = CheckEnumNotSameAsEntity(services, nameForOptionSet);
                    nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
                    return nameForOptionSet;
                }
                // Interrogate entity and attribute metadata to get option set name.
                AttributeMetadata? attribute = (from a in entityMetadata.Attributes
                                                where a.AttributeType == AttributeTypeCode.Picklist
                                                      && ((EnumAttributeMetadata)a).OptionSet.MetadataId == optionSetMetadata.MetadataId
                                                select a).FirstOrDefault();
                if (attribute == null)
                {
                    return nameForOptionSet;
                }
                nameForOptionSet = string.Format(CultureInfo.CurrentCulture, "{0}{1}", GetNameForEntity(entityMetadata, services),
                    GetNameForAttribute(entityMetadata, attribute, services));
                nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
                return nameForOptionSet;
            }
            // Return default option set name if not using the display name.
            nameForOptionSet = DefaultNamingService.GetNameForOptionSet(entityMetadata, optionSetMetadata, services);
            nameForOptionSet = CheckDuplicateOptionSetNames(services, entityMetadata, optionSetMetadata, nameForOptionSet);
            return nameForOptionSet;
        }

        /// <summary>
        ///     Retrieves a name for the Attribute being generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="attributeMetadata">
        ///     The attribute metadata.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for attribute.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForAttribute(EntityMetadata,AttributeMetadata,IServiceProvider)"/>
        public string GetNameForAttribute(EntityMetadata entityMetadata, AttributeMetadata attributeMetadata, IServiceProvider services)
        {
            string nameForAttribute = DefaultNamingService.GetNameForAttribute(entityMetadata, attributeMetadata, services);
            nameForAttribute = CheckDuplicateAttributeNames(services, entityMetadata, attributeMetadata, nameForAttribute);
            // Return logical name of attribute.
            if (!_useDisplayName)
            {
                return nameForAttribute;
            }
            // Remove publisher prefix from out-of-the-box attribute and return.
            string nameForEntity = GetNameForEntity(entityMetadata, services);
            if (attributeMetadata.IsCustomAttribute == null ||
                (!attributeMetadata.IsCustomAttribute.Value && !attributeMetadata.SchemaName.Contains("_") &&
                 !attributeMetadata.LogicalName.Equals("statecode") &&
                 !attributeMetadata.LogicalName.Equals("statuscode")) ||
                string.IsNullOrEmpty(attributeMetadata.DisplayName?.UserLocalizedLabel?.Label))
            {
                nameForAttribute = RemovePublisherCustomizationPrefix(services,
                    nameForAttribute);
                nameForAttribute =
                    CheckNameNotSameAsParent(nameForEntity, nameForAttribute);
                nameForAttribute =
                    CheckDuplicateAttributeNames(services, entityMetadata, attributeMetadata, nameForAttribute);
                return nameForAttribute;
            }
            // Return main entity identifier attribute.
            if (attributeMetadata.LogicalName.ToLower(CultureInfo.CurrentCulture).Equals(
                    string.Format(CultureInfo.CurrentCulture, "{0}id",
                        entityMetadata.LogicalName.ToLower(CultureInfo.CurrentCulture))))
            {
                nameForAttribute = string.Format(CultureInfo.CurrentCulture, "{0}Id", nameForEntity);
                nameForAttribute = CheckDuplicateAttributeNames(services, entityMetadata, attributeMetadata, nameForAttribute);
                return nameForAttribute;
            }
            switch (attributeMetadata.LogicalName)
            {
                case "statecode":
                    // We append the word "Code" in case there are custom fields called "Status" to avoid duplicates.
                    // Return the status attribute.
                    return string.Format(CultureInfo.CurrentCulture, "{0}StatusCode", nameForEntity);
                case "statuscode":
                    // We append the word "Code" in case there are custom fields called "StatusReason" to avoid duplicates.
                    // Return the status reason attribute.
                    return string.Format(CultureInfo.CurrentCulture, "{0}StatusReasonCode", nameForEntity);
                default:
                    // If the field is not the identifier, state or status field and it's name is a duplicate of the identifier, state or status field, we append "1" to the end of the name. 
                    nameForAttribute = EnsureValidIdentifier(attributeMetadata.DisplayName?.UserLocalizedLabel?.Label!);
                    nameForAttribute =
                        CheckNameNotSameAsParent(nameForEntity, nameForAttribute);
                    nameForAttribute = CheckDuplicateAttributeNames(services, entityMetadata, attributeMetadata, nameForAttribute);
                    if (!string.Equals(nameForAttribute,
                            string.Format(CultureInfo.CurrentCulture, "{0}Id", nameForEntity)) &&
                        !string.Equals(nameForAttribute,
                            string.Format(CultureInfo.CurrentCulture, "{0}StatusCode", nameForEntity)) &&
                        !string.Equals(nameForAttribute,
                            string.Format(CultureInfo.CurrentCulture, "{0}StatusReasonCode", nameForEntity)))
                    {
                        return nameForAttribute;
                    }
                    nameForAttribute = string.Format(CultureInfo.CurrentCulture, "{0}1", nameForAttribute);
                    return nameForAttribute;
            }
        }

        /// <summary>
        ///     Retrieves a name for the Entity being generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for entity.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForEntity(EntityMetadata,IServiceProvider)"/>
        public string GetNameForEntity(EntityMetadata entityMetadata, IServiceProvider services)
        {
            string name;
            int nameCount = 1;
            if (!_useDisplayName)
            {
                name = DefaultNamingService.GetNameForEntity(entityMetadata, services);
            }
            else
            {
                if (entityMetadata.IsCustomEntity != null && (entityMetadata.IsCustomEntity.Value || entityMetadata.SchemaName.Contains("_")) && !string.IsNullOrEmpty(entityMetadata.DisplayName?.UserLocalizedLabel?.Label))
                {
                    name = EnsureValidIdentifier(entityMetadata.DisplayName?.UserLocalizedLabel?.Label!);
                }
                else
                {
                    name = RemovePublisherCustomizationPrefix(services, DefaultNamingService.GetNameForEntity(entityMetadata, services));
                }
            }
            foreach (KeyValuePair<Guid, string> entityName in _entityNames.Where(entityName => entityMetadata.MetadataId != null && entityMetadata.MetadataId.Value != entityName.Key))
            {
                while (entityName.Value == name)
                {
                    name += nameCount.ToString(CultureInfo.CurrentCulture);
                    nameCount++;
                }
            }
            if (entityMetadata.MetadataId != null && !_entityNames.ContainsKey(entityMetadata.MetadataId.Value))
            {
                _entityNames.Add(entityMetadata.MetadataId.Value, name);
            }
            return name;
        }

        /// <summary>
        ///     Retrieves a name for a set of entities.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for entity set.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForEntitySet(EntityMetadata,IServiceProvider)"/>
        public string GetNameForEntitySet(EntityMetadata entityMetadata, IServiceProvider services)
        {
            string name;
            int nameCount = 1;
            if (!_useDisplayName)
            {
                name = DefaultNamingService.GetNameForEntitySet(entityMetadata, services);
            }
            else
            {
                if (entityMetadata.IsCustomEntity != null && (entityMetadata.IsCustomEntity.Value || entityMetadata.SchemaName.Contains("_")) && !string.IsNullOrEmpty(entityMetadata.DisplayName?.UserLocalizedLabel?.Label))
                {
                    name = EnsureValidIdentifier(entityMetadata.DisplayName?.UserLocalizedLabel?.Label!) + "Set";
                }
                else
                {
                    name = RemovePublisherCustomizationPrefix(services, DefaultNamingService.GetNameForEntitySet(entityMetadata, services));
                }
            }
            foreach (KeyValuePair<Guid, string> entitySetName in _entitySetNames.Where(entitySetName => entityMetadata.MetadataId != null && entityMetadata.MetadataId.Value != entitySetName.Key))
            {
                while (entitySetName.Value == name)
                {
                    name += nameCount.ToString(CultureInfo.CurrentCulture);
                    nameCount++;
                }
            }
            if (entityMetadata.MetadataId != null && !_entitySetNames.ContainsKey(entityMetadata.MetadataId.Value))
            {
                _entitySetNames.Add(entityMetadata.MetadataId.Value, name);
            }
            return name;
        }

        /// <summary>
        ///     Retrieves a name for the MessagePair being generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="messagePair">
        ///     The message pair.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for message pair.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForMessagePair(SdkMessagePair,IServiceProvider)"/>
        public string GetNameForMessagePair(SdkMessagePair messagePair, IServiceProvider services)
        {
            return RemovePublisherCustomizationPrefix(services, DefaultNamingService.GetNameForMessagePair(messagePair, services));
        }

        /// <summary>
        ///     Retrieves a name for the 1:N, N:N, or N:1 relationship being generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="relationshipMetadata">
        ///     The relationship metadata.
        /// </param>
        /// <param name="reflexiveRole">
        ///     The reflexive role.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for relationship.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForRelationship(EntityMetadata,RelationshipMetadataBase,EntityRole?,IServiceProvider)"/>
        public string GetNameForRelationship(EntityMetadata? entityMetadata, RelationshipMetadataBase relationshipMetadata, EntityRole? reflexiveRole, IServiceProvider services)
        {
            string name = DefaultNamingService.GetNameForRelationship(entityMetadata, relationshipMetadata, reflexiveRole, services);
            name = RemovePublisherCustomizationPrefix(services, name);
            name = EnsureValidIdentifier(name);
            name = CheckDuplicateRelationshipNames(services, entityMetadata, relationshipMetadata, name);
            return name;
        }

        /// <summary>
        ///     Retrieves a name for the Request Field being generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="request">
        ///     The request.
        /// </param>
        /// <param name="requestField">
        ///     The request field.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for request field.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForRequestField(SdkMessageRequest,SdkMessageRequestField,IServiceProvider)"/>
        public string GetNameForRequestField(SdkMessageRequest request, SdkMessageRequestField requestField, IServiceProvider services)
        {
            return DefaultNamingService.GetNameForRequestField(request, requestField, services);
        }

        /// <summary>
        ///     Retrieves a name for the Response Field being generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="response">
        ///     The response.
        /// </param>
        /// <param name="responseField">
        ///     The response field.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for response field.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForResponseField(SdkMessageResponse,SdkMessageResponseField,IServiceProvider)"/>
        public string GetNameForResponseField(SdkMessageResponse response, SdkMessageResponseField responseField, IServiceProvider services)
        {
            return DefaultNamingService.GetNameForResponseField(response, responseField, services);
        }

        /// <summary>
        ///     Retrieves a name for the data context being generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for service context.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForServiceContext(IServiceProvider)"/>
        public string GetNameForServiceContext(IServiceProvider services)
        {
            return DefaultNamingService.GetNameForServiceContext(services);
        }

        /// <summary>
        ///     Retrieves a name for the Option being generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <param name="optionMetadata">
        ///     The option metadata.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The name for option.
        /// </returns>
        /// <seealso cref="INamingService.GetNameForOption(OptionSetMetadataBase,OptionMetadata,IServiceProvider)"/>
        public string GetNameForOption(OptionSetMetadataBase optionSetMetadata, OptionMetadata optionMetadata, IServiceProvider services)
        {
            string name = DefaultNamingService.GetNameForOption(optionSetMetadata, optionMetadata, services);
            name = EnsureValidIdentifier(name);
            name = EnsureUniqueOptionName(optionSetMetadata, name);
            if (string.IsNullOrEmpty(name))
            {
                name = "Blank";
            }
            return name;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Removes the publisher customization prefix.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private string RemovePublisherCustomizationPrefix(IServiceProvider services, string name)
        {
            if (_customizationPrefixes?.Count == 0)
            {
                _metadataProviderService = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
                Type sdkMetadataProviderService = _metadataProviderService.GetType();
                MethodInfo createOrganizationServiceEndpoint = sdkMetadataProviderService.GetMethod("CreateOrganizationServiceEndpoint", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod)!;
                CrmServiceClient serviceEndpoint = (CrmServiceClient)createOrganizationServiceEndpoint.Invoke(_metadataProviderService, null)!;
                if (serviceEndpoint.OrganizationWebProxyClient != null)
                {
                    using OrganizationWebProxyClient service = serviceEndpoint.OrganizationWebProxyClient;
                    GetPublishers(service);
                }
                else
                {
                    if (serviceEndpoint.OrganizationServiceProxy != null)
                    {
                        using OrganizationServiceProxy service = serviceEndpoint.OrganizationServiceProxy;
                        GetPublishers(service);
                    }
                }
            }

            if (_customizationPrefixes != null)
            {
                name = name.Contains("_")
                    ? _customizationPrefixes.Aggregate(name, (current, prefix) => current.Replace(prefix, string.Empty))
                    : name;
            }
            string tempName = name
                .Split('_')
                .Where(word => !string.IsNullOrEmpty(word))
                .Aggregate(string.Empty, (current, word)
                    => current + (string.Format(CultureInfo.CurrentCulture, "{0}{1}",
                        word.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture),
                        word.Substring(1, word.Length - 1))));
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
                if (_customizationPrefixes != null && !_customizationPrefixes.Contains(customizationPrefix))
                {
                    _customizationPrefixes.Add(string.Format(CultureInfo.CurrentCulture, "{0}_",
                        customizationPrefix));
                }
            }
        }

        /// <summary>
        ///     Ensures that valid identifier.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private static string EnsureValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            name = name.Trim();
            // Check to make sure that the name begins with an alphabet character.
            if (name.ToCharArray(0, 1)[0] >= '0' && name.ToCharArray(0, 1)[0] <= '9')
            {
                // Prepend to the name if it is not valid.
                name = string.Format(CultureInfo.CurrentCulture, "ClsCompliant{0}", name);
            }
            // Remove special characters.
            foreach (char c in name.Where(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'z' or >= 'A' and <= 'Z')))
            {
                name = name.Replace(c.ToString(), string.Empty);
            }
            return name;
        }

        /// <summary>
        ///     Ensures that unique option name.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="optionMetadata">
        ///     The option metadata.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private string EnsureUniqueOptionName(OptionSetMetadataBase optionMetadata, string name)
        {
            while (true)
            {
                if (_optionNames.ContainsKey(optionMetadata))
                {
                    if (_optionNames[optionMetadata].ContainsKey(name))
                    {
                        // Increment the number of times that an option with this name has
                        // been found.
                        ++_optionNames[optionMetadata][name];

                        // Append the number to the name to create a new, unique name.
                        string newName = string.Format(CultureInfo.CurrentCulture, "{0}{1}", name, _optionNames[optionMetadata][name]);

                        // Call this function again to make sure that our new name is unique.
                        name = newName;
                        continue;
                    }
                }
                else
                {
                    // This is the first time this OptionSet has been encountered. Add it to
                    // the dictionary.
                    _optionNames[optionMetadata] = new Dictionary<string, int>();
                }

                // This is the first time this name has been encountered. Begin keeping track
                // of the times we've run across it.
                _optionNames[optionMetadata][name] = 1;

                return name;
            }
        }

        /// <summary>
        ///     Gets option set name.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="logicalName">
        ///     Name of the logical.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <returns>
        ///     The option set name.
        /// </returns>
        private string GetOptionSetName(string logicalName, IServiceProvider services, EntityMetadata entityMetadata, OptionSetMetadataBase optionSetMetadata)
        {
            string[] logicalNameMetadata = logicalName.Split('_');
            if (logicalNameMetadata.Length != 2)
            {
                return !string.IsNullOrEmpty(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label)
                    ? EnsureValidIdentifier(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label!)
                    : RemovePublisherCustomizationPrefix(services,
                        DefaultNamingService.GetNameForOptionSet(entityMetadata, optionSetMetadata, services));
            }
            string entityLogicalName = logicalNameMetadata[0];
            string attributeLogicalName = logicalNameMetadata[1];
            _metadataProviderService = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
            _metadata = _metadataProviderService.LoadMetadata();
            EntityMetadata? localEntityMetadata = _metadata.Entities.FirstOrDefault(entity => entity.LogicalName.Equals(entityLogicalName));
            if (localEntityMetadata != null)
            {
                return ProcessAttributeMetadata(services, optionSetMetadata, attributeLogicalName, localEntityMetadata);
            }
            return !string.IsNullOrEmpty(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label)
                ? EnsureValidIdentifier(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label!)
                : RemovePublisherCustomizationPrefix(services, DefaultNamingService.GetNameForOptionSet(null, optionSetMetadata, services));
        }

        /// <summary>
        ///     Check enum not same as entity.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/04/2022.
        /// </remarks>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private string CheckEnumNotSameAsEntity(IServiceProvider services, string name)
        {
            int nameCount = 1;
            _metadataProviderService = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
            _metadata = _metadataProviderService.LoadMetadata();
            while (_metadata.Entities.Any(entityMetadata => GetNameForEntity(entityMetadata, services) == name))
            {
                name += nameCount.ToString(CultureInfo.CurrentCulture);
                nameCount++;
            }
            return name;
        }

        /// <summary>
        ///     Check name not same as parent.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/04/2022.
        /// </remarks>
        /// <param name="parent">
        ///     The parent.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private static string CheckNameNotSameAsParent(string parent, string name)
        {
            if (parent == name)
            {
                name += "1";
            }
            return name;
        }

        /// <summary>
        ///     Process the attribute metadata.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <param name="attributeLogicalName">
        ///     Name of the attribute logical.
        /// </param>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private string ProcessAttributeMetadata(IServiceProvider services, OptionSetMetadataBase optionSetMetadata, string attributeLogicalName, EntityMetadata entityMetadata)
        {
            return attributeLogicalName switch
            {
                "statecode" => string.Format(CultureInfo.CurrentCulture, "{0}StatusCode",
                    GetNameForEntity(entityMetadata, services)),
                "statuscode" => string.Format(CultureInfo.CurrentCulture, "{0}StatusReasonCode",
                    GetNameForEntity(entityMetadata, services)),
                _ => !string.IsNullOrEmpty(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label)
                    ? EnsureValidIdentifier(optionSetMetadata.DisplayName?.UserLocalizedLabel?.Label!)
                    : DefaultNamingService.GetNameForOptionSet(entityMetadata, optionSetMetadata, services)
            };
        }

        /// <summary>
        ///     Check duplicate attribute names.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/04/2022.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when one or more required arguments are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="attributeMetadata">
        ///     The attribute metadata.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private string CheckDuplicateAttributeNames(IServiceProvider services, MetadataBase? entityMetadata, AttributeMetadata attributeMetadata, string name)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (attributeMetadata is null)
            {
                throw new ArgumentNullException(nameof(attributeMetadata));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }
            int nameCount = 1;
            if (entityMetadata == null)
            {
                _metadataProviderService = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
                _metadata = _metadataProviderService.LoadMetadata();
                entityMetadata = _metadata.Entities.FirstOrDefault(e =>
                    e.Attributes.Any(a => a.MetadataId == attributeMetadata.MetadataId));
            }
            if (entityMetadata?.MetadataId == null && attributeMetadata.MetadataId != null)
            {
                foreach (PropertyName attributeName in _attributeNames.Where(attributeName =>
                             attributeName != null && attributeMetadata.MetadataId.Value != attributeName.AttributeMetadataId))
                {
                    while (attributeName?.AttributeName == name)
                    {
                        name += nameCount.ToString(CultureInfo.CurrentCulture);
                        nameCount++;
                    }
                }
            }
            if (entityMetadata?.MetadataId != null && attributeMetadata.MetadataId != null)
            {
                foreach (PropertyName attributeName in _attributeNames.Where(attributeName =>
                                 attributeName != null
                                 && attributeMetadata.MetadataId.Value != attributeName.AttributeMetadataId
                                 && entityMetadata.MetadataId.Value == attributeName.EntityMetadataId))
                {
                    while (attributeName?.AttributeName == name)
                    {
                        name += nameCount.ToString(CultureInfo.CurrentCulture);
                        nameCount++;
                    }
                }
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("id"))
            {
                name = "Identifier";
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("entitylogicalname"))
            {
                name = "EntityLogicalNameColumn";
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("entitysetname"))
            {
                name = "EntitySetNameColumn";
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("entitytypecode"))
            {
                name = "EntityTypeCodeColumn";
            }
            if (attributeMetadata.MetadataId != null
                && _attributeNames.All(propertyName => propertyName.AttributeMetadataId != attributeMetadata.MetadataId.Value))
            {
                if (entityMetadata?.MetadataId != null)
                {
                    _attributeNames.Add(new PropertyName(entityMetadata.MetadataId.Value,
                        attributeMetadata.MetadataId.Value, name));
                }
                else
                {
                    _attributeNames.Add(new PropertyName(null,
                        attributeMetadata.MetadataId.Value, name));
                }
            }
            else
            {
                if (attributeMetadata.MetadataId == null)
                {
                    return name;
                }
                foreach (PropertyName propertyName in _attributeNames.Where(p =>
                             p.EntityMetadataId == null && p.AttributeMetadataId == attributeMetadata.MetadataId.Value))
                {
                    if (entityMetadata?.MetadataId != null)
                    {
                        propertyName.EntityMetadataId = entityMetadata.MetadataId.Value;
                    }
                    propertyName.AttributeName = name;
                }
                if (entityMetadata?.MetadataId == null)
                {
                    return name;
                }
                foreach (PropertyName propertyName in _attributeNames.Where(p =>
                             p.EntityMetadataId == entityMetadata.MetadataId.Value &&
                             p.AttributeMetadataId == attributeMetadata.MetadataId.Value))
                {
                    propertyName.AttributeName = name;
                }
                if (_attributeNames.Any(propertyName
                        => propertyName.EntityMetadataId != entityMetadata.MetadataId.Value
                           && propertyName.AttributeMetadataId == attributeMetadata.MetadataId.Value))
                {
                    _attributeNames.Add(new PropertyName(entityMetadata.MetadataId.Value,
                        attributeMetadata.MetadataId.Value, name));
                }
            }
            return name;
        }

        /// <summary>
        ///     Check duplicate option set names.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 19/04/2022.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when one or more required arguments are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private string CheckDuplicateOptionSetNames(IServiceProvider services, MetadataBase? entityMetadata, OptionSetMetadataBase optionSetMetadata, string name)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (optionSetMetadata is null)
            {
                throw new ArgumentNullException(nameof(optionSetMetadata));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }
            int nameCount = 1;
            if (entityMetadata == null)
            {
                _metadataProviderService = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
                _metadata = _metadataProviderService.LoadMetadata();
                entityMetadata = _metadata.Entities.FirstOrDefault(e =>
                    e.Attributes.Any(a => a.MetadataId == optionSetMetadata.MetadataId));
            }
            name = CheckAndUpdateDuplicateOptionSetName(entityMetadata, optionSetMetadata, name, nameCount);
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("id"))
            {
                name = "Identifier";
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("entitylogicalname"))
            {
                name = "EntityLogicalNameColumn";
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("entitysetname"))
            {
                name = "EntitySetNameColumn";
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("entitytypecode"))
            {
                name = "EntityTypeCodeColumn";
            }
            if (optionSetMetadata.MetadataId != null
                && _optionSetNames.All(propertyName => propertyName.AttributeMetadataId != optionSetMetadata.MetadataId.Value))
            {
                AddOptionSetName(entityMetadata, optionSetMetadata, name);
            }
            else
            {
                UpdateOptionSetName(entityMetadata, optionSetMetadata, name);
                AddOptionSetToDifferentEntity(entityMetadata, optionSetMetadata, name);
            }
            return name;
        }

        /// <summary>
        ///     Updates the option set name.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 22/04/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        private void UpdateOptionSetName(MetadataBase? entityMetadata, MetadataBase optionSetMetadata, string name)
        {
            if (optionSetMetadata.MetadataId == null)
            {
                return;
            }
            foreach (PropertyName propertyName in _optionSetNames.Where(p =>
                         p.EntityMetadataId == null && p.AttributeMetadataId == optionSetMetadata.MetadataId.Value))
            {
                if (entityMetadata?.MetadataId != null)
                {
                    propertyName.EntityMetadataId = entityMetadata.MetadataId.Value;
                }

                propertyName.AttributeName = name;
            }
            if (entityMetadata?.MetadataId == null)
            {
                return;
            }
            foreach (PropertyName propertyName in _optionSetNames.Where(p =>
                         p.EntityMetadataId == entityMetadata.MetadataId.Value &&
                         p.AttributeMetadataId == optionSetMetadata.MetadataId.Value))
            {
                propertyName.AttributeName = name;
            }
        }

        /// <summary>
        ///     Adds an option set to different entity.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 22/04/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        private void AddOptionSetToDifferentEntity(MetadataBase? entityMetadata, OptionSetMetadataBase optionSetMetadata,
            string name)
        {
            if (optionSetMetadata.MetadataId == null)
            {
                return;
            }
            if (entityMetadata?.MetadataId != null)
            {
                if (_optionSetNames.Any(propertyName
                            => propertyName.EntityMetadataId != entityMetadata.MetadataId.Value
                               && propertyName.AttributeMetadataId == optionSetMetadata.MetadataId.Value))
                {
                    _optionSetNames.Add(new PropertyName(entityMetadata.MetadataId.Value,
                        optionSetMetadata.MetadataId.Value, name));
                } 
            }
            else
            {
                if (_optionSetNames.Any(propertyName
                        => propertyName.EntityMetadataId != null
                           && propertyName.AttributeMetadataId == optionSetMetadata.MetadataId.Value))
                {
                    _optionSetNames.Add(new PropertyName(null,
                        optionSetMetadata.MetadataId.Value, name));
                }
            }
        }

        /// <summary>
        ///     Adds an option set name.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 22/04/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        private void AddOptionSetName(MetadataBase? entityMetadata, MetadataBase optionSetMetadata, string name)
        {
            if (optionSetMetadata.MetadataId == null)
            {
                return;
            }
            if (entityMetadata?.MetadataId != null)
            {
                _optionSetNames.Add(new PropertyName(entityMetadata.MetadataId.Value,
                    optionSetMetadata.MetadataId.Value, name));
            }
            else
            {
                _optionSetNames.Add(new PropertyName(null,
                    optionSetMetadata.MetadataId.Value, name));
            }
        }

        /// <summary>
        ///     Check and update duplicate option set name.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 22/04/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <param name="nameCount">
        ///     Number of names.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private string CheckAndUpdateDuplicateOptionSetName(MetadataBase? entityMetadata,
            MetadataBase optionSetMetadata, string name, int nameCount)
        {
            if (entityMetadata?.MetadataId == null && optionSetMetadata.MetadataId != null)
            {
                foreach (PropertyName optionSetName in _optionSetNames.Where(optionSetName =>
                             optionSetName != null && optionSetMetadata.MetadataId.Value != optionSetName.AttributeMetadataId))
                {
                    while (optionSetName?.AttributeName == name)
                    {
                        name += nameCount.ToString(CultureInfo.CurrentCulture);
                        nameCount++;
                    }
                }
            }
            if (optionSetMetadata.MetadataId == null)
            {
                return name;
            }
            foreach (PropertyName optionSetName in _optionSetNames.Where(optionSetName =>
                         optionSetMetadata.MetadataId != null &&
                         optionSetName != null &&
                         optionSetMetadata.MetadataId.Value != optionSetName.AttributeMetadataId))
            {
                while (optionSetName?.AttributeName == name)
                {
                    name += nameCount.ToString(CultureInfo.CurrentCulture);
                    nameCount++;
                }
            }
            return name;
        }

        /// <summary>
        ///     Check duplicate relationship names.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/04/2022.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when one or more required arguments are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        /// <param name="services">
        ///     .
        /// </param>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="relationshipMetadata">
        ///     The relationship metadata.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private string CheckDuplicateRelationshipNames(IServiceProvider services, MetadataBase? entityMetadata, RelationshipMetadataBase relationshipMetadata, string name)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (relationshipMetadata is null)
            {
                throw new ArgumentNullException(nameof(relationshipMetadata));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }
            int nameCount = 1;
            if (entityMetadata == null)
            {
                _metadataProviderService = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
                _metadata = _metadataProviderService.LoadMetadata();
                entityMetadata = _metadata.Entities.FirstOrDefault(e =>
                    e.Attributes.Any(a => a.MetadataId == relationshipMetadata.MetadataId));
            }
            if (entityMetadata?.MetadataId == null && relationshipMetadata.MetadataId != null)
            {
                foreach (PropertyName relationshipName in _relationshipNames.Where(relationshipName =>
                             relationshipName != null && relationshipMetadata.MetadataId.Value != relationshipName.AttributeMetadataId))
                {
                    while (relationshipName?.AttributeName == name)
                    {
                        name += nameCount.ToString(CultureInfo.CurrentCulture);
                        nameCount++;
                    }
                }
            }
            if (entityMetadata?.MetadataId != null && relationshipMetadata.MetadataId != null)
            {
                foreach (PropertyName relationshipName in _relationshipNames.Where(relationshipName =>
                                 relationshipName != null
                                 && relationshipMetadata.MetadataId.Value != relationshipName.AttributeMetadataId
                                 && entityMetadata.MetadataId.Value == relationshipName.EntityMetadataId))
                {
                    while (relationshipName?.AttributeName == name)
                    {
                        name += nameCount.ToString(CultureInfo.CurrentCulture);
                        nameCount++;
                    }
                }
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("id"))
            {
                name = "Identifier";
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("entitylogicalname"))
            {
                name = "EntityLogicalNameColumn";
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("entitysetname"))
            {
                name = "EntitySetNameColumn";
            }
            if (name.ToLower(CultureInfo.CurrentCulture).Equals("entitytypecode"))
            {
                name = "EntityTypeCodeColumn";
            }
            if (relationshipMetadata.MetadataId != null
                && _relationshipNames.All(propertyName => propertyName.AttributeMetadataId != relationshipMetadata.MetadataId.Value))
            {
                if (entityMetadata?.MetadataId != null)
                {
                    _relationshipNames.Add(new PropertyName(entityMetadata.MetadataId.Value,
                        relationshipMetadata.MetadataId.Value, name));
                }
                else
                {
                    _relationshipNames.Add(new PropertyName(null,
                        relationshipMetadata.MetadataId.Value, name));
                }
            }
            else
            {
                if (relationshipMetadata.MetadataId == null)
                {
                    return name;
                }
                foreach (PropertyName propertyName in _relationshipNames.Where(p =>
                             p.EntityMetadataId == null && p.AttributeMetadataId == relationshipMetadata.MetadataId.Value))
                {
                    if (entityMetadata?.MetadataId != null)
                    {
                        propertyName.EntityMetadataId = entityMetadata.MetadataId.Value;
                    }
                    propertyName.AttributeName = name;
                }
                if (entityMetadata?.MetadataId == null)
                {
                    return name;
                }
                foreach (PropertyName propertyName in _relationshipNames.Where(p =>
                             p.EntityMetadataId == entityMetadata.MetadataId.Value &&
                             p.AttributeMetadataId == relationshipMetadata.MetadataId.Value))
                {
                    propertyName.AttributeName = name;
                }
                if (_relationshipNames.Any(propertyName
                        => propertyName.EntityMetadataId != entityMetadata.MetadataId.Value
                           && propertyName.AttributeMetadataId == relationshipMetadata.MetadataId.Value))
                {
                    _relationshipNames.Add(new PropertyName(entityMetadata.MetadataId.Value,
                        relationshipMetadata.MetadataId.Value, name));
                }
            }
            return name;
        }

        #endregion

    }
}