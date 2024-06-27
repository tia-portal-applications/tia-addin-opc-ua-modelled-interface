using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Siemens.Engineering;

namespace AddInOpcUaInterface.Other
{
    public static class AddInFields
    {
        // Interface name and URI
        public static string InterfaceName;
        public static string InterfaceURI;
        public static string FilePath;

        // XDocument of the server interface
        public static XDocument OpcUaInterface;             // XML document to build the server interface
        public static XNamespace RootNameSpace;             // Namespace of the OPC Foundation 
        public static XNamespace RootNameSpaceSi;           // Namespace used to map nodes with project variables

        // Variables for the Access Level filter:
        // Possible values: "Not Accessible" "Read only" "Write only" "Read Write" "Project's access levels"
        public static Dictionary<int, string> AccessLevelDictionary = new Dictionary<int, string>
        {
            { 0, "Not Accessible" },
            { 1, "Read only" },
            { 2, "Write only" },
            { 3, "Read Write" },
            { 4, "Project's access levels" }
        };

        // Default access level values for the "Create" option
        public static int InputsAccessLevel                 = 4;    // Access level for input variables
        public static int MemoryAccessLevel                 = 4;    // Access level for memory variables
        public static int OutputsAccessLevel                = 4;    // Access level for output variables
        public static int CountersAccessLevel               = 4;    // Access level for counters
        public static int TimersAccessLevel                 = 4;    // Access level for timers
        public static int GlobalDBsAccessLevel              = 4;    // Access level for Global DB variables
        public static int InstanceDBsAccessLevel            = 4;    // Access level for Instance DB variables
        public static int SafetyGlobalDBsAccessLevel        = 1;    // Access level for Safety Global DB variables
        public static int SafetyInstanceDBsAccessLevel      = 1;    // Access level for Safety Instance DB variables

        // Other settings for "Extend Create"
        public static string NodeIdentifier                 = "String";     // Use "string"/"numeric" node identifiers
        public static bool OptimizedData                    = false;        // Use "Not optimized"/"optimized" server interface
        public static bool KeepEmptyDBs                     = false;        // Remove empty Data Blocks
        public static bool KeepFolderStructure              = false;        // Keep the folder structure present in the project

        // Number of nodes added to the server interface
        public static int NumberDefaultNodes;               // Number of nodes imported from the InterfaceTemplate.xml
        public static int NumberUserSystemDataTypes;        // Number of User and System Data Types defined in the project
        public static int NumberTags;                       // Number of tags defined on all Tag Tables
        public static int NumberGlobalDBs;                  // Number of nodes (folders, data blocks, variables) associated with Global DBs
        public static int NumberInstanceDBs;                // Number of nodes (folders, data blocks, variables) associated with Instance DBs
    }
}
