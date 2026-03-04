using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.ComponentModel;
using System.Threading;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;
using Siemens.Engineering;
using Siemens.Engineering.AddIn;
using Siemens.Engineering.AddIn.Menu;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.Hmi.Tag;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW.Types;
using Siemens.Engineering.SW.ExternalSources;
using Siemens.Engineering.SW.Alarm;
using Siemens.Engineering.SW.OpcUa;
using Siemens.Engineering.SW.Units;
using AddInOPCUAInterface.UI;
using AddInOpcUaInterface.Phases;
using AddInOpcUaInterface.Other;
using AddInOpcUaInterface.Phases.Phase4;
using AddInOPCUAInterface.UI.Actions;

namespace AddInOpcUaInterface
{
    /// <summary>
    /// This class contains your AddIn.
    /// Its providing entries for the context menu in TIA Portal
    /// Your AddIn can contain sub menus and one or multiple functions a user can call
    /// </summary>
    public class AddIn : ContextMenuAddIn
    {
        //Private variable referencing the TIA Portal object to get additional information for your add-in if necessary
        private readonly TiaPortal _tiaPortal;

        /// <summary>
        /// The string pasted to the base constructor defines the Label of the context menu entry on root level
        /// </summary>
        /// <param name="tiaPortal"></param>
        public AddIn(TiaPortal tiaPortal) : base("OPC UA Default Interface Add-In")
        {
            _tiaPortal = tiaPortal;
            //Its recommended to keep the contructor clean. TIA Portal has a very short timeout for loading add-ins.
        }

        /// <summary>
        /// In this method you can define the menu entries with the methods to be called.
        /// The hierarchie of the AddIn with sub menus can be defined.
        /// </summary>
        /// <param name="addInRootSubmenu">Add-In are in the context menu</param>
        protected override void BuildContextMenuItems(ContextMenuAddInRoot addInRootSubmenu)
        {
            addInRootSubmenu.Items.AddActionItem<DeviceItem>("Create", OnClickCreate);
            addInRootSubmenu.Items.AddActionItem<DeviceItem>("Extend Create", OnClickExtendCreate);
            addInRootSubmenu.Items.AddActionItem<PlcUnit>("Create server interface for SW Unit", OnClickSWUnitCreate);
        }

        /// <summary>
        /// Create method. Generates a "default" user modelled interface.
        /// </summary>
        /// <param name="menuSelectionProvider"></param>
        private void OnClickCreate(MenuSelectionProvider<DeviceItem> menuSelectionProvider)
        {
            using (var ctx = new AddInExecutionContext())
            {
                try
                {
                    using (ExclusiveAccess exclusiveAccess = _tiaPortal.ExclusiveAccess())
                    {
                        // Set the exclusiveAccess variable to display messages
                        DisplayMessage.SetExclusiveAccess(exclusiveAccess);

                        // Access fields in the TIA Portal project (selectedDevice, softwareContainer, etc.)
                        ctx.AccessTIAFields(menuSelectionProvider);
                    }
                    CreateWindow parametrizationWindow = new CreateWindow();
                    // Read the names of existing server interfaces: prevents users from creating a new server interface with a name that is already in use
                    var interfaceResult = ServerInterfaces.InterfaceNames(ctx.GetServerInterfaces(), ctx.GetSimaticInterfaces());
                    UserInputFields.ExistingInterfaceNames = interfaceResult.Names;
                    UserInputFields.NewestInterface = interfaceResult.NewestInterface;

                    // Open parametrization window for user input
                    parametrizationWindow.ShowDialog();

                    // Checks if the process has been cancelled by the user
                    bool stop = UserInputFields.StopApplication;
                    if (stop) { Environment.Exit(0); }

                    // Get data inserted by the user
                    ctx.InterfaceName = UserInputFields.InterfaceName;
                    ctx.InterfaceURI = UserInputFields.URI;
                    ctx.FilePath = UserInputFields.Path;

                    // Add-In program
                    using (ExclusiveAccess exclusiveAccess = _tiaPortal.ExclusiveAccess())
                    {
                        DisplayMessage.SetExclusiveAccess(exclusiveAccess);
                        RunAddIn(exclusiveAccess);
                    }
                }
                catch (Exception exception)
                {
                    DisplayMessage.ErrorMessage($@"Unexpected error ocurred during the execution of the Add-In: {exception}");
                }
            }
        }

        /// <summary>
        /// Extend create method. Generates a "custom" user modelled interface.
        /// </summary>
        /// <param name="menuSelectionProvider"></param>
        private void OnClickExtendCreate(MenuSelectionProvider<DeviceItem> menuSelectionProvider)
        {
            using (var ctx = new AddInExecutionContext())
            {
                try
                {
                    using (ExclusiveAccess exclusiveAccess = _tiaPortal.ExclusiveAccess())
                    {
                        // Set the exclusiveAccess variable to display messages
                        DisplayMessage.SetExclusiveAccess(exclusiveAccess);

                        // Access fields in the TIA Portal project (selectedDevice, softwareContainer, etc.)
                        ctx.AccessTIAFields(menuSelectionProvider);
                    }
                    ExtendCreateWindow parametrizationWindow = new ExtendCreateWindow();
                    // Read the names of existing server interfaces: prevents users from creating a new server interface with a name that is already in use
                    var interfaceResult = ServerInterfaces.InterfaceNames(ctx.GetServerInterfaces(), ctx.GetSimaticInterfaces());
                    UserInputFields.ExistingInterfaceNames = interfaceResult.Names;
                    UserInputFields.NewestInterface = interfaceResult.NewestInterface;

                    // Open parametrization window for user input
                    parametrizationWindow.ShowDialog();

                    // Checks if the process has been cancelled by the user
                    bool stop = UserInputFields.StopApplication;
                    if (stop) { Environment.Exit(0); }

                    // Get data inserted by the user
                    ctx.InterfaceName = UserInputFields.InterfaceName;
                    ctx.InterfaceURI = UserInputFields.URI;
                    ctx.FilePath = UserInputFields.Path;
                    ctx.KeepEmptyDBs = UserInputFields.KeepEmptyDBs;
                    ctx.KeepFolderStructure = UserInputFields.KeepFolderStructure;
                    ctx.InputsAccessLevel = UserInputFields.InputsAccessLevel;
                    ctx.MemoryAccessLevel = UserInputFields.MemoryAccessLevel;
                    ctx.OutputsAccessLevel = UserInputFields.OutputsAccessLevel;
                    ctx.CountersAccessLevel = UserInputFields.CountersAccessLevel;
                    ctx.TimersAccessLevel = UserInputFields.TimersAccessLevel;
                    ctx.GlobalDBsAccessLevel = UserInputFields.GlobalDBsAccessLevel;
                    ctx.InstanceDBsAccessLevel = UserInputFields.InstanceDBsAccessLevel;
                    ctx.SafetyGlobalDBsAccessLevel = UserInputFields.SafetyGlobalDBsAccessLevel;
                    ctx.SafetyInstanceDBsAccessLevel = UserInputFields.SafetyInstanceDBsAccessLevel;

                    // Add-In program
                    using (ExclusiveAccess exclusiveAccess = _tiaPortal.ExclusiveAccess())
                    {
                        DisplayMessage.SetExclusiveAccess(exclusiveAccess);
                        RunAddIn(exclusiveAccess);
                    }
                }
                catch (Exception exception)
                {
                    DisplayMessage.ErrorMessage($@"Unexpected error ocurred during the execution of the Add-In: {exception}");
                }
            }
        }

        /// <summary>
        /// Create method for Software Unit. Generates a "default" user modelled interface for the selected Software Unit.
        /// </summary>
        /// <param name="menuSelectionProvider"></param>
        private void OnClickSWUnitCreate(MenuSelectionProvider<PlcUnit> menuSelectionProvider)
        {
            using (var ctx = new AddInExecutionContext())
            {
                try
                {
                    using (ExclusiveAccess exclusiveAccess = _tiaPortal.ExclusiveAccess())
                    {
                        // Set the exclusiveAccess variable to display messages
                        DisplayMessage.SetExclusiveAccess(exclusiveAccess);

                        // Access fields in the TIA Portal project (selectedDevice, softwareContainer, etc.)
                        ctx.AccessSoftwareUnitFields(menuSelectionProvider);
                    }
                    CreateWindow parametrizationWindow = new CreateWindow();
                    // Read the names of existing server interfaces: prevents users from creating a new server interface with a name that is already in use
                    var interfaceResult = ServerInterfaces.InterfaceNames(ctx.GetServerInterfaces(), ctx.GetSimaticInterfaces());
                    UserInputFields.ExistingInterfaceNames = interfaceResult.Names;
                    UserInputFields.NewestInterface = interfaceResult.NewestInterface;

                    // Open parametrization window for user input
                    parametrizationWindow.ShowDialog();

                    // Checks if the process has been cancelled by the user
                    bool stop = UserInputFields.StopApplication;
                    if (stop) { Environment.Exit(0); }

                    // Get data inserted by the user
                    ctx.InterfaceName = UserInputFields.InterfaceName;
                    ctx.InterfaceURI = UserInputFields.URI;
                    ctx.FilePath = UserInputFields.Path;

                    // Add-In program
                    using (ExclusiveAccess exclusiveAccess = _tiaPortal.ExclusiveAccess())
                    {
                        DisplayMessage.SetExclusiveAccess(exclusiveAccess);
                        RunAddIn(exclusiveAccess);
                    }
                }
                catch (Exception exception)
                {
                    DisplayMessage.ErrorMessage($@"Unexpected error ocurred during the execution of the Add-In: {exception}");
                }
            }
        }

        /// <summary>
        /// Executes the five phases required to construct the server interface.
        /// </summary>
        private void RunAddIn(ExclusiveAccess exclusiveAccess)
        {
            var ctx = AddInExecutionContext.Current;

            // Start timer to measure performance of the Add-In
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Create a log file to record logs (track variables that have not been added into the server interface)
            LogMessages.CreateLogFile();

            #region PHASE 1: IMPORT THE XML TEMPLATE OF THE SERVER INTERFACE AS AN XDOCUMENT

            Stream file = this.GetType().Assembly.GetManifestResourceStream("AddInOpcUaInterface.Phases.Phase1.InterfaceTemplate.xml");
            InterfaceTemplate.ImportTemplate(file);

            // Count the number of interface elements included with the template
            ctx.NumberDefaultNodes = InterfaceTemplate.GetTotalInterfaceElements();
            #endregion

            if (exclusiveAccess.IsCancellationRequested) { OperationCancelled(exclusiveAccess); }

            #region PHASE 2: ADD DATA TYPES (DT) TO THE SERVER INTERFACE (USER DT, SYSTEM DT, PROGRAM RESOURCES)
            // Read user constants (in case they are used to define array's lengths)
            UserConstants.GetUserConstants(ctx.GetTagTableGroup());

            exclusiveAccess.Text = $@"Adding ""User Data Types"" and ""System Data Types"" to the server interface...";

            // User DT and System DT from the device
            UserSystemDataTypes.GetUserSystemDataTypeElements(ctx.GetTypeGroup(), false);

            // User DT and System DT from the SoftwareUnit
            if (ctx.IsSoftwareUnit) { UserSystemDataTypes.GetUserSystemDataTypeElements(ctx.GetSoftwareUnitTypeGroup(), true); }

            // Add the new User DT and System DT to the root element
            ctx.OpcUaInterface.Root.Add(UserSystemDataTypes.XElementUserSystemDataTypes);

            // Count the number of new interface elements
            ctx.NumberUserSystemDataTypes = UserSystemDataTypes.XElementUserSystemDataTypes.Count;
            #endregion

            if (exclusiveAccess.IsCancellationRequested) { OperationCancelled(exclusiveAccess); }

            #region PHASE 3: ADD TAGS FROM TAGTABLES TO THE SERVER INTERFACE

            exclusiveAccess.Text = $@"Adding ""Tags"" from tag tables to the server interface...";
            Tags.ResetTagElements();
            Tags.GetTagElements(ctx.GetTagTableGroup());

            // Add the new tag variables to the root element
            ctx.OpcUaInterface.Root.Add(Tags.XElementTags);

            // Number of interface elements added as tags
            ctx.NumberTags = Tags.XElementTags.Count;
            #endregion

            if (exclusiveAccess.IsCancellationRequested) { OperationCancelled(exclusiveAccess); }

            #region PHASE 4: ADD DATA BLOCKS TO THE SERVER INTERFACE

            BuildDataBlockElements.ResetDatablocksElements();

            exclusiveAccess.Text = $@"Adding ""Global"" DBs to the server interface... Count:";
            DataBlocksGlobal.ResetDatablockElements();
            DataBlocksGlobal.GetDatablockElements(ctx.GetBlockGroup());

            // Number of Global DB elements added as nodes
            ctx.NumberGlobalDBs = BuildDataBlockElements.XElementDataBlocks.Count;

            if (exclusiveAccess.IsCancellationRequested) { OperationCancelled(exclusiveAccess); }

            exclusiveAccess.Text = $@"Adding ""Instance"" DBs to the server interface... Count:";
            DataBlocksInstance.ResetDatablockElements();
            DataBlocksInstance.GetDatablockElements(ctx.GetBlockGroup());

            // Number of Instance DB elements added as nodes
            ctx.NumberInstanceDBs = BuildDataBlockElements.XElementDataBlocks.Count - ctx.NumberGlobalDBs;

            // Add folders and variables to the root element (from both Global and Instance DBs)
            ctx.OpcUaInterface.Root.Add(BuildDataBlockElements.XElementDataBlocks);
            #endregion

            if (exclusiveAccess.IsCancellationRequested) { OperationCancelled(exclusiveAccess); }

            #region PHASE 5: EXPORT THE SERVER INTERFACE AS AN XML FILE AND IMPORT IT INTO THE PROJECT

            // Export the server interface to the filePath        
            ctx.OpcUaInterface.Save(ctx.FilePath);

            exclusiveAccess.Text = $@"Importing the server interface into the project...";

            // Import the server interface into TIA Portal
            ImportServerInterface.Import(stopwatch);
            #endregion

        }

        /// <summary>
        /// Exits the program's execution if the user cancels the operation at any given time.
        /// </summary>
        /// <param name="exclusiveAccess"></param>
        private void OperationCancelled(ExclusiveAccess exclusiveAccess)
        {
            exclusiveAccess.Text = $@"Operation canceled...";
            LogMessages.PublishLog($@"The operation has been canceled.");
            Thread.Sleep(2000);
            Environment.Exit(0);
        }
    }
}



