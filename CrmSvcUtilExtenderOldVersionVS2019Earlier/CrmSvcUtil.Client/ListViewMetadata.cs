#nullable enable
namespace CrmSvcUtil.Client
{
    /// <summary>
    ///     List View Metadata.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 07/03/2022.
    /// </remarks>
    public class ListViewMetadata
    {
        /// <summary>
        ///     Gets or sets the name of the friendly.
        /// </summary>
        /// <value>
        ///     The name of the friendly.
        /// </value>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string? FriendlyName { get; set; }

        /// <summary>
        ///     Gets or sets the name of the unique.
        /// </summary>
        /// <value>
        ///     The name of the unique.
        /// </value>
        public string? UniqueName { get; set; }
    }
}
