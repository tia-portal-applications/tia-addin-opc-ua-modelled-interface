using AddInOpcUaInterface.Other;
using AddInOpcUaInterface.Phases.Phase4;
using Siemens.Engineering;
using Siemens.Engineering.Multiuser;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Blocks.Interface;
using Siemens.Engineering.SW.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace AddInOpcUaInterface.Phases.Phase4
{
    public static class DataBlocksInstance
    {
        private static int _numberOfProcessedDBs = 0;

        /// <summary>
        /// Resets the count of processed datablock elements to zero.
        /// </summary>
        public static void ResetDatablockElements()
        {
            _numberOfProcessedDBs = 0;
        }

        /// <summary>
        /// Browses through all the project's data blocks.
        /// </summary>
        /// <param name="blockGroup"></param>
        public static void GetDatablockElements(PlcBlockGroup blockGroup)
        {
            string parentFolder = "DataBlocksInstance";
            foreach (PlcBlock block in blockGroup.Blocks)
            {
                //Only process Datablocks
                if (block.ProgrammingLanguage == ProgrammingLanguage.DB && AddInExecutionContext.Current.InstanceDBsAccessLevel != 0)
                {
                    NewDataBlockElement(block, parentFolder, false);
                }
                //Only process failsafe Datablocks
                if (block.ProgrammingLanguage == ProgrammingLanguage.F_DB && AddInExecutionContext.Current.SafetyInstanceDBsAccessLevel != 0)
                {
                    NewDataBlockElement(block, parentFolder, true);
                }
            }
            foreach (PlcBlockUserGroup userGroup in blockGroup.Groups)
            {
                IterateThroughBlockGroups(userGroup, parentFolder);
            }
        }

        /// <summary>
        /// Recursive method to access those data blocks located inside block groups (folders).
        /// </summary>
        /// <param name="blockGroup"></param>
        /// <param name="parentFolder"></param>
        public static void IterateThroughBlockGroups(PlcBlockGroup blockGroup, string parentFolder)
        {
            if (AddInExecutionContext.Current.KeepFolderStructure)
            {
                // Replicate the folder structure of the project
                string folderName = blockGroup.Name;
                string fullPathName = parentFolder + '.' + folderName;
                XElement uaObjectElement =
                    new XElement(AddInExecutionContext.Current.RootNameSpace + "UAObject",
                        new XAttribute("NodeId", $"ns=2;s={fullPathName}"),
                        new XAttribute("BrowseName", $"2:{folderName}"),
                        new XElement(AddInExecutionContext.Current.RootNameSpace + "DisplayName", folderName),
                        new XElement(AddInExecutionContext.Current.RootNameSpace + "References",
                            new XElement(AddInExecutionContext.Current.RootNameSpace + "Reference", $"ns=2;s={parentFolder}",
                                new XAttribute("ReferenceType", "HasComponent"),
                                new XAttribute("IsForward", "false")
                            ),
                            new XElement(AddInExecutionContext.Current.RootNameSpace + "Reference", "i=61",
                                new XAttribute("ReferenceType", "HasTypeDefinition")
                            )
                        )
                    );
                BuildDataBlockElements.XElementDataBlocks.Add(uaObjectElement);
                parentFolder = fullPathName;
            }

            foreach (PlcBlock block in blockGroup.Blocks)
            {
                //Only process Datablocks
                if (block.ProgrammingLanguage == ProgrammingLanguage.DB && AddInExecutionContext.Current.InstanceDBsAccessLevel != 0)
                {
                    NewDataBlockElement(block, parentFolder, false);
                }
                //Only process failsafe Datablocks
                if (block.ProgrammingLanguage == ProgrammingLanguage.F_DB && AddInExecutionContext.Current.SafetyInstanceDBsAccessLevel != 0)
                {
                    NewDataBlockElement(block, parentFolder, true);
                }
            }
            foreach (PlcBlockUserGroup userGroup in blockGroup.Groups)
            {
                IterateThroughBlockGroups(userGroup, parentFolder);
            }
        }

        /// <summary>
        /// Creates an UAObject to represent the Instance DB in the server interface.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="parentFolder"></param>
        /// <param name="isSafety"></param>
        private static void NewDataBlockElement(PlcBlock block, string parentFolder, bool isSafety)
        {
            // Update display message every 2 datablocks
            if (_numberOfProcessedDBs % 2 == 0)
            {
                DisplayMessage.GetExclusiveAccess().Text = $@"Adding ""Instance"" DBs to the server interface... Count: {_numberOfProcessedDBs}";
            }

            #region EXPORT DATA BLOCK FROM TIA'S PROJECT

            // Check if the data block is consistent (the project has been compiled)
            bool isConsistent = (bool)block.GetAttribute("IsConsistent");
            if (isConsistent == false)
            {
                DisplayMessage.ErrorMessage("Some elements are not consistent. Please compile the project before running the Add-In.");
            }
            ;

            // Export the data block as a .txt file
            string filePath = Path.ChangeExtension(AddInExecutionContext.Current.FilePath, ".txt");
            DataBlock db = (DataBlock)block;

            try
            {
                db.Export(new FileInfo(filePath), ExportOptions.None);
            }
            catch
            {
                DisplayMessage.ErrorMessage($@"Unexpected error ocurred while exporting the .txt file of the {block.Name} Instance DB.");
            }
            #endregion

            #region IMPORT DATA BLOCK AS AN XDOCUMENT VARIABLE

            XElement inputElement = new XElement("Empty");
            try
            {
                inputElement = XElement.Load(filePath);
                File.Delete(filePath);
            }
            catch
            {
                DisplayMessage.ErrorMessage($@"Unexpected error ocurred while importing the .txt file of the {block.Name} Instance DB.");
            }
            #endregion

            #region CHECK IF THE DATA BLOCK IS A GLOBAL OR AN INSTANCE DB

            // Extract relevant information from the input XElement
            XElement attributeList = new XElement("Template");
            string dbAccessibleFromOPCUA = "false";
            string typeOfDB = string.Empty;
            //Check if it is an instance DB
            if (inputElement.Element("SW.Blocks.InstanceDB") != null)
            {
                attributeList = inputElement.Element("SW.Blocks.InstanceDB").Element("AttributeList");
                typeOfDB = "Instance DB";

                // Check if the Instance DB is accessible via OPC UA
                if (attributeList.Element("DBAccessibleFromOPCUA") != null)
                {
                    // If the XElement DBAccessibleFromOPCUA exists, it means that the DB is not accesible via OPC UA
                    dbAccessibleFromOPCUA = attributeList.Element("DBAccessibleFromOPCUA").Value;
                    return;
                }
                else
                {
                    // If the XElement DBAccessibleFromOPCUA does not exists, it means that the DB is accessible via OPC UA
                    dbAccessibleFromOPCUA = "true";
                }
                _numberOfProcessedDBs += 1;
            }
            else { return; }
            #endregion

            #region CHECK THE NAMESPACE OF THE DATA BLOCK (ONLY FOR SW UNITS)

            // In Software Units, datablocks can be assigned to a specific namespace
            if (AddInExecutionContext.Current.IsSoftwareUnit)
            {
                AddInExecutionContext.Current.SoftwareUnitNamespace = attributeList.Element("Namespace").Value;
            }
            #endregion

            #region CREATE THE DATA BLOCK INSTANCE OBJECT FOR THE SERVER INTERFACE

            string blockName = block.Name;
            string nodeId = '"' + blockName + '"';
            // Creating the UAObject element
            XElement uaObjectElement =
                new XElement(AddInExecutionContext.Current.RootNameSpace + "UAObject",
                    new XAttribute("NodeId", $"ns=2;s={nodeId}"),
                    new XAttribute("BrowseName", $"2:{blockName}"),
                    new XElement(AddInExecutionContext.Current.RootNameSpace + "DisplayName", blockName),
                    new XElement(AddInExecutionContext.Current.RootNameSpace + "References",
                        new XElement(AddInExecutionContext.Current.RootNameSpace + "Reference", $"ns=2;s={parentFolder}",
                            new XAttribute("ReferenceType", "HasComponent"),
                            new XAttribute("IsForward", "false")
                        ),
                        new XElement(AddInExecutionContext.Current.RootNameSpace + "Reference", "i=58",
                            new XAttribute("ReferenceType", "HasTypeDefinition")
                        )
                    )
                );
            BuildDataBlockElements.XElementDataBlocks.Add(uaObjectElement);

            BuildMethod.CheckMethod(nodeId, attributeList);


            /*

                        //Check if the instance DB belongs to a server method block

                        bool hasInputArgs = false;
                        bool hasOutputArgs = false;
                        bool isMethod = false;
                        //XElement member;

                        if (typeOfDB == "Instance DB")
                        {
                            XNamespace ns = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
                            XElement sectionStatic = attributeList.Element("Interface").Element(ns + "Sections").Elements(ns + "Section").FirstOrDefault(s => (string)s.Attribute("Name") == "Static");

                            if (sectionStatic.Elements(ns + "Member").FirstOrDefault(m => (string)m.Attribute("Name") == "OPC_UA_ServerMethodPre_Instance") != null)
                            {

                                isMethod = true;

                                if (sectionStatic.Elements(ns + "Member").FirstOrDefault(m => (string)m.Attribute("Name") == "UAMethod_InParameters") != null)
                                {

                                    hasInputArgs = true;
                                }

                                if (sectionStatic.Elements(ns + "Member").FirstOrDefault(m => (string)m.Attribute("Name") == "UAMethod_OutParameters") != null)
                                {
                                    hasOutputArgs = true;

                                }

                            }

                        }

                        if (isMethod)
                        {

                            // Build the UAMethod depending on whether InputArguments and/or OutputArguments exist

                            var methodReferences = new List<XElement>
                            {
                                // Mandatory modelling rule reference
                                new XElement(AddInExecutionContext.Current.RootNameSpace + "Reference",
                                    new XAttribute("ReferenceType", "HasModellingRule"),
                                    "i=78"),

                                // Reference back to the parent DB object
                                new XElement(AddInExecutionContext.Current.RootNameSpace + "Reference",
                                    new XAttribute("ReferenceType", "HasComponent"),
                                    new XAttribute("IsForward", "false"),
                                    $"ns=2;s={nodeId}")
                            };

                            // Add HasProperty reference for InputArguments if the struct exists
                            if (hasInputArgs)
                                methodReferences.Add(
                                    new XElement(AddInExecutionContext.Current.RootNameSpace + "Reference",
                                        new XAttribute("ReferenceType", "HasProperty"),
                                        $"ns=2;s={nodeId}.Method.InputArguments"));

                            // Add HasProperty reference for OutputArguments if the struct exists
                            if (hasOutputArgs)
                                methodReferences.Add(
                                    new XElement(AddInExecutionContext.Current.RootNameSpace + "Reference",
                                        new XAttribute("ReferenceType", "HasProperty"),
                                        $"ns=2;s={nodeId}.Method.OutputArguments"));


                            // Create the UAMethod node

                            XElement uaMethodElement =
                                new XElement(AddInExecutionContext.Current.RootNameSpace + "UAMethod",
                                    new XAttribute("NodeId", $"ns=2;s={nodeId}.Method"),
                                    new XAttribute("BrowseName", "2:Method"),
                                    new XAttribute("ParentNodeId", $"ns=2;s={nodeId}"),
                                    new XAttribute("MethodDeclarationId", $"ns=2;s={nodeId}"),
                                    new XElement(AddInExecutionContext.Current.RootNameSpace + "DisplayName",
                                        "Method"),
                                    new XElement(AddInExecutionContext.Current.RootNameSpace + "References",
                                        methodReferences),
                                    new XElement(AddInExecutionContext.Current.RootNameSpace + "Extensions",
                                        new XElement(AddInExecutionContext.Current.RootNameSpace + "Extension",
                                            new XElement(Ctx.RootNameSpaceSi + "MethodMapping",
                                                $"{nodeId}.Method")))
                                );

                            // Add the UAMethod node to the output list
                            BuildDataBlockElements.XElementDataBlocks.Add(uaMethodElement);

                            // Create InputArguments UAVariable if UAMethod_InParameters exists

                            if (hasInputArgs)
                            {
                                XElement inputArguments = BuildMethodArguments(
                                    nodeId, "InputArguments", attributeList, "UAMethod_InParameters");
                                BuildDataBlockElements.XElementDataBlocks.Add(inputArguments);
                            }

                            // Create OutputArguments UAVariable if UAMethod_OutParameters exists

                            if (hasOutputArgs)
                            {
                                XElement outputArguments = BuildMethodArguments(
                                    nodeId, "OutputArguments", attributeList, "UAMethod_OutParameters");
                                BuildDataBlockElements.XElementDataBlocks.Add(outputArguments);
                            }

                        }

                        #endregion
            */

            #region CONVERT THE DATA BLOCK INSTANCE VARIABLES TO OPC UA FORMAT

            // Save the current count before adding variables, to detect empty DBs later
            int currentElementsCount = BuildDataBlockElements.XElementDataBlocks.Count();
            
            XNamespace nsInterface = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
            XElement sectionStatic = attributeList
                .Element("Interface")
                ?.Element(nsInterface + "Sections")
                ?.Elements(nsInterface + "Section")
                .FirstOrDefault(s => (string)s.Attribute("Name") == "Static");

            string paramStructName = string.Empty;
            string argumentType = string.Empty;

            if (sectionStatic != null)
            {
                if (sectionStatic.Elements(nsInterface + "Member")
                    .Any(m => (string)m.Attribute("Name") == "UAMethod_InParameters"))
                {
                    paramStructName = "UAMethod_InParameters";
                    argumentType = "InputArguments";
                }
                else if (sectionStatic.Elements(nsInterface + "Member")
                    .Any(m => (string)m.Attribute("Name") == "UAMethod_OutParameters"))
                {
                    paramStructName = "UAMethod_OutParameters";
                    argumentType = "OutputArguments";
                }
            }
            BuildDataBlockElements.BuildXElement(attributeList, blockName, typeOfDB, isSafety, paramStructName, argumentType);

            #endregion

            #region DELETE DATA BLOCK IF NO VARIABLES ARE ADDED

            int newElementsCount = BuildDataBlockElements.XElementDataBlocks.Count();
            if (currentElementsCount == newElementsCount && AddInExecutionContext.Current.KeepEmptyDBs == false)
            {
                // No new nodes were added to the server interface
                BuildDataBlockElements.XElementDataBlocks.Remove(uaObjectElement);
                LogMessages.PublishLog($@"EMPTY DB: The Instance DB ""{blockName}"" has not been added to the server interface as it does not contain any variables accesible via OPC UA.");
            }

            #endregion

        }

        /*

        // Builds a UAVariable node for InputArguments or OutputArguments of a UAMethod.

        private static XElement BuildMethodArguments(
            string nodeId,
            string argumentType,     // "InputArguments" or "OutputArguments"
            XElement attributeList,
            string paramStructName)  // "UAMethod_InParameters" or "UAMethod_OutParameters"
        {
            XNamespace ns = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
            XNamespace uax = "http://opcfoundation.org/UA/2008/02/Types.xsd";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace rootNs = AddInExecutionContext.Current.RootNameSpace;

            // Navigate to the static section 
            
            XElement sectionStatic = attributeList
                .Element("Interface")
                .Element(ns + "Sections")
                .Elements(ns + "Section")
                .FirstOrDefault(s => (string)s.Attribute("Name") == "Static");

            // Find the struct member

            XElement paramMember = sectionStatic
                ?.Elements(ns + "Member")
                .FirstOrDefault(m => (string)m.Attribute("Name") == paramStructName);

            // Build one ExtensionObject per child member

            var arguments = new List<XElement>();

            if (paramMember != null)
            {
                List<XElement> childMembers = ResolveMembers(paramMember, ns);

                foreach (XElement childMember in childMembers)
                {
                    string memberName = (string)childMember.Attribute("Name") ?? "Unknown";
                    string rawType = (string)childMember.Attribute("Datatype") ?? "Bool";
                    string cleanType = rawType.Trim('"');
                    string dataTypeId = string.Empty;

                    // Check if the Input/Output Argument is defined as a struct or a UDT
                    bool isStruct = false;
                    (isStruct, dataTypeId) = ResolveDataTypeId(cleanType);

                    if (isStruct)
                    {
                        arguments.Add(
                            new XElement(uax + "ExtensionObject",
                                new XElement(uax + "TypeId",
                                    new XElement(uax + "Identifier", "i=297")),
                                new XElement(uax + "Body",
                                    new XElement(uax + "Argument",
                                        new XElement(uax + "Name", memberName),
                                        new XElement(uax + "DataType",
                                            new XElement(uax + "Identifier", dataTypeId)),
                                        new XElement(uax + "ValueRank", "-1"),
                                        new XElement(uax + "ArrayDimensions"),
                                        new XElement(uax + "Description",
                                            new XAttribute(xsi + "nil", "true"))))));
                    }
                    else
                    {
                        List<(string name, string datatype)> udtChildren = AddChildElementsOfUdt(cleanType);

                        foreach (var (childName, childDatatype) in udtChildren)
                        {
                            (bool isUdt, string childDataTypeId) = ResolveDataTypeId(childDatatype);

                            arguments.Add(
                                new XElement(uax + "ExtensionObject",
                                    new XElement(uax + "TypeId",
                                        new XElement(uax + "Identifier", "i=297")),
                                    new XElement(uax + "Body",
                                        new XElement(uax + "Argument",
                                            new XElement(uax + "Name", $"{memberName}.{childName}"),
                                            new XElement(uax + "DataType",
                                                new XElement(uax + "Identifier", childDataTypeId)),
                                            new XElement(uax + "ValueRank", "-1"),
                                            new XElement(uax + "ArrayDimensions"),
                                            new XElement(uax + "Description",
                                                new XAttribute(xsi + "nil", "true"))))));
                        }
                    }
                }

            }

        */
        /*
            // Build and return the full UAVariable node

            return new XElement(rootNs + "UAVariable",
                    new XAttribute("NodeId", $"ns=2;s={nodeId}.Method.{argumentType}"),
                    new XAttribute("BrowseName", $"0:{argumentType}"),
                    new XAttribute("ParentNodeId", $"ns=2;s={nodeId}.Method"),
                    new XAttribute("DataType", "i=296"),
                    new XAttribute("AccessLevel", "1"),
                    new XAttribute("ValueRank", "1"),
                    new XElement(rootNs + "DisplayName", argumentType),
                    new XElement(rootNs + "References",
                        new XElement(rootNs + "Reference",
                            new XAttribute("ReferenceType", "HasTypeDefinition"),
                            "i=68"),
                        new XElement(rootNs + "Reference",
                            new XAttribute("ReferenceType", "HasModellingRule"),
                            "i=78"),
                        new XElement(rootNs + "Reference",
                            new XAttribute("ReferenceType", "HasProperty"),
                            new XAttribute("IsForward", "false"),
                            $"ns=2;s={nodeId}.Method")
                    ),
                    new XElement(rootNs + "Value",
                        new XElement(uax + "ListOfExtensionObject",
                            new XAttribute(XNamespace.Xmlns + "uax", uax),
                            arguments))
                );
        }

        // Function to resolve the data type inside of a method

        private static (bool isStruct, string datatype) ResolveDataTypeId(string cleanType)
        {
            // Check if the data type is one of the defined at _dataTypeMap
            if (_dataTypeMap.TryGetValue(cleanType, out string mapped))
            {
                return (true, mapped);
            }
            else
            {
                return (false, mapped);
            }
        }
        private static List<(string name, string datatype)> AddChildElementsOfUdt(string cleanType)
        {
            var result = new List<(string name, string datatype)>();
            string dataTypeNodeId = "ns=2;s=DT_" + cleanType;
            XNamespace ns = "http://opcfoundation.org/UA/2011/03/UANodeSet.xsd";
            XDocument xDoc = Ctx.OpcUaInterface;

            // Both UserDataTypes and SystemDataTypes share the same logic
            if (UserSystemDataTypes.UserDataTypes.Contains(cleanType) ||
                UserSystemDataTypes.SystemDataTypes.Contains(cleanType))
            {
                XElement dataType = xDoc.Descendants(ns + "UADataType")
                    .FirstOrDefault(e => (string)e.Attribute("NodeId") == dataTypeNodeId);

                XElement definition = dataType?.Element(ns + "Definition");
                IEnumerable<XElement> fields = definition?.Elements(ns + "Field") ?? Enumerable.Empty<XElement>();

                foreach (XElement field in fields)
                {
                    string name = (string)field.Attribute("Name");
                    string datatype = (string)field.Attribute("DataType");
                    result.Add((name, datatype));
                }
            }

            // If cleanType is not found or has no fields, an empty list is returned
            return result;
        }

        // Function to get the full list of members of an element, regardless of whether they are inside of a struct variable or contained in a dataType variable. 
        private static List<XElement> ResolveMembers(XElement paramMember, XNamespace ns)
        {
            var result = new List<XElement>();
            var children = paramMember.Elements(ns + "Member").ToList();

            if (children.Count > 0)
            {
                foreach (XElement child in children)
                {
                    var grandChildren = child.Elements(ns + "Member").ToList();

                    if (grandChildren.Count > 0)
                    {
                        // Child is a nested Struct/UDT — go one level deeper
                        foreach (XElement grandChild in grandChildren)
                        {
                            result.Add(grandChild);
                        }
                    }
                    else
                    {
                        // Child is added directly
                        result.Add(child);
                    }
                }
            }
            else
            {
                // No direct children, the member is treated as a single argument
                result.Add(paramMember);
            }

            return result;
        }*/



    }
    #endregion
}

