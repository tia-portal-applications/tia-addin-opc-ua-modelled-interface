using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;
using AddInOpcUaInterface.Other;

namespace AddInOpcUaInterface.Phases.Phase4
{
    internal static class BuildDataBlockElements
    {
        private static XNamespace _swNamespace;

        public static List<String> UaObjectTypes = new List<String>();
        public static List<XElement> XElementDataBlocks = new List<XElement>();

        /// <summary>
        /// Builds XElements for the variables of Global and Instance DBs.
        /// </summary>
        /// <param name="attributeList"></param>
        /// <param name="blockName"></param>
        /// <param name="typeOfDB"></param>
        /// <param name="isSafety"></param>
        public static void BuildXElement(XElement attributeList, string blockName, string typeOfDB, bool isSafety)
        {
            // Access the "Sections" XElement within the "Interface" element
            XElement Sections = (XElement)attributeList.Element("Interface").FirstNode;
            // Extract the namespace associated with the "Sections" element
            _swNamespace = Sections.Name.Namespace;
            // Get all "Section" XElements inside <Sections>
            IEnumerable<XElement> sections = Sections.Elements(_swNamespace + "Section");

            foreach (XElement section in sections)
            {
                // "Temp" and "Constant" fields from Instance DBs are never accesible via OPC UA
                if (section.Attribute("Name").Value == "Temp" || section.Attribute("Name").Value == "Constant")
                {
                    continue;
                }

                // Counts the number of variables before adding the Nodes from each section
                int currentNumberOfVariables = BuildDataBlockElements.XElementDataBlocks.Count;

                // Parent node of the variables
                string parent = string.Empty;
                if (typeOfDB == "Instance DB")
                {
                    // For Instance DBs, the parent is a folder with the same name as the section: "Input", "Output", "InOut", "Static"
                    parent = @"O_""" + blockName + $@""".""" + section.Attribute("Name").Value + '"';
                }
                else
                {
                    // For Global DBs, the parent is the UaObject that represents the data block
                    parent = '"' + blockName + '"';
                }
                IEnumerable<XElement> members = section.Elements(_swNamespace + "Member");

                foreach (XElement member in members)
                {
                    string variableName = member.Attribute("Name").Value.Replace("\"", string.Empty);
                    string nodeId;

                    // In Software Units, NodeIds are preceeded by the name of the Software Unit 
                    if (ProjectFields.IsSoftwareUnit) 
                    { 
                        nodeId = '"' + ProjectFields.SelectedSoftwareUnit.Name + "." + blockName + $@""".""" + variableName + '"'; 
                    }
                    else 
                    { 
                        nodeId = '"' + blockName + $@""".""" + variableName + '"'; 
                    }

                    string dataType = member.Attribute("Datatype").Value.Replace("\"", string.Empty);

                    IEnumerable<XElement> attributes = member.Elements(_swNamespace + "AttributeList").Elements(_swNamespace + "BooleanAttribute");

                    // Extract values from attributes, defaulting to false if the attribute doesn't exist
                    bool externalAccessible = true;
                    bool externalWritable = true;
                    if (attributes.Count() != 0) { (externalAccessible, externalWritable) = GetAttributeValue(attributes); }
                    int accessLevel = GetAccessLevel(externalAccessible, externalWritable);
                    // Only for the "Extend Create" use case
                    accessLevel = GetExtendedAccessLevel(accessLevel, typeOfDB, isSafety, nodeId);
                    // If a variable is not accessible with OPC UA, it must not be included in the server interface
                    // Data type Variant is never accesible via OPC UA
                    if (accessLevel == 0 || dataType == "Variant")
                    {
                        continue;   //exits the current iteration of the foreach loop
                    }
                    // Create the data block variable XElement
                    XElement variableElement = new XElement(AddInFields.RootNameSpace + "UAVariable");
                    variableElement.SetAttributeValue("NodeId", $"ns=2;s={nodeId}");
                    variableElement.SetAttributeValue("BrowseName", $"2:{variableName}");
                    variableElement.SetAttributeValue("AccessLevel", accessLevel);
                    variableElement.SetAttributeValue("DataType", dataType);

                    // Handle Arrays
                    bool isArray = dataType.StartsWith("Array");
                    if (isArray)
                    {
                        if (dataType.Contains("[*]"))
                        {
                            LogMessages.PublishLog($@"UNSUPPORTED DATA TYPE: Arrays with variable limits, such as ""{dataType}"" are not supported.");
                            continue;   //exits the current iteration of the foreach loop
                        }
                        (string arrayDimensions, string valueRank) = GetArrayDimensions(dataType);
                        string arrayElementType = GetArrayElementType(dataType);

                        // Add attributes for array handling
                        variableElement.SetAttributeValue("DataType", arrayElementType);
                        variableElement.SetAttributeValue("ArrayDimensions", arrayDimensions);
                        variableElement.SetAttributeValue("ValueRank", valueRank);
                    }

                    // If these three conditions are true, the variable is not ExternalAccessible or ExternalWritable
                    bool isInstanceDB = typeOfDB == "Instance DB";
                    bool isInOutSection = section.Attribute("Name").Value == "InOut";
                    bool isInvalidDataType = !InterfaceTemplate.TagTableDataTypes.Contains(dataType.ToUpper()) || isArray;
                    if (isInstanceDB && isInOutSection && isInvalidDataType)
                    {
                        continue;
                    }
                    
                    // Detect if the data type is known
                    dataType = variableElement.Attribute("DataType").Value.Replace("\"", string.Empty);
                    bool isRemovedDataType = UserSystemDataTypes.RemovedDataTypes.Contains(dataType);
                    bool isUserSystemDataType = UserSystemDataTypes.UserDataTypes.Contains(dataType) || UserSystemDataTypes.SystemDataTypes.Contains(dataType);

                    if (isRemovedDataType)
                    {
                        LogMessages.PublishLog($@"MISSING USER/SYSTEM DATA TYPE: {nodeId} has not been added as the ""{dataType}"" was previously removed from the server's interface.");
                        continue;   //exits the current iteration of the foreach loop
                    }
                    else if (isUserSystemDataType)
                    {
                        variableElement.SetAttributeValue("DataType", $"ns=2;s=DT_{dataType}");
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

                        isUserSystemDataType = UserSystemDataTypes.UserDataTypes.Contains(dataType) || UserSystemDataTypes.SystemDataTypes.Contains(dataType);

                        if (isUserSystemDataType)
                        {
                            variableElement.SetAttributeValue("DataType", $"ns=2;s=DT_{dataType}");
                        }
                        else
                        {
                            // Remove the number inside brackets. Example: STRING [32] or WSTRING[32]
                            dataType = System.Text.RegularExpressions.Regex.Replace(dataType, @"\s*\[\d+\]", string.Empty);
                            if (InterfaceTemplate.TemplateDataTypes.Contains(dataType.ToUpper()) || dataType.StartsWith("Struct"))
                            {
                                variableElement.SetAttributeValue("DataType", dataType.ToUpper());
                            }

                            else
                            {
                                // If the data type is unknown, the variable is trated as a multi-instance object
                                // ATENTION! Users must review the log file to check if these are indied multi-instance objects and not just data types that are not added yet
                                if (!UaObjectTypes.Contains(dataType))
                                {
                                    UaObjectTypes.Add(dataType);

                                    // Code to make the message a bit easier to read 
                                    int numberOfDots = 30 - dataType.Length;
                                    string dots = string.Empty;
                                    if (numberOfDots > 0)
                                    {
                                        dots = new string('.', numberOfDots);
                                    }
                                    LogMessages.PublishLog($@"CHECK: ""{dataType}"" has been treated as a multi-instance object instead of a data type.{dots}If ""{dataType}"" is a data type, add it to the InterfaceTemplate.xml file and rebuild the project.");
                                }

                                XElement uaObjectElement =
                                new XElement(AddInFields.RootNameSpace + "UAObject",
                                    new XAttribute("NodeId", $"ns=2;s={nodeId}"),
                                    new XAttribute("BrowseName", $"2:{variableName}"),
                                    new XElement(AddInFields.RootNameSpace + "DisplayName", variableName),
                                    new XElement(AddInFields.RootNameSpace + "References",
                                        new XElement(AddInFields.RootNameSpace + "Reference", $"ns=2;s={parent}",
                                            new XAttribute("ReferenceType", "HasComponent"),
                                            new XAttribute("IsForward", "false")),
                                        new XElement(AddInFields.RootNameSpace + "Reference", "i=58",
                                            new XAttribute("ReferenceType", "HasTypeDefinition"))
                                    )
                                );

                                XElementDataBlocks.Add(uaObjectElement);
                                continue;
                            }
                        }
                    }

                    // Handle structs
                    dataType = variableElement.Attribute("DataType").Value.Replace("\"", string.Empty);
                    if (dataType.StartsWith("STRUCT"))
                    {
                        string structName = variableName;
                        // Change data type field
                        variableElement.SetAttributeValue("DataType", $"ns=2;s=DT_{blockName + "." + structName}");
                        IEnumerable<XElement> submembers = member.Elements(_swNamespace + "Member");
                        IEnumerable<XElement> subfields = ProcessFields(submembers, blockName + "." + structName);

                        if (subfields != null) 
                        {
                            CreateStructElements(structName, blockName, subfields);
                        }
                        else 
                        {
                            // If the struct has an invalid data type, it is not created
                            LogMessages.PublishLog($@"MISSING DATA TYPE: The struct ""{parent.Replace($@"""", String.Empty) + "." + structName}"" has not been included "
                            + $@"in the server interface as the data type of one or more fields is not contemplated in the Add-In.");

                            continue;
                        }
                    }

                    XElement uaDataTypeElement = variableElement;
                    uaDataTypeElement.Add(
                            new XElement(AddInFields.RootNameSpace + "DisplayName", variableName),
                            new XElement(AddInFields.RootNameSpace + "References",
                                new XElement(AddInFields.RootNameSpace + "Reference", $"ns=2;s={parent}",
                                    new XAttribute("ReferenceType", "HasComponent"),
                                    new XAttribute("IsForward", "false")),
                                new XElement(AddInFields.RootNameSpace + "Reference", "i=63",
                                    new XAttribute("ReferenceType", "HasTypeDefinition"))
                            ),
                            new XElement(AddInFields.RootNameSpace + "Extensions",
                                    new XElement(AddInFields.RootNameSpace + "Extension",
                                        new XElement(AddInFields.RootNameSpaceSi + "VariableMapping", nodeId)
                                    )
                                )
                        );
                    XElementDataBlocks.Add(uaDataTypeElement);
                }

                // Counts the number of variables that have been added in each section
                int newNumberOfVariables = BuildDataBlockElements.XElementDataBlocks.Count;

                // If at least one variable has been added and the DB is of type "Instance DB", add a folder with the name of the section
                if (currentNumberOfVariables != newNumberOfVariables && typeOfDB == "Instance DB")
                {
                    // Add folder for "Input", "Output", "InOut" and "Static" variables
                    XElement uaObjectElement =
                    new XElement(AddInFields.RootNameSpace + "UAObject",
                        new XAttribute("NodeId", $@"ns=2;s=O_""{blockName}"".""{section.Attribute("Name").Value}"""),
                        new XAttribute("BrowseName", $"2:{section.Attribute("Name").Value}"),
                        new XElement(AddInFields.RootNameSpace + "DisplayName", section.Attribute("Name").Value),
                        new XElement(AddInFields.RootNameSpace + "References",
                            new XElement(AddInFields.RootNameSpace + "Reference", $@"ns=2;s=""{blockName}""",
                                new XAttribute("ReferenceType", "HasComponent"),
                                new XAttribute("IsForward", "false")
                            ),
                            new XElement(AddInFields.RootNameSpace + "Reference", "i=61",
                                new XAttribute("ReferenceType", "HasTypeDefinition")
                            )
                        )
                    );
                    BuildDataBlockElements.XElementDataBlocks.Add(uaObjectElement);
                }
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
        /// <param name="typeOfDB"></param>
        /// <param name="isSafety"></param>
        /// <returns></returns>
        private static int GetExtendedAccessLevel(int projectAccessLevel, string typeOfDB, bool isSafety, string variableName)
        {
            int extendedAccessLevel = 0;

            if (typeOfDB == "Global DB")
            {
                if (isSafety)
                {
                    extendedAccessLevel = AddInFields.SafetyGlobalDBsAccessLevel;
                }
                else
                {
                    extendedAccessLevel = AddInFields.GlobalDBsAccessLevel;
                }
            }
            else
            {
                if (isSafety)
                {
                    extendedAccessLevel = AddInFields.SafetyInstanceDBsAccessLevel;
                }
                else
                {
                    extendedAccessLevel = AddInFields.InstanceDBsAccessLevel;
                }
            }

            //Define the Access Level of the tag
            //"Not Accessible"/"Read only"/"Write only"/"Read Write"/"Project's access levels"

            if (projectAccessLevel == extendedAccessLevel || extendedAccessLevel == 4) { return projectAccessLevel; }
            else
            {
                if (extendedAccessLevel == 0 || projectAccessLevel == 0)
                {
                    return 0;
                }
                else if (projectAccessLevel == 3 && extendedAccessLevel == 1)
                {
                    // If the project allow Read/Write access, it is valid to set up a Read-only access level
                    return 1;
                }
                else if (projectAccessLevel == 3 && extendedAccessLevel == 2)
                {
                    // If the project allow Read/Write access, it is valid to set up a Write-only access level
                   return 2;
                }
                else
                {
                    // Project access level and extended access level are not compatible
                    // Example: Project access level = Read only; Extended access level = Read/Write

                    // Convert the project and extended access levels (int) to string format
                    if (AddInFields.AccessLevelDictionary.TryGetValue(projectAccessLevel, out string projectAccessLevelStr))
                    {
                        if (AddInFields.AccessLevelDictionary.TryGetValue(extendedAccessLevel, out string extendedAccessLevelStr))
                        {
                            LogMessages.PublishLog($@"ACCESS LEVEL MISMATCH: The variable {variableName} has not been added to the "
                                         + $@"server interface as its access level in the project ({projectAccessLevelStr}) is not "
                                         + $@"compatible with the selected access level ({extendedAccessLevelStr}).");
                        }
                    }
                    return 0;
                }
            }
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
        private static string GetArrayElementType(string arrayType)
        {
            int pFrom = arrayType.LastIndexOf($@"] of ") + $@"] of ".Length;
            int pTo = arrayType.Length;

            return arrayType.Substring(pFrom, pTo - pFrom).Replace($@"""", string.Empty);
        }

        /// <summary>
        /// Returns the "ExternalAccessible" and "ExternalWritable" attributes to determine the Access Level of the variable.
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        private static (bool, bool) GetAttributeValue(IEnumerable<XElement> attributes)
        {
            bool externalAccessible = true;
            bool externalWritable = true;
            foreach (XElement attribute in attributes)
            {
                if (attribute.Attribute("Name").Value == "ExternalAccessible")
                {
                    externalAccessible = false;
                }
                else if (attribute.Attribute("Name").Value == "ExternalWritable")
                {
                    externalWritable = false;
                }
            }
            return (externalAccessible, externalWritable);
        }

        /// <summary>
        /// Creates the UADataType and UAObject for a struct element.
        /// </summary>
        /// <param name="structName"></param>
        /// <param name="parent"></param>
        /// <param name="fields"></param>
        private static void CreateStructElements(string structName, string parent, IEnumerable<XElement> fields)
        {
            // Logic to create UADataType and UAObject for the struct
            XElement uaDataTypeElement =
                new XElement(AddInFields.RootNameSpace + "UADataType",
                    new XAttribute("NodeId", $"ns=2;s=DT_{parent + "." + structName}"),
                    new XAttribute("BrowseName", $"2:{parent + "." + structName}"),
                    new XElement(AddInFields.RootNameSpace + "DisplayName", parent + "." + structName),
                    new XElement(AddInFields.RootNameSpace + "Description", $"This UDT ({parent + "." + structName}) has no description"),
                    new XElement(AddInFields.RootNameSpace + "References",
                        new XElement(AddInFields.RootNameSpace + "Reference", $"ns=1;i=3400",
                            new XAttribute("ReferenceType", "HasSubtype"),
                            new XAttribute("IsForward", "false")),
                        new XElement(AddInFields.RootNameSpace + "Reference", $"ns=2;s=B_{parent + "." + structName}",
                            new XAttribute("ReferenceType", "HasEncoding"))
                    ),
                    new XElement(AddInFields.RootNameSpace + "Definition",
                        new XAttribute("Name", parent + "." + structName),
                        fields
                    )
                );

            XElement uaObjectElement =
                new XElement(AddInFields.RootNameSpace + "UAObject",
                    new XAttribute("NodeId", $"ns=2;s=B_{parent + "." + structName}"),
                    new XAttribute("BrowseName", "Default Binary"),
                    new XElement(AddInFields.RootNameSpace + "DisplayName", "Default Binary"),
                    new XElement(AddInFields.RootNameSpace + "References",
                        new XElement(AddInFields.RootNameSpace + "Reference", "i=76",
                            new XAttribute("ReferenceType", "HasTypeDefinition"))
                    )
                );

            XElementDataBlocks.Add(uaDataTypeElement);
            XElementDataBlocks.Add(uaObjectElement);
        }

        /// <summary>
        /// Processes the fields inside the "Member" XElement to adapt them to the required format.
        /// These fields can belong to a data type or to a struct defined within the data type itself.
        /// </summary>
        /// <param name="members"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static IEnumerable<XElement> ProcessFields(IEnumerable<XElement> members, string parent)
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
                // Handle Arrays
                string dataType = field.Attribute("DataType").Value.Replace("\"", string.Empty);
                if (dataType.StartsWith("Array"))
                {
                    if (dataType.Contains("[*]"))
                        {
                        LogMessages.PublishLog($@"UNSUPPORTED DATA TYPE: Arrays with variable limits, such as ""{dataType}"" are not supported.");
                        continue;   //exits the current iteration of the foreach loop
                    }
                    (string arrayDimensions, string valueRank) = GetArrayDimensions(dataType);
                    string arrayElementType = GetArrayElementType(dataType);

                    // Add attributes for array handling
                    field.SetAttributeValue("DataType", arrayElementType);
                    field.SetAttributeValue("ArrayDimensions", arrayDimensions);
                    field.SetAttributeValue("ValueRank", valueRank);
                }

                // Differentiate built-in data types from user-defined data types
                dataType = field.Attribute("DataType").Value.Replace("\"", string.Empty);
                bool isUserSystemDataTypes = UserSystemDataTypes.UserDataTypes.Contains(dataType) || UserSystemDataTypes.SystemDataTypes.Contains(dataType);
                if (isUserSystemDataTypes)
                {
                    field.Attribute("DataType").Value = $"ns=2;s=DT_{dataType}";
                }
                else
                {
                    // Example: STRING [32] or WSTRING[32]
                    // Remove the number inside brackets
                    dataType = System.Text.RegularExpressions.Regex.Replace(dataType, @"\[\d+\]", string.Empty);
                    if (InterfaceTemplate.TemplateDataTypes.Contains(dataType.ToUpper()) || dataType.StartsWith("Struct"))
                    {
                        field.Attribute("DataType").Value = dataType.ToUpper();
                    }
                    else
                    {
                        return null;
                    }
                }

                // Handle structs
                dataType = field.Attribute("DataType").Value.Replace("\"", string.Empty);
                if (dataType.StartsWith("STRUCT"))
                {
                    string structName = field.Attribute("Name").Value.Replace("\"", string.Empty);

                    // Change data type field
                    field.SetAttributeValue("DataType", $"ns=2;s=DT_{parent + "." + structName}");
                    // Create new XElements
                    IEnumerable<XElement> submembers = field.Elements(_swNamespace + "Member");
                    IEnumerable<XElement> subfields = ProcessFields(submembers, parent + "." + structName);
                    if (subfields != null)
                    {
                        CreateStructElements(structName, parent, subfields);
                    }
                    else
                    {
                        // If the struct has an invalid data type, it is not created
                        LogMessages.PublishLog($@"MISSING DATA TYPE: The struct ""{parent.Replace($@"""", String.Empty) + "." + structName}"" has not been included "
                        + $@"in the server interface as the data type of one or more fields is not contemplated in the Add-In.");

                        return null;
                    }
                }

                field.RemoveNodes();
            }
            return fields;
        }
    }
}
