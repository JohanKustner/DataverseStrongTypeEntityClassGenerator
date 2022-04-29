using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using Microsoft.Crm.Services.Utility;
using Microsoft.Xrm.Sdk.Metadata;

namespace CrmDeveloperToolkitExtender.CrmSvcUtil
{
    /// <summary>
    ///     A service for accessing filtering information. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 17/03/2022.
    /// </remarks>
    /// <seealso cref="ICodeWriterFilterService"/>
    // ReSharper disable once UnusedType.Global
    public sealed class FilteringService : ICodeWriterFilterService
    {
        /// <summary>
        ///     The valid entities.
        /// </summary>
        private Collection<string>? _validEntities;

        /// <summary>
        ///     Gets the valid entities.
        /// </summary>
        /// <value>
        ///     The valid entities.
        /// </value>
        private Collection<string>? ValidEntities
        {
            get
            {
                string includedEntities = ConfigurationManager.AppSettings["includedEntities"];
                _validEntities = new Collection<string>();
                foreach (string entity in includedEntities.Split(','))
                {
                    _validEntities.Add(entity);
                }
                for (int i = 0; i < _validEntities.Count; i++)
                {
                    _validEntities[i] = _validEntities[i].ToLower(CultureInfo.CurrentCulture);
                }
                return _validEntities;
            }
        }

        /// <summary>
        ///     Gets the sets the generated option belongs to.
        /// </summary>
        /// <value>
        ///     The generated option sets.
        /// </value>
        private Dictionary<string, bool> GeneratedOptionSets { get; }

        /// <summary>
        ///     Gets the default service.
        /// </summary>
        /// <value>
        ///     The default service.
        /// </value>
        private ICodeWriterFilterService DefaultService { get; }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="defaultService">
        ///     The default service.
        /// </param>
        public FilteringService(ICodeWriterFilterService defaultService)
        {
            DefaultService = defaultService;
            GeneratedOptionSets = new Dictionary<string, bool>();
        }

        /// <summary>
        ///     Returns true to generate code for the OptionSet and false otherwise.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="optionSetMetadata">
        ///     The option set metadata.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     True if it succeeds, false if it fails.
        /// </returns>
        /// <seealso cref="ICodeWriterFilterService.GenerateOptionSet(OptionSetMetadataBase,IServiceProvider)"/>
        public bool GenerateOptionSet(OptionSetMetadataBase optionSetMetadata, IServiceProvider services)
        {
            if (optionSetMetadata.OptionSetType == null)
            {
                return DefaultService.GenerateOptionSet(optionSetMetadata, services);
            }
            switch (optionSetMetadata.OptionSetType.Value)
            {
                case OptionSetType.Boolean:
                    return DefaultService.GenerateOptionSet(optionSetMetadata, services);
                case OptionSetType.Picklist:
                    if (!optionSetMetadata.IsGlobal.HasValue || !optionSetMetadata.IsGlobal.Value)
                    {
                        return true;
                    }
                    if (GeneratedOptionSets.ContainsKey(optionSetMetadata.Name))
                    {
                        return DefaultService.GenerateOptionSet(optionSetMetadata, services);
                    }
                    GeneratedOptionSets[optionSetMetadata.Name] = true;
                    return true;
                case OptionSetType.State:
                    return DefaultService.GenerateOptionSet(optionSetMetadata, services);
                case OptionSetType.Status:
                    return true;
                default:
                    return DefaultService.GenerateOptionSet(optionSetMetadata, services);
            }
        }

        /// <summary>
        ///     Returns true to generate code for the Attribute and false otherwise.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="attributeMetadata">
        ///     The attribute metadata.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     True if it succeeds, false if it fails.
        /// </returns>
        /// <seealso cref="ICodeWriterFilterService.GenerateAttribute(AttributeMetadata,IServiceProvider)"/>
        public bool GenerateAttribute(AttributeMetadata attributeMetadata, IServiceProvider services)
        {
            return attributeMetadata.AttributeType is AttributeTypeCode.Picklist or AttributeTypeCode.State or AttributeTypeCode.Status || DefaultService.GenerateAttribute(attributeMetadata, services);
        }

        /// <summary>
        ///     Returns true to generate code for the Entity and false otherwise.
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
        ///     True if it succeeds, false if it fails.
        /// </returns>
        /// <seealso cref="ICodeWriterFilterService.GenerateEntity(EntityMetadata,IServiceProvider)"/>
        public bool GenerateEntity(EntityMetadata entityMetadata, IServiceProvider services)
        {
            if (ValidEntities?[0] == "*")
            {
                return DefaultService.GenerateEntity(entityMetadata, services);
            }
            return ValidEntities != null && ValidEntities.Contains(entityMetadata.LogicalName.ToLower(CultureInfo.CurrentCulture));
        }

        /// <summary>
        ///     Returns true to generate code for the 1:N, N:N, or N:1 relationship and false otherwise.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="relationshipMetadata">
        ///     The relationship metadata.
        /// </param>
        /// <param name="otherEntityMetadata">
        ///     The other entity metadata.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     True if it succeeds, false if it fails.
        /// </returns>
        /// <seealso cref="ICodeWriterFilterService.GenerateRelationship(RelationshipMetadataBase,EntityMetadata,IServiceProvider)"/>
        public bool GenerateRelationship(RelationshipMetadataBase relationshipMetadata, EntityMetadata otherEntityMetadata, IServiceProvider services)
        {
            return DefaultService.GenerateRelationship(relationshipMetadata, otherEntityMetadata, services);
        }

        /// <summary>
        ///     Returns true to generate code for the data context and false otherwise.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     True if it succeeds, false if it fails.
        /// </returns>
        /// <seealso cref="ICodeWriterFilterService.GenerateServiceContext(IServiceProvider)"/>
        public bool GenerateServiceContext(IServiceProvider services)
        {
            return DefaultService.GenerateServiceContext(services);
        }

        /// <summary>
        ///     Returns true to generate code for the Option and false otherwise.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="optionMetadata">
        ///     The option metadata.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     True if it succeeds, false if it fails.
        /// </returns>
        /// <seealso cref="ICodeWriterFilterService.GenerateOption(OptionMetadata,IServiceProvider)"/>
        public bool GenerateOption(OptionMetadata optionMetadata, IServiceProvider services)
        {
            return DefaultService.GenerateOption(optionMetadata, services);
        }
    }
}