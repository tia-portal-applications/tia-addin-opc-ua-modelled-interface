using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.AddIn.Menu;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.OpcUa;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW.Types;
using Siemens.Engineering.SW.Units;

namespace AddInOpcUaInterface.Other
{
    /// <summary>
    /// Scoped ambient context that holds all per-execution state.
    /// Engineering objects are instance members (not static fields), satisfying the TIA Portal V20+ requirement.
    /// Access from anywhere via <see cref="Current"/> without parameter drilling.
    /// Wrap each Add-In execution in a <c>using (new AddInExecutionContext())</c> block.
    /// </summary>
    internal class AddInExecutionContext : IDisposable
    {
        [ThreadStatic]
        private static AddInExecutionContext _current;

        /// <summary>
        /// Gets the current execution context. Throws if no context is active.
        /// </summary>
        public static AddInExecutionContext Current =>
            _current ?? throw new InvalidOperationException("No active AddIn execution context. Ensure code runs within a 'using (new AddInExecutionContext())' block.");

        #region Project Fields (formerly ProjectFields)

        // Engineering objects stored as 'object' to satisfy TIA Portal V20+ Publisher constraints.
        // The Publisher forbids fields/properties typed as Siemens engineering objects.
        // Typed getter methods are provided below.
        private object _selectedDevice;
        private object _softwareContainer;
        private object _software;
        private object _provider;
        private object _commGroup;
        private object _serverInterfaceGroup;
        private object _serverInterfaces;
        private object _simaticInterfaces;
        private object _systemBlockGroupComposition;
        private object _tagTableGroup;
        private object _blockGroup;
        private object _typeGroup;

        // Software Units
        public bool IsSoftwareUnit;
        private object _selectedSoftwareUnit;
        private object _softwareUnitTypeGroup;
        public string SoftwareUnitNamespace;

        // Typed getter methods for engineering objects
        public DeviceItem GetSelectedDevice() => (DeviceItem)_selectedDevice;
        public SoftwareContainer GetSoftwareContainer() => (SoftwareContainer)_softwareContainer;
        public PlcSoftware GetSoftware() => (PlcSoftware)_software;
        public OpcUaProvider GetProvider() => (OpcUaProvider)_provider;
        public OpcUaCommunicationGroup GetCommGroup() => (OpcUaCommunicationGroup)_commGroup;
        public ServerInterfaceGroup GetServerInterfaceGroup() => (ServerInterfaceGroup)_serverInterfaceGroup;
        public ServerInterfaceComposition GetServerInterfaces() => (ServerInterfaceComposition)_serverInterfaces;
        public SimaticInterfaceComposition GetSimaticInterfaces() => (SimaticInterfaceComposition)_simaticInterfaces;
        public PlcSystemBlockGroupComposition GetSystemBlockGroupComposition() => (PlcSystemBlockGroupComposition)_systemBlockGroupComposition;
        public PlcTagTableGroup GetTagTableGroup() => (PlcTagTableGroup)_tagTableGroup;
        public PlcBlockGroup GetBlockGroup() => (PlcBlockGroup)_blockGroup;
        public PlcTypeSystemGroup GetTypeGroup() => (PlcTypeSystemGroup)_typeGroup;
        public PlcUnit GetSelectedSoftwareUnit() => (PlcUnit)_selectedSoftwareUnit;
        public PlcTypeSystemGroup GetSoftwareUnitTypeGroup() => (PlcTypeSystemGroup)_softwareUnitTypeGroup;

        #endregion

        #region AddIn Fields (formerly AddInFields)

        // Interface name and URI
        public string InterfaceName;
        public string InterfaceURI;
        public string FilePath;

        // XDocument of the server interface
        public XDocument OpcUaInterface;             // XML document to build the server interface
        public XNamespace RootNameSpace;             // Namespace of the OPC Foundation 
        public XNamespace RootNameSpaceSi;           // Namespace used to map nodes with project variables

        // Variables for the Access Level filter:
        // Possible values: "Not Accessible" "Read only" "Write only" "Read Write" "Project's access levels"
        public Dictionary<int, string> AccessLevelDictionary = new Dictionary<int, string>
        {
            { 0, "Not Accessible" },
            { 1, "Read only" },
            { 2, "Write only" },
            { 3, "Read Write" },
            { 4, "Project's access levels" }
        };

        // Default access level values for the "Create" option
        public int InputsAccessLevel = 4;    // Access level for input variables
        public int MemoryAccessLevel = 4;    // Access level for memory variables
        public int OutputsAccessLevel = 4;    // Access level for output variables
        public int CountersAccessLevel = 4;    // Access level for counters
        public int TimersAccessLevel = 4;    // Access level for timers
        public int GlobalDBsAccessLevel = 4;    // Access level for Global DB variables
        public int InstanceDBsAccessLevel = 4;    // Access level for Instance DB variables
        public int SafetyGlobalDBsAccessLevel = 1;    // Access level for Safety Global DB variables
        public int SafetyInstanceDBsAccessLevel = 1;    // Access level for Safety Instance DB variables

        // Other settings for "Extend Create"
        public string NodeIdentifier = "String";     // Use "string"/"numeric" node identifiers
        public bool OptimizedData = false;        // Use "Not optimized"/"optimized" server interface
        public bool KeepEmptyDBs = false;        // Remove empty Data Blocks
        public bool KeepFolderStructure = false;        // Keep the folder structure present in the project

        // Number of nodes added to the server interface
        public int NumberDefaultNodes = 0;     // Number of nodes imported from the InterfaceTemplate.xml
        public int NumberUserSystemDataTypes = 0;     // Number of User and System Data Types defined in the project
        public int NumberTags = 0;     // Number of tags defined on all Tag Tables
        public int NumberGlobalDBs = 0;     // Number of nodes (folders, data blocks, variables) associated with Global DBs
        public int NumberInstanceDBs = 0;     // Number of nodes (folders, data blocks, variables) associated with Instance DBs

        #endregion

        /// <summary>
        /// Creates a new execution context and sets it as the ambient current context.
        /// </summary>
        public AddInExecutionContext()
        {
            _current = this;
        }

        /// <summary>
        /// Retrieves all relevant fields from the TIA Portal project.
        /// </summary>
        /// <param name="menuSelectionProvider">Device (PLC) selected when calling the Add-In.</param>
        public void AccessTIAFields(MenuSelectionProvider<DeviceItem> menuSelectionProvider)
        {
            // Define the device selected from the project tree
            _selectedDevice = menuSelectionProvider.GetSelection<DeviceItem>().FirstOrDefault();

            // Creates the Software Container and PlcSoftware
            _softwareContainer =
            ((IEngineeringServiceProvider)GetSelectedDevice()).GetService<SoftwareContainer>();
            if (_softwareContainer != null)
            {
                _software = ((SoftwareContainer)_softwareContainer).Software as PlcSoftware;
                _provider = GetSoftware().GetService<OpcUaProvider>();

                if (_provider != null)
                {
                    _commGroup = GetProvider().CommunicationGroup;
                    _serverInterfaceGroup = GetCommGroup().ServerInterfaceGroup;
                    _serverInterfaces = GetServerInterfaceGroup().ServerInterfaces;       // Note: Server Interfaces are blue
                    _simaticInterfaces = GetServerInterfaceGroup().SimaticInterfaces;     // Note: Simatic Interfaces are purple
                    if (_serverInterfaces != null)
                    {
                        // Discover all available Tag Tables, Data Types and Blocks
                        _tagTableGroup = GetSoftware().TagTableGroup;
                        _blockGroup = GetSoftware().BlockGroup;
                        _typeGroup = GetSoftware().TypeGroup;

                        // System blocks: Program Resources and S7 Safety
                        _systemBlockGroupComposition = GetSoftware().BlockGroup.SystemBlockGroups;
                    }
                    else
                    {
                        DisplayMessage.ErrorMessage("The Add-In cannot be executed on the selected device.");
                    }
                }
                else
                {
                    try
                    {
                        DisplayCustomErrorMessage();
                    }
                    catch
                    {
                        DisplayMessage.ErrorMessage("The Add-In cannot be executed on the selected device.");
                    }
                }
            }
            else
            {
                DisplayMessage.ErrorMessage("The Add-In cannot be executed on the selected device.");
            }
        }

        /// <summary>
        /// Retrieves all relevant fields from the selected Software Unit.
        /// </summary>
        /// <param name="menuSelectionProvider">Software Unit selected when calling the Add-In.</param>
        public void AccessSoftwareUnitFields(MenuSelectionProvider<PlcUnit> menuSelectionProvider)
        {
            // Working with a Software Unit
            IsSoftwareUnit = true;

            // Define the Software Unit selected from the project tree
            _selectedSoftwareUnit = menuSelectionProvider.GetSelection<PlcUnit>().FirstOrDefault();

            // Define the device and software container where the Software Unit is located
            try
            {
                // Case for TIA V20
                // Navigate up the hierarchy to get the DeviceItem from the SoftwareUnit
                _selectedDevice = GetSelectedSoftwareUnit().Parent.Parent as DeviceItem;

                // Retrieve the SoftwareContainer from the selected device
                _softwareContainer =
                ((IEngineeringServiceProvider)GetSelectedDevice()).GetService<SoftwareContainer>();
            }
            catch
            {
                // Case for TIA V19
                // Navigate up the hierarchy to get the SoftwareContainer from the SoftwareUnit
                _softwareContainer = GetSelectedSoftwareUnit().Parent.Parent.Parent.Parent as SoftwareContainer;

                // Retrieve the DeviceItem from the SoftwareContainer
                _selectedDevice = ((SoftwareContainer)_softwareContainer).Parent as DeviceItem;
            }

            if (_softwareContainer != null)
            {
                _software = ((SoftwareContainer)_softwareContainer).Software as PlcSoftware;
                _provider = GetSoftware().GetService<OpcUaProvider>();

                if (_provider != null)
                {
                    _commGroup = GetProvider().CommunicationGroup;
                    _serverInterfaceGroup = GetCommGroup().ServerInterfaceGroup;
                    _serverInterfaces = GetServerInterfaceGroup().ServerInterfaces;       // Note: Server Interfaces are blue
                    _simaticInterfaces = GetServerInterfaceGroup().SimaticInterfaces;     // Note: Simatic Interfaces are purple
                    if (_serverInterfaces != null)
                    {
                        // Discover all available Tag Tables, Data Types and Blocks in the Software Container
                        _tagTableGroup = GetSelectedSoftwareUnit().TagTableGroup;
                        _blockGroup = GetSelectedSoftwareUnit().BlockGroup;
                        _softwareUnitTypeGroup = GetSelectedSoftwareUnit().TypeGroup;

                        // System blocks: Program Resources and S7 Safety in the Software Container
                        _systemBlockGroupComposition = GetSelectedSoftwareUnit().BlockGroup.SystemBlockGroups;

                        // Discover all Data Types in the device
                        _typeGroup = GetSoftware().TypeGroup;
                    }
                    else
                    {
                        DisplayMessage.ErrorMessage("The Add-In cannot be executed on the selected device.");
                    }
                }
                else
                {
                    try
                    {
                        DisplayCustomErrorMessage();
                    }
                    catch
                    {
                        DisplayMessage.ErrorMessage("The Add-In cannot be executed on the selected device.");
                    }
                }
            }
            else
            {
                DisplayMessage.ErrorMessage("The Add-In cannot be executed on the selected device.");
            }
        }

        /// <summary>
        /// Displays a custom error message with a recommended action for each specific HW.
        /// </summary>
        private void DisplayCustomErrorMessage()
        {
            // Get the CPU's type name, article/order number and firmware version of the device
            string typeName = GetSoftwareContainer().GetAttribute("TypeName").ToString();
            string orderNumber = GetSoftwareContainer().GetAttribute("OrderNumber").ToString();
            string firmwareVersion = GetSoftwareContainer().GetAttribute("FirmwareVersion").ToString();

            string errorMessage = $@"The CPU ""{orderNumber}"" with FW {firmwareVersion} does not support OPC UA communication.";

            // Identify the CPU family and display the required firmware update
            if (typeName.Contains("CPU 12"))
            {
                DisplayMessage.ErrorMessage(errorMessage + " Consider updating it to FW V4.4 or higher."
                    + " For S7-1200 G2 CPUs, refer to the available documentation for further details.");
            }
            else if (typeName.Contains("H") || typeName.Contains("R"))
            {
                DisplayMessage.ErrorMessage(errorMessage + " Consider updating it to FW V3.1 or higher");
            }
            else if (typeName.Contains("CPU 15"))
            {
                DisplayMessage.ErrorMessage(errorMessage + " Consider updating it to FW V2.0 or higher");
            }
            else
            {
                DisplayMessage.ErrorMessage("The Add-In cannot be executed on the selected device as it does not support OPC UA communication.");
            }
        }

        /// <summary>
        /// Clears the ambient context to prevent stale engineering object references.
        /// </summary>
        public void Dispose()
        {
            if (_current == this)
            {
                _current = null;
            }
        }
    }
}
