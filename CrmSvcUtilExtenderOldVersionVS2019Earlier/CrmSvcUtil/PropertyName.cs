using System;

namespace CrmDeveloperToolkitExtender.CrmSvcUtil
{
    /// <summary>
    ///     A property name.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 08/04/2022.
    /// </remarks>
    internal class PropertyName
    {
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/04/2022.
        /// </remarks>
        /// <param name="entityMetadataId">
        ///     The identifier of the entity metadata.
        /// </param>
        /// <param name="attributeMetadataId">
        ///     The identifier of the attribute metadata.
        /// </param>
        /// <param name="attributeName">
        ///     The name of the attribute.
        /// </param>
        internal PropertyName(Guid? entityMetadataId, Guid attributeMetadataId, string attributeName)
        {
            EntityMetadataId = entityMetadataId;
            AttributeMetadataId = attributeMetadataId;
            AttributeName = attributeName;
        }

        /// <summary>
        ///     Gets or sets the identifier of the entity metadata.
        /// </summary>
        /// <value>
        ///     The identifier of the entity metadata.
        /// </value>
        internal Guid? EntityMetadataId { get; set; }

        /// <summary>
        ///     Gets or sets the identifier of the attribute metadata.
        /// </summary>
        /// <value>
        ///     The identifier of the attribute metadata.
        /// </value>
        internal Guid AttributeMetadataId { get; }

        /// <summary>
        ///     Gets or sets the name of the attribute.
        /// </summary>
        /// <value>
        ///     The name of the attribute.
        /// </value>
        internal string AttributeName { get; set; }
    }
}
