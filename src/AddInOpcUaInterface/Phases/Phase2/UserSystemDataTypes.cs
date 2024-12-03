using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using AddInOpcUaInterface.Other;
using AddInOpcUaInterface.Phases;
using Siemens.Engineering;
using Siemens.Engineering.SW.Types;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace AddInOpcUaInterface
{
    internal static class UserSystemDataTypes
    {
        private static XNamespace _swNamespace;

        public static List<string> SystemDataTypes  = new List<string>();                   // List containing names of system data types
        public static List<string> UserDataTypes    = new List<string>();                   // List containing names of user data types
        public static List<string> RemovedDataTypes = new List<string>();                   // List containing names of removed data types
        public static List<XElement> XElementUserSystemDataTypes = new List<XElement>();    // List of XElements containing all user and system data types

        /// <summary>
        /// Retrieves the names of both system and user data types.
        /// It also calls the method that creates the system and user data type XElements.
        /// </summary>
        /// <param name="typeGroup"></param>
        public static void GetUserSystemDataTypeElements(PlcTypeSystemGroup typeGroup, bool isSwUnit)
        {
            #region COLLECT SYSTEM AND USER DATA TYPES
                       
            string phase = "Collect names";

            // Iterate through all system data types and store their names in a List<string>
            PlcSystemTypeGroupComposition sysGroupComposition = typeGroup.SystemTypeGroups;

            foreach (PlcSystemTypeGroup sysGroup in sysGroupComposition)
            {
                PlcTypeComposition sysTypes = sysGroup.Types;
                foreach (PlcStruct plcStruct in sysTypes)
                {
                    // Data Types defined in a Software Unit need to be preceeded by the name of the Software Unit
                    if (isSwUnit)
                    {
                        SystemDataTypes.Add(ProjectFields.SelectedSoftwareUnit.Name + "." + plcStruct.Name);
                    }
                    else { SystemDataTypes.Add(plcStruct.Name); }
                }
            }

            // Iterate through all user data types and store their names in a List<string>
            PlcTypeUserGroupComposition Groups = typeGroup.Groups;
            PlcTypeComposition Types = typeGroup.Types;

            foreach (PlcStruct plcStruct in Types)
            {
                // Data Types defined in a Software Unit need to be preceeded by the name of the Software Unit
                if (isSwUnit)
                { 
                    UserDataTypes.Add(ProjectFields.SelectedSoftwareUnit.Name + "." + plcStruct.Name);
                }
                else { UserDataTypes.Add(plcStruct.Name); }
            }
            foreach (PlcTypeUserGroup userGroup in Groups)
            {
                IterateThroughUserGroups(userGroup, phase, isSwUnit);
            }
            #endregion

            #region CREATE NEW XELEMENTS FOR SYSTEM AND USER DATA TYPES

            phase = "Create XElements";
            
            // Iterate through all system data types and stores their names in a List<string>.
            sysGroupComposition = typeGroup.SystemTypeGroups;

            foreach (PlcSystemTypeGroup sysGroup in sysGroupComposition)
            {
                PlcTypeComposition sysTypes = sysGroup.Types;
                foreach (PlcStruct plcStruct in sysTypes)
                {
                    NewXElement(plcStruct, "ns=1;i=3500", isSwUnit);
                }
            }

            // Iterate through all user data types and create a new XElement for each of them
            Groups = typeGroup.Groups;
            Types = typeGroup.Types;

            foreach (PlcStruct plcStruct in Types)
            {
                NewXElement(plcStruct, "ns=1;i=3400", isSwUnit);
            }
            foreach (PlcTypeUserGroup userGroup in Groups)
            {
                IterateThroughUserGroups(userGroup, phase, isSwUnit);
            }
            #endregion
        }

        /// <summary>
        /// Recursively explores all folders (user groups) where data types are defined.
        /// </summary>
        /// <param name="userGroup">The current folder being processed.</param>
        /// <param name="phase">Indicates the current phase, distinguishing between "Collect" and "Create" phases.</param>
        private static void IterateThroughUserGroups(PlcTypeUserGroup userGroup, string phase, bool isSwUnit)
        {
            PlcTypeUserGroupComposition Groups = userGroup.Groups;
            PlcTypeComposition Types = userGroup.Types;

            foreach (PlcStruct plcStruct in Types)
            {
                if (phase == "Collect names")
                {
                    // Data Types defined in a Software Unit need to be preceeded by the name of the Software Unit
                    if (isSwUnit)
                    {
                        UserDataTypes.Add(ProjectFields.SelectedSoftwareUnit.Name + "." + plcStruct.Name);
                    }
                    else { UserDataTypes.Add(plcStruct.Name); }
                }
                else if (phase == "Create XElements")
                {
                    // Create XElements from user/system data types
                    NewXElement(plcStruct, "ns=1;i=3400", isSwUnit);
                }
            }

            foreach (PlcTypeUserGroup subuserGroup in Groups)
            {
                IterateThroughUserGroups(subuserGroup, phase, isSwUnit);
            }
        }

        /// <summary>
        /// Creates XElements for each system and user data type.
        /// To do so, it exports an xml file from the project containing all relevant information about the data type.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="parentNodeId">Parent node for the user/system data types: SimaticStructures (ns=1;s=3400) or SimaticSystemStructures (ns=1;s=3500)</param>
        private static void NewXElement(PlcStruct dataType, string parentNodeId, bool isSwUnit)
        {
            #region EXPORT USER DATA TYPE FROM TIA'S PROJECT AND IMPORT IT AS AN XDOCUMENT

            // Check if the user data type is consistent (the project has been compiled)
            bool isConsistent = (bool)dataType.GetAttribute("IsConsistent");
            if (isConsistent == false)
            {
                DisplayMessage.ErrorMessage("Some elements are not consistent. Please compile the project before running the Add-In.");
            };

            XElement inputElement = new XElement("EmptyElement");

            try
            {
                // Export the user/system data type as a .txt file
                string filePath = Path.ChangeExtension(AddInFields.FilePath, ".txt");
                dataType.Export(new FileInfo(filePath), ExportOptions.None);

                // Import the file as an XDocument
                inputElement = XElement.Load(filePath);
                File.Delete(filePath);
            }
            catch
            {
                DisplayMessage.ErrorMessage("Unexpected error ocurred while exporting a system/user data type.");
            }
            #endregion

            #region  CONVERT THE USER DATA TYPE TO OPC UA FORMAT
            
            // Extract relevant information from the input XElement
            XElement attributeList  = inputElement.Element("SW.Types.PlcStruct").Element("AttributeList");
            string dataTypeName = attributeList.Element("Name")?.Value;

            if(isSwUnit)
            {
                // Data Types defined in a Software Unit need to be preceeded by the name of the Software Unit
                dataTypeName = ProjectFields.SelectedSoftwareUnit.Name + "." + dataTypeName;               
            }

            // Access the "Sections" XElement within the "Interface" element
            XElement Sections = (XElement)attributeList.Element("Interface").FirstNode;
            // Extract the namespace associated with the "Sections" element
            _swNamespace = Sections.Name.Namespace;
            // Get all "Section" XElements inside <Sections>
            XElement section = Sections.Element(_swNamespace + "Section");

            IEnumerable<XElement> members = section.Elements(_swNamespace + "Member");
            IEnumerable<XElement> fields = ProcessFields(members, dataTypeName);
            
            if (fields != null)
            {
                CreateDataTypeElement(dataTypeName, fields, parentNodeId);
            }
            else
            {
                // If the data type is unknown, the variable is not added to the project
                LogMessages.PublishLog("                   " 
                                     + $@"MISSING DATA TYPE: The UDT/SDT ""{dataTypeName}"" has not been included "
                                     + $@"in the server interface.");

                RemovedDataTypes.Add(dataTypeName);
            }
            #endregion
        }

        /// <summary>
        /// Processes the fields inside the "Member" XElement to adapt them to the required format.
        /// These fields can belong to a data type or to a struct defined within the data type itself.
        /// </summary>
        /// <param name="members"></param>
        /// <param name="parentElement">The name of the parent element, which can be a data type or a struct.</param>
        /// <returns></returns>
        private static IEnumerable<XElement> ProcessFields(IEnumerable<XElement> members, string parentElement)
        {
            // Transform XElements to align with the XML file requirements for the server interface
            IEnumerable<XElement> fields = members.Select(field =>
            {
                // Rename XElement from "Member" to "Field"
                field.Name = AddInFields.RootNameSpace + "Field";

                // Rename the attribute "Datatype" to "DataType"
                // Step 1: Create new attribute called "DataType" with the same value as the attribute "Datatype"
                field.Add(new XAttribute("DataType", field.Attribute("Datatype").Value));
                // Step 2: Delete the old attribute "Datatype"
                field.Attribute("Datatype").Remove();

                // Remove the "Version" attribute, which only appears in the export of specific data types
                // As it is not a valid attribute for the server interface, it needs to be removed
                if (field.Attribute("Version") != null)
                {
                    field.Attribute("Version").Remove();
                }
                return field;
            }).ToList();

            foreach (XElement field in fields)
            {
                string dataType = field.Attribute("DataType").Value.Replace(@"""", string.Empty);

                // Handle Arrays
                bool isArray = dataType.StartsWith("Array");
                if (isArray)
                {
                    (string arrayDimensions, string valueRank) = GetArrayDimensions(dataType);
                    string arrayElementType = GetArrayElementType(dataType);

                    // Add attributes for array handling
                    field.SetAttributeValue("DataType", arrayElementType);
                    field.SetAttributeValue("ArrayDimensions", arrayDimensions);
                    field.SetAttributeValue("ValueRank", valueRank);

                    // New data type. Example: Array [0..1] of Bool --> Bool
                    dataType = arrayElementType;
                }

                // Handle user/system data types
                bool isUserSystemDataTypes = UserDataTypes.Contains(dataType) || SystemDataTypes.Contains(dataType);
                if (isUserSystemDataTypes)
                {
                    field.Attribute("DataType").Value = $"ns=2;s=DT_{dataType}";
                }
                else
                {
                    // Handle Software Unit data types that reference a type defined in the device (PLC)
                    if (ProjectFields.IsSoftwareUnit)
                    {
                        if (dataType.StartsWith(ProjectFields.SelectedSoftwareUnit.Name))
                        {
                            dataType = dataType.Substring(ProjectFields.SelectedSoftwareUnit.Name.Length + 1);
                        }
                    }

                    isUserSystemDataTypes = UserDataTypes.Contains(dataType) || SystemDataTypes.Contains(dataType);

                    if (isUserSystemDataTypes)
                    {
                        field.Attribute("DataType").Value = $"ns=2;s=DT_{dataType}";
                    }
                    else
                    {

                        // Remove the number inside brackets if there is any. Example: STRING [32] or WSTRING[32]
                        dataType = Regex.Replace(dataType, @"\s*\[\d+\]", string.Empty);
                        if (InterfaceTemplate.TemplateDataTypes.Contains(dataType.ToUpper()) || dataType.StartsWith("Struct"))
                        {
                            field.SetAttributeValue("DataType", dataType.ToUpper());
                        }
                        else
                        {
                            // If the data type is unknown, the variable is not added to the project
                            LogMessages.PublishLog($@"MISSING DATA TYPE: The field ""{parentElement + "." + field.Attribute("Name").Value}"" has not been included "
                                                 + $@"in the server interface as the data type ""{dataType}"" is not contemplated in the Add-In.");

                            return null;
                        }
                    }
                }

                // Handle structs
                bool isStruct = dataType.StartsWith("Struct");
                if (isStruct)
                {
                    string structName = field.Attribute("Name").Value.Replace(@"""", string.Empty);

                    // Change data type field
                    field.SetAttributeValue("DataType", $"ns=2;s=DT_{parentElement + "." + structName}");

                    // Create new XElements for the stuct's children
                    IEnumerable<XElement> structChilds = field.Elements(_swNamespace + "Member");
                    IEnumerable<XElement> structFields = ProcessFields(structChilds, parentElement + "." + structName);

                    if (structFields != null)
                    {
                        CreateStructElements(structName, parentElement, structFields);
                    }
                    else
                    {
                        // If the struct has an invalid data type, it is not created
                        LogMessages.PublishLog(("                   "
                                            + $@"The struct ""{parentElement.Replace($@"""", String.Empty) + "." + structName}"" "
                                            + $@"has not been included in the server interface."));

                        return null;
                    }
                }

                field.RemoveNodes();
            }
            return fields;
        }

        /// <summary>
        /// Returns the dimensions of an array.
        /// </summary>
        /// <param name="arrayType"></param>
        /// <returns></returns>
        private static (string, string) GetArrayDimensions(string arrayDataType)
        {
            int arrayStartIndex;
            int arrayEndIndex;
            List<int> dimensions = new List<int>();

            // Extract the dimension definitions
            int positionFrom = arrayDataType.IndexOf("[") + 1;
            int positionTo = arrayDataType.IndexOf("]");
            string dimensionsString = arrayDataType.Substring(positionFrom, positionTo - positionFrom);
            string[] dimensionRanges = dimensionsString.Split(',');

            foreach (string range in dimensionRanges)
            {
                string trimmedRange = range.Trim();
                int rangePositionFrom = trimmedRange.IndexOf("..") + 2;
                int rangePositionTo = trimmedRange.IndexOf("..");
                string substringStartIndex = trimmedRange.Substring(0, rangePositionTo);
                string substringEndIndex = trimmedRange.Substring(rangePositionFrom);

                if (int.TryParse(substringStartIndex, out arrayStartIndex)) { }
                else
                {
                    if (substringStartIndex.StartsWith("_."))
                    {
                        substringStartIndex = substringStartIndex.Substring(2);
                    }
                    arrayStartIndex = UserConstants.UserConstantValues[substringStartIndex];
                }

                if (int.TryParse(substringEndIndex, out arrayEndIndex)) { }
                else
                {
                    if (substringEndIndex.StartsWith("_."))
                    {
                        substringEndIndex = substringEndIndex.Substring(2);
                    }
                    arrayEndIndex = UserConstants.UserConstantValues[substringEndIndex];
                }

                // Array dimensions = arrayEndIndex - arrayStartIndex + 1
                int arrayDimension = arrayEndIndex - arrayStartIndex + 1;
                dimensions.Add(arrayDimension);
            }

            return (string.Join(",", dimensions), dimensionRanges.Count().ToString());
        }

        /// <summary>
        /// Extracts the data type of an array.
        /// </summary>
        /// <param name="arrayType"></param>
        /// <returns></returns>
        private static string GetArrayElementType(string arrayDataType)
        {
            int pFrom = arrayDataType.LastIndexOf($@"] of ") + $@"] of ".Length;
            int pTo = arrayDataType.Length;

            return arrayDataType.Substring(pFrom, pTo - pFrom).Replace(@"""", string.Empty);
        }

        /// <summary>
        /// Creates the UADataType and UAObject for the user/system data type.
        /// </summary>
        /// <param name="dataTypeName"></param>
        /// <param name="fields"></param>
        private static void CreateDataTypeElement(string dataTypeName, IEnumerable<XElement> fields, string parentNodeId)
        {
            XElement uaDataTypeElement =
                new XElement(AddInFields.RootNameSpace + "UADataType",
                    new XAttribute("NodeId", $"ns=2;s=DT_{dataTypeName}"),
                    new XAttribute("BrowseName", $"2:{dataTypeName}"),
                    new XElement(AddInFields.RootNameSpace + "DisplayName", dataTypeName),
                    new XElement(AddInFields.RootNameSpace + "Description", "This UDT has no description"),
                    new XElement(AddInFields.RootNameSpace + "References",
                        new XElement(AddInFields.RootNameSpace + "Reference", parentNodeId,
                            new XAttribute("ReferenceType", "HasSubtype"),
                            new XAttribute("IsForward", "false")),
                        new XElement(AddInFields.RootNameSpace + "Reference", $"ns=2;s=B_{dataTypeName}",
                            new XAttribute("ReferenceType", "HasEncoding"))
                    ),
                    new XElement(AddInFields.RootNameSpace + "Definition",
                        new XAttribute("Name", dataTypeName),
                        fields
                    )
            );

            // Create the UAObject XElement
            XElement uaObjectElement =
                new XElement(AddInFields.RootNameSpace + "UAObject",
                    new XAttribute("NodeId", $"ns=2;s=B_{dataTypeName}"),
                    new XAttribute("BrowseName", "Default Binary"),
                    new XElement(AddInFields.RootNameSpace + "DisplayName", "Default Binary"),
                    new XElement(AddInFields.RootNameSpace + "References",
                        new XElement(AddInFields.RootNameSpace + "Reference", "i=76",
                            new XAttribute("ReferenceType", "HasTypeDefinition"))
                    )
            );

            XElementUserSystemDataTypes.Add(uaDataTypeElement);
            XElementUserSystemDataTypes.Add(uaObjectElement);
        }

        /// <summary>
        /// Creates the UADataType and UAObject for a struct element.
        /// </summary>
        /// <param name="structName"></param>
        /// <param name="parentElement"></param>
        /// <param name="structFields"></param>
        private static void CreateStructElements(string structName, string parentElement, IEnumerable<XElement> structFields)
        {
            XElement uaDataTypeElement =
                new XElement(AddInFields.RootNameSpace + "UADataType",
                    new XAttribute("NodeId", $"ns=2;s=DT_{parentElement + "." + structName}"),
                    new XAttribute("BrowseName", $"2:{parentElement + "." + structName}"),
                    new XElement(AddInFields.RootNameSpace + "DisplayName", parentElement + "." + structName),
                    new XElement(AddInFields.RootNameSpace + "Description", $"This UDT ({parentElement + "." + structName}) has no description"),
                    new XElement(AddInFields.RootNameSpace + "References",
                        new XElement(AddInFields.RootNameSpace + "Reference", "ns=1;i=3400",
                            new XAttribute("ReferenceType", "HasSubtype"),
                            new XAttribute("IsForward", "false")),
                        new XElement(AddInFields.RootNameSpace + "Reference", $"ns=2;s=B_{parentElement + "." + structName}",
                            new XAttribute("ReferenceType", "HasEncoding"))
                    ),
                    new XElement(AddInFields.RootNameSpace + "Definition",
                        new XAttribute("Name", parentElement + "." + structName),
                        structFields
                    )
                );

            XElement uaObjectElement =
                new XElement(AddInFields.RootNameSpace + "UAObject",
                    new XAttribute("NodeId", $"ns=2;s=B_{parentElement + "." + structName}"),
                    new XAttribute("BrowseName", "Default Binary"),
                    new XElement(AddInFields.RootNameSpace + "DisplayName", "Default Binary"),
                    new XElement(AddInFields.RootNameSpace + "References",
                        new XElement(AddInFields.RootNameSpace + "Reference", "i=76",
                            new XAttribute("ReferenceType", "HasTypeDefinition"))
                    )
                );

            XElementUserSystemDataTypes.Add(uaDataTypeElement);
            XElementUserSystemDataTypes.Add(uaObjectElement);
        }
    }
}
