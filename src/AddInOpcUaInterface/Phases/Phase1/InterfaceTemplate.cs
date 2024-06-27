using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Siemens.Engineering;
using AddInOpcUaInterface.Other;
using System.IO;

namespace AddInOpcUaInterface.Phases
{
    internal static class InterfaceTemplate
    {
        private static List<string> _templateDataTypes = new List<string>();    // All project data types (for tag tables, data blocks, etc.)
        private static List<string> _tagTableDataTypes = new List<string>();    // Data types that are supported in tag tables

        public static List<string> TemplateDataTypes { get => _templateDataTypes; }
        public static List<string> TagTableDataTypes { get => _tagTableDataTypes; }

        /// <summary>
        /// Imports the server's XML template and stores it as an XDocument.
        /// </summary>
        /// <param name="file"></param>
        public static void ImportTemplate(Stream file)
        {
            var fileContent = new StreamReader(file).ReadToEnd();

            fileContent = fileContent.Replace("{insert_Date}", System.DateTime.Now.ToString("yyyy-MM-ddTHH:MM:ss"));
            fileContent = fileContent.Replace("{insert_namespaceUri}", AddInFields.InterfaceURI);
            fileContent = fileContent.Replace("{insert_interfaceName}", AddInFields.InterfaceName);
            AddInFields.OpcUaInterface = XDocument.Parse(fileContent);

            AddInFields.RootNameSpace   = AddInFields.OpcUaInterface.Root.Name.NamespaceName;
            AddInFields.RootNameSpaceSi = AddInFields.OpcUaInterface.Root.GetNamespaceOfPrefix("si");
        }

        /// <summary>
        /// Counts the number of nodes in the InterfaceTemplate.xml file.
        /// </summary>
        /// <param name="opcuaInterface">XDocument with the contents of the InterfaceTemplate.xml file</param>
        /// <returns>The number of nodes defined in the xml template.</returns>
        public static int GetTotalInterfaceElements()
        {
            XDocument opcuaTemplate = AddInFields.OpcUaInterface;
            int totalInterfaceElements = 0;
            
            // The template for the interface is composed of: UADataTypes, UAVariables, UAObjectTypes, UAObjects
            IEnumerable<XElement> uaDataTypes = opcuaTemplate.Root.Elements(AddInFields.RootNameSpace + "UADataType");
            totalInterfaceElements += uaDataTypes.Count();
            IEnumerable<XElement> uaVariables = opcuaTemplate.Root.Elements(AddInFields.RootNameSpace + "UAVariable");
            totalInterfaceElements += uaVariables.Count();
            IEnumerable<XElement> uaObjectTypes = opcuaTemplate.Root.Elements(AddInFields.RootNameSpace + "UAObjectType");
            totalInterfaceElements += uaObjectTypes.Count();
            IEnumerable<XElement> uaObjects = opcuaTemplate.Root.Elements(AddInFields.RootNameSpace + "UAObject");
            totalInterfaceElements += uaObjects.Count();

            // Store the aliases of data types for later use
            IEnumerable<XElement> aliases = opcuaTemplate.Root.Element(AddInFields.RootNameSpace + "Aliases").Elements(AddInFields.RootNameSpace + "Alias");
            GetTemplateAliases(aliases);

            return totalInterfaceElements;
        }

        /// <summary>
        /// Adds all data types defined in the template to the collection "_templateDataTypes".
        /// Additionally, adds "tag table data types" to the collection "_tagTableDataTypes".
        /// </summary>
        /// <param name="aliases">"Alias" XElements from the template.</param>
        /// <returns>The total number of data types defined in the InterfaceTemplate.xml file</returns>
        public static int GetTemplateAliases(IEnumerable<XElement> aliases)
        {
            bool isTagTableDataTypes = true;

            foreach (XElement alias in aliases)
            {
                // Always add the data type to the project's data types
                _templateDataTypes.Add(alias.FirstAttribute.Value);

                // Only adds the data type to the tagTableDataTypes if its part of the "tag table data types".
                // The STRING alias is the first element in the template that is not part of the "tag table data types".
                if (alias.FirstAttribute.Value == "STRING")
                {
                    isTagTableDataTypes = false;
                }
                if (isTagTableDataTypes == true)
                {
                    _tagTableDataTypes.Add(alias.FirstAttribute.Value);
                }
            }
            return _templateDataTypes.Count;
        }
    }
}
