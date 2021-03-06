﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ComponentModelHost;
using System.ComponentModel.Composition.Hosting;

namespace VsExplorer
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(DocumentBufferViewToolWindow))]
    [ProvideToolWindow(typeof(DocumentTreeViewToolWindow))]
    [ProvideToolWindow(typeof(DocumentTagToolWindow))]
    [Guid(GuidList.guidVsExplorerPkgString)]
    public sealed class VsExplorerPackage : Package
    {
        private IComponentModel _componentModel;
        private ExportProvider _exportProvider;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VsExplorerPackage()
        {

        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            _componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            _exportProvider = _componentModel.DefaultExportProvider;

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                AddDisplayToolWindow<DocumentBufferViewToolWindow>(commandService, GuidList.guidVsExplorerCmdSet, PackageCommands.DisplayDocumentBufferView);
                AddDisplayToolWindow<DocumentTreeViewToolWindow>(commandService, GuidList.guidVsExplorerCmdSet, PackageCommands.DisplayDocumentTreeView);
                AddDisplayToolWindow<DocumentTagToolWindow>(commandService, GuidList.guidVsExplorerCmdSet, PackageCommands.DisplayDocumentTagView);
            }
        }

        private void AddDisplayToolWindow<T>(OleMenuCommandService commandService, Guid commandSet, int id)
            where T : ToolWindowPane
        {
            var commandID = new CommandID(commandSet, id);
            var menuCommand = new MenuCommand((sender, e) => ShowToolWindow<T>(), commandID);
            commandService.AddCommand(menuCommand);
        }

        private void ShowToolWindow<T>()
            where T : ToolWindowPane
        {
            ToolWindowPane window = this.FindToolWindow(typeof(T), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
