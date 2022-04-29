#nullable enable

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
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
// ReSharper disable UnusedType.Global

namespace CrmDeveloperToolkitExtender.CrmSvcUtil
{
    /// <summary>
    ///     A service for accessing code customizations information. This class cannot be
    ///     inherited.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 17/03/2022.
    /// </remarks>
    /// <seealso cref="ICustomizeCodeDomService"/>
    public sealed class CodeCustomizationService : ICustomizeCodeDomService
    {
        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        public CodeCustomizationService()
        {
            _useDisplayName = bool.TryParse(ConfigurationManager.AppSettings["useDisplayName"], out _useDisplayName) && bool.Parse(ConfigurationManager.AppSettings["useDisplayName"]);
            _customizationPrefixes = new Collection<string>();
            _updatedEntityNames = new Dictionary<Guid, string?>();
        }

        #region Properties

        /// <summary>
        ///     The metadata.
        /// </summary>
        private IOrganizationMetadata? _metadata;

        /// <summary>
        ///     The entity class.
        /// </summary>
        private CodeTypeDeclaration? _entityClass;

        /// <summary>
        ///     The code namespace.
        /// </summary>
        private CodeNamespace? _codeNamespace;

        /// <summary>
        ///     The metadata provider service.
        /// </summary>
        private IMetadataProviderService? _metadataProviderService;

        /// <summary>
        ///     The filter service.
        /// </summary>
        private ICodeWriterFilterService? _filterService;

        /// <summary>
        ///     (Immutable) the open summary.
        /// </summary>
        private const string OpenSummary = "<Summary>";

        /// <summary>
        ///     (Immutable) the close summary.
        /// </summary>
        private const string CloseSummary = "</Summary>";

        /// <summary>
        ///     The entities class.
        /// </summary>
        private GenerateEntities? _entitiesClass;

        /// <summary>
        ///     (Immutable) true to use display name.
        /// </summary>
        private readonly bool _useDisplayName;

        /// <summary>
        ///     Name of the entity.
        /// </summary>
        private string? _entityName;

        /// <summary>
        /// The customization prefixes
        /// </summary>
        private static Collection<string>? _customizationPrefixes;

        /// <summary>
        ///     (Immutable) list of names of the updated entities.
        /// </summary>
        private readonly Dictionary<Guid, string?> _updatedEntityNames;

        #endregion

        /// <summary>
        ///     Customize the generated types before code is generated.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="codeUnit">
        ///     The code unit.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <seealso cref="ICustomizeCodeDomService.CustomizeCodeDom(CodeCompileUnit,IServiceProvider)"/>
        public void CustomizeCodeDom(CodeCompileUnit codeUnit, IServiceProvider services)
        {
            foreach (CodeNamespace codeNamespace in codeUnit.Namespaces)
            {
                _codeNamespace = codeNamespace;
                _metadataProviderService = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
                _filterService = (ICodeWriterFilterService)services.GetService(typeof(ICodeWriterFilterService));
                _metadata = _metadataProviderService.LoadMetadata();
                UpdateEntities(services);
                foreach (CodeTypeDeclaration codeTypeDeclaration in _codeNamespace.Types)
                {
                    GetUpdatedEntityNames(codeTypeDeclaration);
                    MarkTypesNotClsCompliant(codeTypeDeclaration);
                    AddComments(codeTypeDeclaration);
                    CheckEnumMemberAttributeValues(codeTypeDeclaration);
                    AddSuppressions(codeTypeDeclaration);
                }
            }
        }

        /// <summary>
        /// 	Check enum member attribute values.
        /// </summary>
        /// <remarks>
        /// 	Johan Küstner, 30/03/2022.
        /// </remarks>
        /// <param name="codeTypeDeclaration">
        /// 	The code type declaration.
        /// </param>
        private static void CheckEnumMemberAttributeValues(CodeTypeDeclaration codeTypeDeclaration)
        {
            if (!codeTypeDeclaration.IsEnum)
            {
                return;
            }
            foreach (CodeTypeMember codeTypeMember in codeTypeDeclaration.Members)
            {
                codeTypeMember.Name = EnsureValidAttributeIdentifier(codeTypeMember.Name);
            }
        }

        /// <summary>
        ///     Gets the updated entity names.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 29/03/2022.
        /// </remarks>
        /// <param name="typeDeclaration">
        ///     The type declaration.
        /// </param>
        private void GetUpdatedEntityNames(CodeTypeMember typeDeclaration)
        {
            foreach (CodeAttributeDeclaration attributeDeclaration in typeDeclaration.CustomAttributes)
            {
                if (!attributeDeclaration.Name.ToLower(CultureInfo.CurrentCulture)
                    .Contains(typeof(EntityLogicalNameAttribute).ToString().ToLower(CultureInfo.CurrentCulture)))
                {
                    continue;
                }
                if (attributeDeclaration.Arguments[0].Value is not CodePrimitiveExpression codePrimitiveExpression)
                {
                    continue;
                }
                IEnumerable<EntityMetadata>? entityMetadataCollection =
                    _metadata?.Entities.Where(x => x.LogicalName == codePrimitiveExpression.Value as string &&
                                                   x.SchemaName.ToLower(CultureInfo.CurrentCulture) != typeDeclaration.Name.ToLower(CultureInfo.CurrentCulture));
                if (entityMetadataCollection == null)
                {
                    continue;
                }
                foreach (EntityMetadata entityMetadata in entityMetadataCollection)
                {
                    if (entityMetadata.MetadataId.HasValue)
                    {
                        _updatedEntityNames.Add(entityMetadata.MetadataId.Value, typeDeclaration.Name);
                    }
                }
            }
        }

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
        private string? RemovePublisherCustomizationPrefix(IServiceProvider services, string? name)
        {
            if (_customizationPrefixes is { Count: 0 })
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
                name = name != null && name.Contains("_")
                    ? _customizationPrefixes.Aggregate(name, (current, prefix) => current.Replace(prefix, string.Empty))
                    : name;
            }
            string? tempName = name?.Split('_').Where(word => !string.IsNullOrEmpty(word))
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
        ///     Updates the entities described by services.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 29/03/2022.
        /// </remarks>
        /// <param name="services">
        ///     The services.
        /// </param>
        private void UpdateEntities(IServiceProvider services)
        {
            if (_metadata?.Entities == null)
            {
                return;
            }
            foreach (EntityMetadata entityMetadata in _metadata.Entities)
            {
                if (_filterService != null && !_filterService.GenerateEntity(entityMetadata, services))
                {
                    continue;
                }
                switch (_useDisplayName)
                {
                    case true when entityMetadata?.IsCustomEntity != null && entityMetadata.IsCustomEntity.Value &&
                                   !string.IsNullOrEmpty(entityMetadata.DisplayName?.UserLocalizedLabel?.Label):
                        if (entityMetadata.MetadataId.HasValue)
                        {
                            _entityName = EnsureValidEntityIdentifier(
                                entityMetadata.DisplayName?.UserLocalizedLabel?.Label!,
                                entityMetadata.MetadataId.Value);
                        }

                        break;
                    case true:
                        {
                            if (entityMetadata != null)
                            {
                                _entityName = RemovePublisherCustomizationPrefix(services,
                                    entityMetadata.SchemaName);
                            }

                            break;
                        }
                    default:
                        {
                            if (entityMetadata != null)
                            {
                                _entityName = entityMetadata.SchemaName;
                            }

                            break;
                        }
                }
                _entitiesClass = new GenerateEntities();
                if (_codeNamespace?.Types == null
                    || _codeNamespace.Types.Count == 0)
                {
                    continue;
                }

                if (_codeNamespace.Types.Cast<CodeTypeDeclaration>().All(codeType =>
                        codeType.Name.ToUpper(CultureInfo.CurrentCulture) !=
                        _entityName?.ToUpper(CultureInfo.CurrentCulture)))
                {
                    continue;
                }

                _entityClass = _codeNamespace.Types.Cast<CodeTypeDeclaration>().First(codeType =>
                    codeType.Name.ToUpper(CultureInfo.CurrentCulture) ==
                    _entityName?.ToUpper(CultureInfo.CurrentCulture));
                _entityClass = _entitiesClass.UpdateEntityClassDefaultConstructorsComments(_entityClass);
                _entityClass = _entitiesClass.UpdateEntityClassComments(_entityClass);
                UpdateEnumSetter(entityMetadata!);
            }
        }

        /// <summary>
        ///     Adds the suppressions.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 26/04/2022.
        /// </remarks>
        /// <param name="codeTypeDeclaration">
        ///     The code type declaration.
        /// </param>
        private static void AddSuppressions(CodeTypeDeclaration codeTypeDeclaration)
        {
            if (codeTypeDeclaration.Name != "EntityOptionSetEnum")
            {
                return;
            }
            foreach (CodeTypeMember codeTypeMember in codeTypeDeclaration.Members)
            {
                if (codeTypeMember.Name != "GetMultiEnum" &&
                    ((CodeMemberMethod)codeTypeMember).ReturnType.BaseType !=
                    typeof(OptionSetValueCollection).ToString())
                {
                    continue;
                }
                codeTypeMember.CustomAttributes.Add(new CodeAttributeDeclaration(
                    new CodeTypeReference(typeof(SuppressMessageAttribute)),
                    new CodeAttributeArgument(new CodePrimitiveExpression("Microsoft.Performance")),
                    new CodeAttributeArgument(new CodePrimitiveExpression("CA1811:AvoidUncalledPrivateCode"))));
                codeTypeMember.CustomAttributes.Add(new CodeAttributeDeclaration(
                    new CodeTypeReference(typeof(SuppressMessageAttribute)),
                    new CodeAttributeArgument(new CodePrimitiveExpression("Microsoft.Usage")),
                    new CodeAttributeArgument(new CodePrimitiveExpression("CA1801:ReviewUnusedParameters")),
                    new CodeAttributeArgument(new CodeSnippetExpression("MessageId = \"entity\""))));
                codeTypeMember.CustomAttributes.Add(new CodeAttributeDeclaration(
                    new CodeTypeReference(typeof(SuppressMessageAttribute)),
                    new CodeAttributeArgument(new CodePrimitiveExpression("Microsoft.Usage")),
                    new CodeAttributeArgument(new CodePrimitiveExpression("CA1801:ReviewUnusedParameters")),
                    new CodeAttributeArgument(new CodeSnippetExpression("MessageId = \"attributeLogicalName\""))));
            }
        }

        /// <summary>
        ///     Marks the types not CLS compliant.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 29/03/2022.
        /// </remarks>
        /// <param name="typeDeclaration">
        ///     The type declaration.
        /// </param>
        private static void MarkTypesNotClsCompliant(CodeTypeMember typeDeclaration)
        {
            if (typeDeclaration.Name == "EntityOptionSetEnum")
            {
                return;
            }
            CodeAttributeDeclaration codeAttributeDeclaration = new(new CodeTypeReference(typeof(CLSCompliantAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(false)));
            bool containsClsCompliance = false;
            foreach (CodeAttributeDeclaration declaration in typeDeclaration.CustomAttributes)
            {
                if (declaration.Name.ToLower(CultureInfo.CurrentCulture).Contains(typeof(CLSCompliantAttribute).ToString().ToLower(CultureInfo.CurrentCulture)))
                {
                    containsClsCompliance = true;
                }
            }
            if (!containsClsCompliance)
            {
                typeDeclaration.CustomAttributes.Add(codeAttributeDeclaration);
            }
        }

        /// <summary>
        ///     Adds comments.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 29/03/2022.
        /// </remarks>
        /// <param name="declaration">
        ///     The declaration.
        /// </param>
        private static void AddComments(CodeTypeDeclaration declaration)
        {
            if (declaration.Comments.Count == 0)
            {
                declaration.Comments.Add(new CodeCommentStatement(OpenSummary, true));
                declaration.Comments.Add(new CodeCommentStatement(declaration.Name, true));
                declaration.Comments.Add(new CodeCommentStatement(CloseSummary, true));
            }
            foreach (CodeTypeMember member in declaration.Members)
            {
                if (member.Comments.Count != 0)
                {
                    continue;
                }
                member.Comments.Add(new CodeCommentStatement(OpenSummary, true));
                member.Comments.Add(new CodeCommentStatement(member.Name, true));
                member.Comments.Add(new CodeCommentStatement(CloseSummary, true));
            }
        }

        /// <summary>
        ///     Updates the enum setter described by entityMetadata.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        private void UpdateEnumSetter(EntityMetadata entityMetadata)
        {
            foreach (AttributeMetadata attributeMetadata in entityMetadata.Attributes)
            {
                string attributeName = GetAttributeName(entityMetadata, attributeMetadata);
                if (attributeMetadata.AttributeType == null ||
                    (attributeMetadata.AttributeType.Value != AttributeTypeCode.Picklist &&
                     attributeMetadata.AttributeType.Value != AttributeTypeCode.Status &&
                     attributeMetadata.AttributeType.Value != AttributeTypeCode.State))
                {
                    continue;
                }
                if (_entityClass?.Members == null
                    || _entityClass.Members.Count == 0
                    || _entityClass.Members.Cast<CodeTypeMember>().All(codeMembers => codeMembers.Name != attributeName))
                {
                    continue;
                }
                foreach (CodeTypeMember codeMember in _entityClass.Members.Cast<CodeTypeMember>().Where(codeMembers => codeMembers.Name == attributeName))
                {
                    CodeMemberProperty codeProperty = (CodeMemberProperty)codeMember;
                    if (codeProperty.Type.BaseType != typeof(OptionSetValue).FullName)
                    {
                        continue;
                    }
                    if (codeProperty.HasGet)
                    {
                        if (codeProperty.GetStatements.Count == 2 && codeProperty.GetStatements[1].GetType() == typeof(CodeConditionStatement))
                        {
                            codeProperty.GetStatements.RemoveAt(1);
                            codeProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("optionSet")));
                        }
                    }
                    if (!codeProperty.HasSet)
                    {
                        continue;
                    }
                    if (codeProperty.SetStatements.Count == 2 && codeProperty.SetStatements[1].GetType() == typeof(CodeConditionStatement))
                    {
                        ((CodeConditionStatement)codeProperty.SetStatements[1]).FalseStatements[0]
                            = new CodeExpressionStatement(
                                new CodeMethodInvokeExpression(
                                    new CodeThisReferenceExpression(),
                                    "SetAttributeValue",
                                    new CodePrimitiveExpression(attributeMetadata.LogicalName),
                                    new CodeSnippetExpression("new Microsoft.Xrm.Sdk.OptionSetValue(value.Value)")));
                    }
                    else
                    {
                        codeProperty.SetStatements[1] = new CodeConditionStatement(
                            new CodeVariableReferenceExpression(
                                "(value == null)"),
                            new CodeStatement[]
                            {
                                new CodeExpressionStatement(
                                    new CodeMethodInvokeExpression(
                                        new CodeThisReferenceExpression(),
                                        "SetAttributeValue",
                                        new CodePrimitiveExpression(
                                            attributeMetadata.LogicalName),
                                        new CodePrimitiveExpression(null)))
                            }, new CodeStatement[]
                            {
                                new CodeExpressionStatement(
                                    new CodeMethodInvokeExpression(
                                        new CodeThisReferenceExpression(),
                                        "SetAttributeValue",
                                        new CodePrimitiveExpression(attributeMetadata.LogicalName),
                                        new CodeSnippetExpression("new Microsoft.Xrm.Sdk.OptionSetValue(value.Value)")))
                            });
                    }
                }
            }
        }

        /// <summary>
        ///     Gets attribute name.
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
        /// <returns>
        ///     The attribute name.
        /// </returns>
        private string GetAttributeName(EntityMetadata entityMetadata, AttributeMetadata attributeMetadata)
        {
            string attributeName = string.Empty;
            if (_useDisplayName
                && attributeMetadata.IsCustomAttribute.HasValue
                && (attributeMetadata.IsCustomAttribute.Value
                    || attributeMetadata.SchemaName.Contains("_")
                    || attributeMetadata.LogicalName.Equals("statecode")
                    || attributeMetadata.LogicalName.Equals("statuscode"))
                && !string.IsNullOrEmpty(attributeMetadata.DisplayName?.UserLocalizedLabel?.Label))
            {
                switch (attributeMetadata.LogicalName)
                {
                    case "statecode":
                        attributeName = GetStateCodeAttributeName(entityMetadata, attributeName);
                        break;
                    case "statuscode":
                        attributeName = GetStatusCodeAttributeName(entityMetadata, attributeName);
                        break;
                    default:
                        if (entityMetadata.MetadataId.HasValue)
                            attributeName = EnsureValidAttributeIdentifier(attributeMetadata.DisplayName?.UserLocalizedLabel?.Label!);
                        break;
                }
            }
            else
            {
                attributeName = attributeMetadata.SchemaName;
            }
            return attributeName;
        }

        /// <summary>
        ///     Gets status code attribute name.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="attributeName">
        ///     Name of the attribute.
        /// </param>
        /// <returns>
        ///     The status code attribute name.
        /// </returns>
        private string GetStatusCodeAttributeName(EntityMetadata entityMetadata, string attributeName)
        {
            if (entityMetadata.MetadataId != null)
            {
                attributeName = entityMetadata.IsCustomEntity.HasValue
                                && (entityMetadata.IsCustomEntity.Value || entityMetadata.SchemaName.Contains("_")) &&
                                !string.IsNullOrEmpty(entityMetadata.DisplayName?.UserLocalizedLabel?.Label)
                    ? string.Format(CultureInfo.CurrentCulture, "{0}StatusReasonCode",
                        EnsureValidEntityIdentifier(entityMetadata.DisplayName?.UserLocalizedLabel?.Label!,
                            entityMetadata.MetadataId.Value))
                    : string.Format(CultureInfo.CurrentCulture, "{0}StatusReasonCode",
                    _updatedEntityNames.ContainsKey(entityMetadata.MetadataId.Value)
                    ? _updatedEntityNames[entityMetadata.MetadataId.Value]
                    : entityMetadata.SchemaName);
            }
            return attributeName;
        }

        /// <summary>
        ///     Gets state code attribute name.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when one or more required arguments are null.
        /// </exception>
        /// <param name="entityMetadata">
        ///     The entity metadata.
        /// </param>
        /// <param name="attributeName">
        ///     Name of the attribute.
        /// </param>
        /// <returns>
        ///     The state code attribute name.
        /// </returns>
        private string GetStateCodeAttributeName(EntityMetadata entityMetadata, string attributeName)
        {
            if (attributeName == null)
            {
                throw new ArgumentNullException(nameof(attributeName));
            }
            if (entityMetadata.MetadataId != null)
            {
                attributeName = entityMetadata.IsCustomEntity.HasValue
                                && (entityMetadata.IsCustomEntity.Value || entityMetadata.SchemaName.Contains("_")) &&
                                !string.IsNullOrEmpty(entityMetadata.DisplayName?.UserLocalizedLabel?.Label)
                    ? string.Format(CultureInfo.CurrentCulture, "{0}StatusCode",
                        EnsureValidEntityIdentifier(entityMetadata.DisplayName?.UserLocalizedLabel?.Label!,
                            entityMetadata.MetadataId.Value))
                    : string.Format(CultureInfo.CurrentCulture, "{0}StatusCode",
                    _updatedEntityNames.ContainsKey(entityMetadata.MetadataId.Value)
                    ? _updatedEntityNames[entityMetadata.MetadataId.Value]
                    : entityMetadata.SchemaName);
            }
            return attributeName;
        }

        /// <summary>
        ///     Ensures that valid attribute identifier.
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
        private static string EnsureValidAttributeIdentifier(string name)
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
        ///     Ensures that valid entity identifier.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <param name="metadataId">
        ///     Identifier for the metadata.
        /// </param>
        /// <returns>
        ///     A string.
        /// </returns>
        private string? EnsureValidEntityIdentifier(string? name, Guid metadataId)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            if (_updatedEntityNames.ContainsKey(metadataId))
            {
                name = _updatedEntityNames[metadataId];
            }
            name = name?.Trim();
            // Check to make sure that the name begins with an alphabet character.
            if (name != null && name.ToCharArray(0, 1)[0] >= '0' && name.ToCharArray(0, 1)[0] <= '9')
            {
                // Prepend to the name if it is not valid.
                name = string.Format(CultureInfo.CurrentCulture, "ClsCompliant{0}", name);
            }
            // Remove special characters.
            if (name == null)
            {
                return name;
            }
            foreach (char c in name.Where(c =>
                         c is not (>= '0' and <= '9' or >= 'a' and <= 'z' or >= 'A' and <= 'Z')))
            {
                name = name.Replace(c.ToString(), string.Empty);
            }
            return name;
        }
    }
}