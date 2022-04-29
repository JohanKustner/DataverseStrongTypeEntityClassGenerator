//------------------------------------------------------------------------------
// <copyright file="ClientCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using CrmSvcUtil.Client.LogOnWindow;
using Microsoft.VisualStudio.Shell;

namespace CrmSvcUtil.VsixProject
{
    /// <summary>
    ///     Command handler.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 08/03/2022.
    /// </remarks>
    internal sealed class ClientCommand
    {
        /// <summary>
        ///     (Immutable)
        ///     Command ID.
        /// </summary>
        private const int CommandId = 0x0100;

        /// <summary>
        ///     (Immutable)
        ///     Command menu group (command set GUID).
        /// </summary>
        private static readonly Guid CommandSet = new("1f95cbbb-de91-4bc4-aa51-dd0fdc333e13");

        /// <summary>
        ///     (Immutable)
        ///     VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ClientCommand"/> class. Adds our command
        ///     handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when one or more required arguments are null.
        /// </exception>
        /// <param name="package">
        ///     Owner package, not null.
        /// </param>
        private ClientCommand(Package package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is not OleMenuCommandService commandService)
            {
                return;
            }
            CommandID menuCommandId = new(CommandSet, CommandId);
            MenuCommand menuItem = new(MenuItemCallback, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        ///     Gets the service provider from the owner package.
        /// </summary>
        /// <value>
        ///     The service provider.
        /// </value>
        private IServiceProvider ServiceProvider => _package;

        /// <summary>
        ///     Initializes the singleton instance of the command.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <param name="package">
        ///     Owner package, not null.
        /// </param>
        public static void Initialize(Package package)
        {
            _ = new ClientCommand(package);
        }

        /// <summary>
        ///     This function is the callback used to execute the command when the menu item is clicked.
        ///     See the constructor to see how the menu item is associated with this function using
        ///     OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 08/03/2022.
        /// </remarks>
        /// <param name="sender">
        ///     Event sender.
        /// </param>
        /// <param name="e">
        ///     Event args.
        /// </param>
        private static void MenuItemCallback(object sender, EventArgs e)
        {
            CrmLogOnForm crmLogOnForm = new();
            crmLogOnForm.Show();
        }
    }
}
