using System;
using Microsoft.Crm.Services.Utility;
using Microsoft.Xrm.Sdk.Metadata;

[assembly: CLSCompliant(true)]
namespace CrmDeveloperToolkitExtender.CrmSvcUtil
{
    /// <summary>
    ///     Sample extension for the CrmSvcUtil.exe tool that generates early-bound classes for
    ///     custom entities.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 08/03/2022.
    /// </remarks>
    /// <seealso cref="Microsoft.Crm.Services.Utility.ICodeWriterFilterService"/>
    /// <seealso cref="ICodeWriterFilterService"/>
    // ReSharper disable once UnusedType.Global
    public sealed class BasicFilteringService : ICodeWriterFilterService
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BasicFilteringService"/> class.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <param name="defaultService">
        ///     The default service.
        /// </param>
        public BasicFilteringService(ICodeWriterFilterService defaultService)
        {
            DefaultService = defaultService;
        }

        /// <summary>
        ///     Gets the default service.
        /// </summary>
        /// <value>
        ///     The default service.
        /// </value>
        private ICodeWriterFilterService DefaultService { get; }

        /// <summary>
        ///     Generates an attribute.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <param name="attributeMetadata">
        ///     .
        /// </param>
        /// <param name="services">
        ///     .
        /// </param>
        /// <returns>
        ///     Returns <see cref="T:System.Boolean"></see>.
        /// </returns>
        bool ICodeWriterFilterService.GenerateAttribute(AttributeMetadata attributeMetadata, IServiceProvider services)
        {
            return DefaultService.GenerateAttribute(attributeMetadata, services);
        }

        /// <summary>
        ///     Generates an entity.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <param name="entityMetadata">
        ///     .
        /// </param>
        /// <param name="services">
        ///     .
        /// </param>
        /// <returns>
        ///     Returns <see cref="T:System.Boolean"></see>.
        /// </returns>
        bool ICodeWriterFilterService.GenerateEntity(EntityMetadata entityMetadata, IServiceProvider services)
        {
            if (entityMetadata != null && !entityMetadata.IsCustomEntity.GetValueOrDefault()) { return false; }
            return DefaultService.GenerateEntity(entityMetadata, services);
        }

        /// <summary>
        ///     Generates an option.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <param name="optionMetadata">
        ///     .
        /// </param>
        /// <param name="services">
        ///     .
        /// </param>
        /// <returns>
        ///     Returns <see cref="T:System.Boolean"></see>.
        /// </returns>
        bool ICodeWriterFilterService.GenerateOption(OptionMetadata optionMetadata, IServiceProvider services)
        {
            return DefaultService.GenerateOption(optionMetadata, services);
        }

        /// <summary>
        ///     Generates an option set.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <param name="optionSetMetadata">
        ///     .
        /// </param>
        /// <param name="services">
        ///     .
        /// </param>
        /// <returns>
        ///     Returns <see cref="T:System.Boolean"></see>.
        /// </returns>
        bool ICodeWriterFilterService.GenerateOptionSet(OptionSetMetadataBase optionSetMetadata, IServiceProvider services)
        {
            return DefaultService.GenerateOptionSet(optionSetMetadata, services);
        }

        /// <summary>
        ///     Generates a relationship.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <param name="relationshipMetadata">
        ///     .
        /// </param>
        /// <param name="otherEntityMetadata">
        ///     .
        /// </param>
        /// <param name="services">
        ///     .
        /// </param>
        /// <returns>
        ///     Returns <see cref="T:System.Boolean"></see>.
        /// </returns>
        bool ICodeWriterFilterService.GenerateRelationship(RelationshipMetadataBase relationshipMetadata, EntityMetadata otherEntityMetadata, IServiceProvider services)
        {
            return DefaultService.GenerateRelationship(relationshipMetadata, otherEntityMetadata, services);
        }

        /// <summary>
        ///     Generates a service context.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <param name="services">
        ///     .
        /// </param>
        /// <returns>
        ///     Returns <see cref="T:System.Boolean"></see>.
        /// </returns>
        bool ICodeWriterFilterService.GenerateServiceContext(IServiceProvider services)
        {
            return DefaultService.GenerateServiceContext(services);
        }
    }
}