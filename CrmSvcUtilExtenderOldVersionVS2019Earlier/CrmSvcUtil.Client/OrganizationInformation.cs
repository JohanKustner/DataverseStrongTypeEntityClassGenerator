#nullable enable
using System;
using Microsoft.Xrm.Sdk.Discovery;

namespace CrmSvcUtil.Client
{
  /// <summary>
  ///   Information about the organization.
  /// </summary>
  /// <remarks>
  ///   Johan Küstner, 07/03/2022.
  /// </remarks>
  public class OrganizationInformation
  {
    /// <summary>
    ///   Gets or sets the identifier of the organization.
    /// </summary>
    /// <value>
    ///   The identifier of the organization.
    /// </value>
    public Guid OrganizationId { get; set; }

    /// <summary>
    ///   Gets or sets the name of the friendly.
    /// </summary>
    /// <value>
    ///   The name of the friendly.
    /// </value>
    public string? FriendlyName { get; set; }

    /// <summary>
    ///   Gets or sets the organization version.
    /// </summary>
    /// <value>
    ///   The organization version.
    /// </value>
    public string? OrganizationVersion { get; set; }

    /// <summary>
    ///   Gets or sets the identifier of the environment.
    /// </summary>
    /// <value>
    ///   The identifier of the environment.
    /// </value>
    public string? EnvironmentId { get; set; }

    /// <summary>
    ///   Gets or sets the identifier of the datacenter.
    /// </summary>
    /// <value>
    ///   The identifier of the datacenter.
    /// </value>
    public Guid DatacenterId { get; set; }

    /// <summary>
    ///   Gets or sets the geo.
    /// </summary>
    /// <value>
    ///   The geo.
    /// </value>
    public string? Geo { get; set; }

    /// <summary>
    ///   Gets or sets the identifier of the tenant.
    /// </summary>
    /// <value>
    ///   The identifier of the tenant.
    /// </value>
    public string? TenantId { get; set; }

    /// <summary>
    ///   Gets or sets the name of the URL.
    /// </summary>
    /// <value>
    ///   The name of the URL.
    /// </value>
    public string? UrlName { get; set; }

    /// <summary>
    ///   Gets or sets a unique name.
    /// </summary>
    /// <value>
    ///   The name of the unique.
    /// </value>
    public string? UniqueName { get; set; }

    /// <summary>
    ///   Gets or sets the state.
    /// </summary>
    /// <value>
    ///   The state.
    /// </value>
    public OrganizationState State { get; set; }

    /// <summary>
    ///   Gets or sets the web application endpoint.
    /// </summary>
    /// <value>
    ///   The web application endpoint.
    /// </value>
    public string? WebApplicationEndpoint { get; set; }

    /// <summary>
    ///   Gets or sets the organization service.
    /// </summary>
    /// <value>
    ///   The organization service.
    /// </value>
    public string? OrganizationService { get; set; }

    /// <summary>
    ///   Gets or sets the organization data service.
    /// </summary>
    /// <value>
    ///   The organization data service.
    /// </value>
    public string? OrganizationDataService { get; set; }
  }
}
