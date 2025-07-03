using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siemens.Engineering.AddIn.Menu;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.OpcUa;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW.Types;
using Siemens.Engineering.SW;
using Siemens.Engineering;
using AddInOpcUaInterface.Other;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Siemens.Engineering.SW.Units;

namespace AddInOpcUaInterface.Phases
{
    internal static class ProjectFields
    {
        public static DeviceItem SelectedDevice;
        
        public static SoftwareContainer SoftwareContainer;

        public static PlcSoftware Software;
        public static OpcUaProvider Provider;

        public static OpcUaCommunicationGroup CommGroup;
        public static ServerInterfaceGroup ServerInterfaceGroup;
        public static ServerInterfaceComposition ServerInterfaces;
        public static SimaticInterfaceComposition SimaticInterfaces;
        public static PlcSystemBlockGroupComposition SystemBlockGroupComposition;

        public static PlcTagTableGroup TagTableGroup;
        public static PlcBlockGroup BlockGroup;       
        public static PlcTypeSystemGroup TypeGroup;

        // Sowtware Units
        public static bool IsSoftwareUnit;
        public static PlcUnit SelectedSoftwareUnit;
        public static PlcTypeSystemGroup SoftwareUnitTypeGroup;
        public static string SoftwareUnitNamespace;
        
        /// <summary>
        /// Retrieves all relevant fields from the TIA Portal project.
        /// </summary>
        /// <param name="menuSelectionProvider">Device (PLC) selected when calling the Add-In.</param>
        public static void AccessTIAFields(MenuSelectionProvider<DeviceItem> menuSelectionProvider)
        {
            // Define the device selected from the project tree
            SelectedDevice = menuSelectionProvider.GetSelection<DeviceItem>().FirstOrDefault();

            // Creates the Software Container and PlcSoftware
            SoftwareContainer =
            ((IEngineeringServiceProvider)SelectedDevice).GetService<SoftwareContainer>();
            if (SoftwareContainer != null)
            {
                Software = SoftwareContainer.Software as PlcSoftware;
                Provider = Software.GetService<OpcUaProvider>();

                if (Provider != null)
                {
                    CommGroup = Provider.CommunicationGroup;
                    ServerInterfaceGroup = CommGroup.ServerInterfaceGroup;
                    ServerInterfaces = ServerInterfaceGroup.ServerInterfaces;       // Note: Server Interfaces are blue
                    SimaticInterfaces = ServerInterfaceGroup.SimaticInterfaces;     // Note: Simatic Interfaces are purple
                    if (ServerInterfaces != null)
                    {
                        // Discover all available Tag Tables, Data Types and Blocks
                        TagTableGroup = Software.TagTableGroup;
                        BlockGroup = Software.BlockGroup;
                        TypeGroup = Software.TypeGroup;

                        // System blocks: Program Resources and S7 Safety
                        SystemBlockGroupComposition = Software.BlockGroup.SystemBlockGroups;
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
        public static void AccessSoftwareUnitFields(MenuSelectionProvider<PlcUnit> menuSelectionProvider)
        {
            // Working with a Software Unit
            IsSoftwareUnit = true;

            // Define the Software Unit selected from the project tree
            SelectedSoftwareUnit = menuSelectionProvider.GetSelection<PlcUnit>().FirstOrDefault();

            // Define the device and software container where the Software Unit is located
            try
            {
                // Case for TIA V20
                // Navigate up the hierarchy to get the DeviceItem from the SoftwareUnit
                SelectedDevice = SelectedSoftwareUnit.Parent.Parent as DeviceItem;
                
                // Retrieve the SoftwareContainer from the selected device
                SoftwareContainer =
                ((IEngineeringServiceProvider)SelectedDevice).GetService<SoftwareContainer>();
            }
            catch
            {
                // Case for TIA V19
                // Navigate up the hierarchy to get the SoftwareContainer from the SoftwareUnit
                SoftwareContainer = SelectedSoftwareUnit.Parent.Parent.Parent.Parent as SoftwareContainer;
                
                // Retrieve the DeviceItem from the SoftwareContainer
                SelectedDevice = SoftwareContainer.Parent as DeviceItem;
            }
            
            if (SoftwareContainer != null)
            {
                Software = SoftwareContainer.Software as PlcSoftware;
                Provider = Software.GetService<OpcUaProvider>();

                if (Provider != null)
                {
                    CommGroup = Provider.CommunicationGroup;
                    ServerInterfaceGroup = CommGroup.ServerInterfaceGroup;
                    ServerInterfaces = ServerInterfaceGroup.ServerInterfaces;       // Note: Server Interfaces are blue
                    SimaticInterfaces = ServerInterfaceGroup.SimaticInterfaces;     // Note: Simatic Interfaces are purple
                    if (ServerInterfaces != null)
                    {
                        // Discover all available Tag Tables, Data Types and Blocks in the Software Container
                        TagTableGroup = SelectedSoftwareUnit.TagTableGroup;
                        BlockGroup = SelectedSoftwareUnit.BlockGroup;
                        SoftwareUnitTypeGroup = SelectedSoftwareUnit.TypeGroup;

                        // System blocks: Program Resources and S7 Safety in the Software Container
                        SystemBlockGroupComposition = SelectedSoftwareUnit.BlockGroup.SystemBlockGroups;

                        // Discover all Data Types in the device
                        TypeGroup = Software.TypeGroup;                                             
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
        public static void DisplayCustomErrorMessage()
        {
            // Get the CPU's type name, article/order number and firmaware version of the device
            string typeName         = SoftwareContainer.GetAttribute("TypeName").ToString();
            string orderNumber      = SoftwareContainer.GetAttribute("OrderNumber").ToString();
            string firmwareVersion  = SoftwareContainer.GetAttribute("FirmwareVersion").ToString();

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
    }
}
