using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Xml.Linq;
using AddInOpcUaInterface.Other;
using AddInOpcUaInterface.Phases.Phase4;
using Siemens.Engineering;
using Siemens.Engineering.Hmi.Tag;
using Siemens.Engineering.Multiuser;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Blocks.Interface;
using Siemens.Engineering.SW.Types;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace AddInOpcUaInterface.Phases
{
    internal static class DataBlocksInstance
    {
        private static int _numberOfProcessedDBs = 0;

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
                if (block.ProgrammingLanguage == ProgrammingLanguage.DB && AddInFields.InstanceDBsAccessLevel != 0)
                {
                    NewDataBlockElement(block, parentFolder, false);
                }
                //Only process failsafe Datablocks
                if (block.ProgrammingLanguage == ProgrammingLanguage.F_DB && AddInFields.SafetyInstanceDBsAccessLevel != 0)
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
            if (AddInFields.KeepFolderStructure)
            {
                // Replicate the folder structure of the project
                string folderName = blockGroup.Name;
                string fullPathName = parentFolder + '.' + folderName;
                XElement uaObjectElement =
                    new XElement(AddInFields.RootNameSpace + "UAObject",
                        new XAttribute("NodeId", $"ns=2;s={fullPathName}"),
                        new XAttribute("BrowseName", $"2:{folderName}"),
                        new XElement(AddInFields.RootNameSpace + "DisplayName", folderName),
                        new XElement(AddInFields.RootNameSpace + "References",
                            new XElement(AddInFields.RootNameSpace + "Reference", $"ns=2;s={parentFolder}",
                                new XAttribute("ReferenceType", "HasComponent"),
                                new XAttribute("IsForward", "false")
                            ),
                            new XElement(AddInFields.RootNameSpace + "Reference", "i=61",
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
                if (block.ProgrammingLanguage == ProgrammingLanguage.DB && AddInFields.InstanceDBsAccessLevel != 0)
                {
                    NewDataBlockElement(block, parentFolder, false);
                }
                //Only process failsafe Datablocks
                if (block.ProgrammingLanguage == ProgrammingLanguage.F_DB && AddInFields.SafetyInstanceDBsAccessLevel != 0)
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
                DisplayMessage.ExclusiveAccess.Text = $@"Adding ""Instance"" DBs to the server interface... Count: {_numberOfProcessedDBs}";
            }

            #region EXPORT DATA BLOCK FROM TIA'S PROJECT

            // Check if the data block is consistent (the project has been compiled)
            bool isConsistent = (bool)block.GetAttribute("IsConsistent");
            if (isConsistent == false)
            {
                DisplayMessage.ErrorMessage("Some elements are not consistent. Please compile the project before running the Add-In.");
            };

            // Export the data block as a .txt file
            string filePath = Path.ChangeExtension(AddInFields.FilePath, ".txt");
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
            if(inputElement.Element("SW.Blocks.InstanceDB") != null)
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

            #region CREATE THE DATA BLOCK INSTANCE OBJECT FOR THE SERVER INTERFACE

            string blockName = block.Name;
            string nodeId = '"' + blockName + '"';
            // Creating the UAObject element
            XElement uaObjectElement =
                new XElement(AddInFields.RootNameSpace + "UAObject",
                    new XAttribute("NodeId", $"ns=2;s={nodeId}"),
                    new XAttribute("BrowseName", $"2:{blockName}"),
                    new XElement(AddInFields.RootNameSpace + "DisplayName", blockName),
                    new XElement(AddInFields.RootNameSpace + "References",
                        new XElement(AddInFields.RootNameSpace + "Reference", $"ns=2;s={parentFolder}",
                            new XAttribute("ReferenceType", "HasComponent"),
                            new XAttribute("IsForward", "false")
                        ),
                        new XElement(AddInFields.RootNameSpace + "Reference", "i=58",
                            new XAttribute("ReferenceType", "HasTypeDefinition")
                        )
                    )
                );
            BuildDataBlockElements.XElementDataBlocks.Add(uaObjectElement);
            int currentElementsCount = BuildDataBlockElements.XElementDataBlocks.Count();
            #endregion

            #region  CONVERT THE DATA BLOCK INSTANCE VARIABLES TO OPC UA FORMAT

            BuildDataBlockElements.BuildXElement(attributeList, blockName, typeOfDB, isSafety);
            #endregion

            #region DELETE DATA BLOCK IF NO VARIABLES ARE ADDED

            int newElementsCount = BuildDataBlockElements.XElementDataBlocks.Count();
            if (currentElementsCount == newElementsCount && AddInFields.KeepEmptyDBs == false) // No new nodes were added to the server interface
            {
                BuildDataBlockElements.XElementDataBlocks.Remove(uaObjectElement);
                LogMessages.PublishLog($@"EMPTY DB: The Instance DB ""{blockName}"" has not been added to the server interface as it does not contain any variables accesible via OPC UA.");
            }
            #endregion
        }
    }
}