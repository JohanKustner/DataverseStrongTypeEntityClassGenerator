using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Constants = EnvDTE.Constants;

namespace CrmSvcUtil.Client
{
    /// <summary>
    ///     Project Helper.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 07/03/2022.
    /// </remarks>
    public static class ProjectHelper
    {
        /// <summary>
        ///     (Immutable)
        ///     The solution.
        /// </summary>
        [SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        private static readonly IVsSolution Solution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));

        /// <summary>
        ///     Gets the projects.
        /// </summary>
        /// <value>
        ///     The projects.
        /// </value>
        [CLSCompliant(false)]
        [SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        public static IEnumerable<Project>? Projects
        {
            get
            {
                return GetProjectsInSolution().Select(GetDteProject).Where(project => project != null && project.Kind != Constants.vsProjectKindSolutionItems);
            }
        }

        /// <summary>
        ///     Gets the projects in solution.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="flags">
        ///     (Optional) The flags.
        /// </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process the projects in solutions in this
        ///     collection.
        /// </returns>
        [SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        private static IEnumerable<IVsHierarchy> GetProjectsInSolution(__VSENUMPROJFLAGS flags = __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION)
        {
            Guid guid = Guid.Empty;
            Solution.GetProjectEnum((uint)flags, ref guid, out IEnumHierarchies enumHierarchies);
            if (enumHierarchies == null)
            {
                yield break;
            }

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            while (enumHierarchies.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (hierarchy.Length > 0)
                {
                    yield return hierarchy[0];
                }
            }
        }

        /// <summary>
        ///     Gets the DTE project.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     hierarchy.
        /// </exception>
        /// <param name="hierarchy">
        ///     The hierarchy.
        /// </param>
        /// <returns>
        ///     The DTE project.
        /// </returns>
        [SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "<Pending>")]
        private static Project GetDteProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object obj);
            return (obj as Project)!;
        }
    }
}
