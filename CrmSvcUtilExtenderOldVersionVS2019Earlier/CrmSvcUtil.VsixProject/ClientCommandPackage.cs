//------------------------------------------------------------------------------
// <copyright file="ClientCommandPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace CrmSvcUtil.VsixProject
{
    /// <summary>
    ///     This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The minimum requirement for a class to be considered a valid package for Visual Studio is
    ///     to implement the IVsPackage interface and register itself with the shell. This package
    ///     uses the helper classes defined inside the Managed Package Framework (MPF)
    ///     to do it: it derives from the Package class that provides the implementation of the
    ///     IVsPackage interface and uses the registration attributes defined in the framework to
    ///     register itself and its components with the shell. These attributes tell the pkgdef
    ///     creation utility what data to put into .pkgdef file.
    ///     </para>
    ///     <para>
    ///     To get loaded into VS, the package must be referred by &lt;Asset
    ///     Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Microsoft.VisualStudio.Shell.Package"/>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [CLSCompliant(false)]
    public sealed class ClientCommandPackage : Package
    {
        /// <summary>
        ///     (Immutable)
        ///     ClientCommandPackage GUID string.
        /// </summary>
        private const string PackageGuidString = "1e660252-7794-446e-957f-98909aa5656e";

        #region Package Members

        /// <summary>
        ///     Initialization of the package; this method is called right after the package is sited, so
        ///     this is the place where you can put all the initialization code that rely on services
        ///     provided by VisualStudio.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        protected override void Initialize()
        {
            ClientCommand.Initialize(this);
            base.Initialize();
        }

        #endregion
    }
}
