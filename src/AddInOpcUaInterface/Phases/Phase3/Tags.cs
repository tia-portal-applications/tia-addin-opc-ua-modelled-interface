using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Siemens.Engineering.CrossReference;
using Siemens.Engineering.SW.Tags;
using AddInOpcUaInterface.Other;
using Siemens.Engineering;

namespace AddInOpcUaInterface.Phases
{
    internal static class Tags
    {
        public static List<XElement> XElementTags = new List<XElement>();

        /// <summary>
        /// Browses all tag tables in the project and creates an XElement for each tag.
        /// </summary>
        /// <param name="tagTableGroup"></param>
        public static void GetTagElements(PlcTagTableGroup tagTableGroup)
        {
            foreach (PlcTagTable table in tagTableGroup.TagTables)
            {
                foreach (PlcTag tag in table.Tags)
                {
                    string nodeId = $@"ns=2;s={tag.Name}";                                      // NodeId
                    string parentNodeId = string.Empty;                                         // Parent NodeId
                    string displayName = tag.Name;                                              // DisplayName
                    string dataType = tag.DataTypeName;                                         // Data Type
                    string logicalAddress = tag.LogicalAddress;                                 // Logical Address (Input, memory or output)
                    bool externalAccessible = tag.ExternalAccessible;                           // Accessible from the OPC UA Server
                    bool externalWritable = tag.ExternalWritable;                               // Writable. An OPC UA client can "overwrite" the value.
                    int accessLevel = GetAccessLevel(externalAccessible, externalWritable);     // Access Level

                    dataType = dataType.Replace($@"""", string.Empty);
                    bool isUserSystemDataType = UserSystemDataTypes.UserDataTypes.Contains(dataType) || UserSystemDataTypes.SystemDataTypes.Contains(dataType);

                    if (isUserSystemDataType)
                    {
                        // It is a User/System Data Type. Therefore, it must include the prefix: "ns=2;s=DT_"
                        dataType = "ns=2;s=DT_" + dataType;
                    }
                    else
                    {
                        // Normal Data Type
                        dataType = tag.DataTypeName.ToUpper();
                    }

                    if (logicalAddress.Contains("%I"))
                    {
                        parentNodeId = "ns=2;s=Inputs";

                        // Only for the "Extend Create" case. An Access Level of 4 implies keeping the project's access level
                        if (AddInFields.InputsAccessLevel != 4)
                        {
                            accessLevel = GetExtendedAccessLevel(accessLevel, AddInFields.InputsAccessLevel, tag.Name);
                        } 
                    }
                    else if (logicalAddress.Contains("%M"))
                    {
                        parentNodeId = "ns=2;s=Memory";

                        // Only for the "Extend Create" case. An Access Level of 4 implies keeping the project's access level
                        if (AddInFields.MemoryAccessLevel != 4)
                        {
                            accessLevel = GetExtendedAccessLevel(accessLevel, AddInFields.MemoryAccessLevel, tag.Name);
                        }
                    }
                    else if (logicalAddress.Contains("%Q"))
                    {
                        parentNodeId = "ns=2;s=Outputs";

                        // Only for the "Extend Create" case. An Access Level of 4 implies keeping the project's access level
                        if (AddInFields.OutputsAccessLevel != 4)
                        {
                            accessLevel = GetExtendedAccessLevel(accessLevel, AddInFields.OutputsAccessLevel, tag.Name);
                        }
                    }
                    else if (logicalAddress.Contains("C"))
                    {
                        parentNodeId = "ns=2;s=Counters";

                        // Only for the "Extend Create" case. An Access Level of 4 implies keeping the project's access level
                        if (AddInFields.CountersAccessLevel != 4)
                        {
                            accessLevel = GetExtendedAccessLevel(accessLevel, AddInFields.CountersAccessLevel, tag.Name);
                        }
                    }
                    else if (logicalAddress.Contains("T"))
                    {
                        parentNodeId = "ns=2;s=Timers";

                        // Only for the "Extend Create" case. An Access Level of 4 implies keeping the project's access level
                        if (AddInFields.TimersAccessLevel != 4)
                        {
                            accessLevel = GetExtendedAccessLevel(accessLevel, AddInFields.TimersAccessLevel, tag.Name);
                        }  
                    }

                    // If a variable is not accessible with OPC UA, it must not be included in the server interface
                    if (accessLevel == 0)
                    {
                        // Exit the current iteration of the loop
                        continue;
                    }

                    // If the name of a Tag contains a '.' it must be mapped to the project's variable between brackets
                    string mapping = displayName;
                    if (mapping.Contains('.'))  { mapping = $@"""{mapping}"""; }

                    XElement uaTagElement =
                    new XElement(AddInFields.RootNameSpace + "UAVariable",
                        new XAttribute("NodeId", $@"ns=2;s=""{displayName}"""),
                        new XAttribute("BrowseName", $"2:{displayName}"),
                        new XAttribute("DataType", dataType),
                        new XAttribute("AccessLevel", accessLevel),
                        new XElement(AddInFields.RootNameSpace + "DisplayName", displayName),
                        new XElement(AddInFields.RootNameSpace + "References",
                            new XElement(AddInFields.RootNameSpace + "Reference", parentNodeId,
                                new XAttribute("ReferenceType", "HasComponent"),
                                new XAttribute("IsForward", "false")),
                            new XElement(AddInFields.RootNameSpace + "Reference", "i=63",
                                new XAttribute("ReferenceType", "HasTypeDefinition"))
                        ),
                        new XElement(AddInFields.RootNameSpace + "Extensions",
                                new XElement(AddInFields.RootNameSpace + "Extension",
                                    new XElement(AddInFields.RootNameSpaceSi + "VariableMapping", mapping)
                                )
                            )
                    );
                    
                    XElementTags.Add(uaTagElement);
                }
            }
            foreach (PlcTagTableGroup tableGroup in tagTableGroup.Groups)
            {
                GetTagElements(tableGroup);
            }
        }

        /// <summary>
        /// Determines the Access Level based on the specified "externalAccessible" and "externalWritable" parameters.
        /// </summary>
        /// <param name="externalAccessible"></param>
        /// <param name="externalWritable"></param>
        /// <returns></returns>
        private static int GetAccessLevel(bool externalAccessible, bool externalWritable)
        {
            int accessLevel = 0;

            if (externalAccessible == true)
            {
                accessLevel += 1;
            }
            if (externalWritable == true)
            {
                accessLevel += 2;
            }
            return accessLevel;
        }

        /// <summary>
        /// Determines the Access Level based on the user's input and the project's Access Level (GetAccessLevel).
        /// </summary>
        /// <param name="projectAccessLevel"></param>
        /// <param name="extendedAccessLevel"></param>
        /// <returns></returns>
        private static int GetExtendedAccessLevel(int projectAccessLevel, int extendedAccessLevel, string tagName)
        {
            //Define the Access Level of the tag
            //"Not Accessible"/"Read only"/"Write only"/"Read Write"/"Project's access levels"
            
            if (projectAccessLevel == extendedAccessLevel) { return projectAccessLevel; }
            else
            {
                int accessLevel = 0;

                if (extendedAccessLevel == 0 || projectAccessLevel == 0)
                {
                    accessLevel = 0;
                }
                else if (projectAccessLevel == 3 && extendedAccessLevel == 1)
                {
                    // If the project allow Read/Write access, it is valid to set up a Read-only access level
                    accessLevel = 1;
                }
                else if (projectAccessLevel == 3 && extendedAccessLevel == 2)
                {
                    // If the project allow Read/Write access, it is valid to set up a Write-only access level
                    accessLevel = 2;
                }
                else
                {
                    // Project access level and extended access level are not compatible
                    // Example: Project access level = Read only; Extended access level = Read/Write
                    accessLevel = 0;
                    // Convert the project's access level to string
                    if (AddInFields.AccessLevelDictionary.TryGetValue(projectAccessLevel, out string projectAccessLevelStr))
                    {
                        if (AddInFields.AccessLevelDictionary.TryGetValue(extendedAccessLevel, out string extendedAccessLevelStr))
                        {
                            LogMessages.PublishLog($@"ACCESS LEVEL MISMATCH: The tag ""{tagName}"" has not been added to the "
                                         + $@"server interface as its access level in the project ({projectAccessLevelStr}) is "
                                         + $@"not compatible with the selected access level ({extendedAccessLevelStr}).");
                        }    
                    }
                }
                return accessLevel;
            }
        }
    }
}
