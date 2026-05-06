using AddInOpcUaInterface.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AddInOpcUaInterface.Phases.Phase4
{
    internal static class BuildMethod
    {
        private static AddInExecutionContext Ctx => AddInExecutionContext.Current;

        private static readonly Dictionary<string, string> _dataTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "bool",   "i=1"  },
            { "sint",   "i=2"  },
            { "byte",   "i=3"  },
            { "usint",  "i=3"  },
            { "int",    "i=4"  },
            { "word",   "i=4"  },
            { "dint",   "i=6"  },
            { "dword",  "i=6"  },
            { "lint",   "i=8"  },
            { "uint",   "i=5"  },
            { "udint",  "i=7"  },
            { "ulint",  "i=9"  },
            { "real",   "i=10" },
            { "lreal",  "i=11" },
            { "string", "i=12" },
            { "ldt",    "i=13" },
            { "wstring","i=12" },

            // OPC UA specific types

            { "OPC_UA_QUALIFIEDNAME",   "i=20" },
            { "OPC_UA_LOCALIZEDTEXT",   "i=21" },
            { "OPC_UA_NODEID",          "i=17" },
            { "OPC_UA_DATETIME",        "i=13" },
            { "OPC_UA_BYTESTRING",      "i=15" },
            { "OPC_UA_GUID",            "i=14" },
            { "OPC_UA_XMLELEMENT",      "i=16" },
            { "OPC_UA_STATUSCODE",      "i=19" },
            { "OPC_UA_ServerMethodPre", "i=58" },
            { "OPC_UA_ServerMethodPost","i=58" }
        };
        private static readonly XNamespace _ns = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
        private static readonly Regex _arrayTypeRegex = new Regex(@"^Array\[(.+)\]\s+of\s+(.+)$",RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Checks whether the instance DB belongs to a server method block
        ///and builds the UAMethod, InputArguments and OutputArguments nodes.
        /// </summary>
        /// <param name="nodeId">The NodeId of the parent DB object.</param>
        /// <param name="attributeList">The AttributeList XElement of the exported DB.</param>

        public static void CheckMethod(string nodeId, XElement attributeList)
        {
            XElement sectionStatic = attributeList
                .Element("Interface")
                .Element(_ns + "Sections")
                .Elements(_ns + "Section")
                .FirstOrDefault(s => (string)s.Attribute("Name") == "Static");

            // Check if this DB belongs to a server method block

            if (sectionStatic?.Elements(_ns + "Member")
                    .FirstOrDefault(m => (string)m.Attribute("Name") == "OPC_UA_ServerMethodPre_Instance") == null)
            {
                return;
            }

            bool hasInputArgs = sectionStatic.Elements(_ns + "Member")
                .Any(m => (string)m.Attribute("Name") == "UAMethod_InParameters");

            bool hasOutputArgs = sectionStatic.Elements(_ns + "Member")
                .Any(m => (string)m.Attribute("Name") == "UAMethod_OutParameters");

            // Build the UAMethod node

            BuildUAMethodNode(nodeId, hasInputArgs, hasOutputArgs);

            // Build InputArguments if the struct exists

            if (hasInputArgs)
            {
                BuildMethodArguments(nodeId, "InputArguments", attributeList, "UAMethod_InParameters");
            }

            // Build OutputArguments if the struct exists

            if (hasOutputArgs)
            {
               BuildMethodArguments(nodeId, "OutputArguments", attributeList, "UAMethod_OutParameters");
            }
        }

        /// <summary>
        /// Builds and adds the UAMethod node to the output list.
        /// </summary>
        /// <param name="nodeId"> The NodeId of the parent DB object.</param>
        /// <param name="hasInputArgs"> </param>
        /// <param name="hasOutputArgs"> </param>
        

        private static void BuildUAMethodNode(string nodeId, bool hasInputArgs, bool hasOutputArgs)
        {
            var rootNs = Ctx.RootNameSpace;
            var rootNsSi = Ctx.RootNameSpaceSi;
            var methodReferences = new List<XElement>
            {
                new XElement(rootNs + "Reference",
                    new XAttribute("ReferenceType", "HasModellingRule"),
                    "i=78"),

                new XElement(rootNs + "Reference",
                    new XAttribute("ReferenceType", "HasComponent"),
                    new XAttribute("IsForward", "false"),
                    $"ns=2;s={nodeId}")
            };

            if (hasInputArgs)
                methodReferences.Add(
                    new XElement(rootNs + "Reference",
                        new XAttribute("ReferenceType", "HasProperty"),
                        $"ns=2;s={nodeId}.Method.InputArguments"));

            if (hasOutputArgs)
                methodReferences.Add(
                    new XElement(rootNs + "Reference",
                        new XAttribute("ReferenceType", "HasProperty"),
                        $"ns=2;s={nodeId}.Method.OutputArguments"));

            XElement uaMethodElement =
                new XElement(rootNs + "UAMethod",
                    new XAttribute("NodeId", $"ns=2;s={nodeId}.Method"),
                    new XAttribute("BrowseName", "2:Method"),
                    new XAttribute("ParentNodeId", $"ns=2;s={nodeId}"),
                    new XAttribute("MethodDeclarationId", $"ns=2;s={nodeId}"),
                    new XElement(rootNs + "DisplayName", "Method"),
                    new XElement(rootNs + "References", methodReferences),
                    new XElement(rootNs + "Extensions",
                        new XElement(rootNs + "Extension",
                            new XElement(rootNsSi + "MethodMapping",
                                $"{nodeId}.Method")))
                );

            BuildDataBlockElements.XElementDataBlocks.Add(uaMethodElement);
        }

        /// <summary>
        /// Builds a UAVariable node for InputArguments or OutputArguments of a UAMethod.
        /// </summary>
        /// <param name="nodeId"> The NodeId of the parent DB object.</param>
        /// <param name="argumentType"> </param>
        /// <param name="attributeList"> The AttributeList XElement of the exported DB u</param>
        /// <param name="paramStructName"> </param>

        private static void BuildMethodArguments(
          string nodeId,
          string argumentType,
          XElement attributeList,
          string paramStructName)
        {
            XNamespace uax = "http://opcfoundation.org/UA/2008/02/Types.xsd";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace rootNs = Ctx.RootNameSpace;
            var extraNodes = new List<XElement>();

            XElement sectionStatic = attributeList
                    .Element("Interface")
                    .Element(_ns + "Sections")
                    .Elements(_ns + "Section")
                    .FirstOrDefault(s => (string)s.Attribute("Name") == "Static");

            XElement paramMember = sectionStatic
                    ?.Elements(_ns + "Member")
                    .FirstOrDefault(m => (string)m.Attribute("Name") == paramStructName);

            var arguments = new List<XElement>();

            if (paramMember != null)

            {
                List<XElement> childMembers = ResolveMembers(paramMember, _ns);

                foreach (XElement childMember in childMembers)
                {
                    string memberName = (string)childMember.Attribute("Name") ?? "Unknown";
                    string rawType = (string)childMember.Attribute("Datatype") ?? "Bool";
                    string cleanType = rawType.Trim('"');

                    (bool isStruct, string dataTypeId) = ResolveDataTypeId(cleanType);

                    if (isStruct)
                    {
                        // Flatten all fields as scalar arguments ──
                        var udtMembers = GetUdtFields(cleanType);

                        foreach (var (udtMemberName, udtMemberDatatype) in udtMembers)
                        {
                            string memberDataTypeId;

                            if (udtMemberDatatype.StartsWith("ns=") || udtMemberDatatype.StartsWith("i="))
                                memberDataTypeId = udtMemberDatatype;
                            else
                            {
                                var (_, resolvedId) = ResolveDataTypeId(udtMemberDatatype);
                                memberDataTypeId = resolvedId;
                            }

                            arguments.Add(
                                new XElement(uax + "ExtensionObject",
                                    new XElement(uax + "TypeId",
                                        new XElement(uax + "Identifier", "i=297")),
                                    new XElement(uax + "Body",
                                        new XElement(uax + "Argument",
                                            new XElement(uax + "Name", udtMemberName),
                                            new XElement(uax + "DataType",
                                                new XElement(uax + "Identifier", memberDataTypeId)),
                                            new XElement(uax + "ValueRank", "-1"),
                                            new XElement(uax + "ArrayDimensions"),
                                            new XElement(uax + "Description",
                                                new XAttribute(xsi + "nil", "true"))))));
                        }
                    }
                    else
                    {
                        // Scalar or Array primitive
                        var (isArray, elementType, dimensions) = ParseArrayType(cleanType);

                        if (isArray)
                        {
                            var (_, elemTypeId) = ResolveDataTypeId(elementType);

                            // Add argument entry to InputArguments / OutputArguments list
                            arguments.Add(
                                new XElement(uax + "ExtensionObject",
                                    new XElement(uax + "TypeId",
                                        new XElement(uax + "Identifier", "i=297")),
                                    new XElement(uax + "Body",
                                        new XElement(uax + "Argument",
                                            new XElement(uax + "Name", memberName),
                                            new XElement(uax + "DataType",
                                                new XElement(uax + "Identifier", elemTypeId)),
                                            new XElement(uax + "ValueRank", dimensions.Length.ToString()),
                                            new XElement(uax + "ArrayDimensions",
                                                dimensions.Select(d =>
                                                    new XElement(uax + "UInt32", d.ToString()))),
                                            new XElement(uax + "Description",
                                                new XAttribute(xsi + "nil", "true"))))));

                            //  Create one  UAVariable node for the array and UAVariable child node for array argument
                            extraNodes.Add(
                                new XElement(rootNs + "UAVariable",
                                    new XAttribute("NodeId", $"ns=2;s={nodeId}.Method.{argumentType}.{memberName}"),
                                    new XAttribute("BrowseName", $"2:{memberName}"),
                                    new XAttribute("ParentNodeId", $"ns=2;s={nodeId}.Method.{argumentType}"), 
                                    new XAttribute("DataType", elemTypeId),
                                    new XAttribute("AccessLevel", "3"),                                        
                                    new XAttribute("ValueRank", dimensions.Length.ToString()),
                                    new XAttribute("ArrayDimensions", string.Join(" ", dimensions)),               
                                    new XElement(rootNs + "DisplayName", memberName),
                                    new XElement(rootNs + "References",
                                        new XElement(rootNs + "Reference",
                                            new XAttribute("ReferenceType", "HasTypeDefinition"), "i=63"),
                                        new XElement(rootNs + "Reference",
                                            new XAttribute("ReferenceType", "HasProperty"),
                                            new XAttribute("IsForward", "false"),
                                            $"ns=2;s={nodeId}.Method.{argumentType}"))));                          
                        }
                        else
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
                    }
                }
            }
            // This node contains the argument definitions for the input/output parameters of the method.

            XElement inputOutputArgNode = new XElement(rootNs + "UAVariable",
                new XAttribute("NodeId", $"ns=2;s={nodeId}.Method.{argumentType}"),
                new XAttribute("BrowseName", argumentType),
                new XAttribute("ParentNodeId", $"ns=2;s={nodeId}.Method"),
                new XAttribute("DataType", "i=296"),
                new XAttribute("AccessLevel", "1"),
                new XAttribute("ValueRank", "1"),
                new XElement(rootNs + "DisplayName", argumentType),
                new XElement(rootNs + "References",
                    new XElement(rootNs + "Reference",
                        new XAttribute("ReferenceType", "HasTypeDefinition"), "i=68"),
                    new XElement(rootNs + "Reference",
                        new XAttribute("ReferenceType", "HasModellingRule"), "i=78"),
                    new XElement(rootNs + "Reference",
                        new XAttribute("ReferenceType", "HasProperty"),
                        new XAttribute("IsForward", "false"),
                        $"ns=2;s={nodeId}.Method")),
                new XElement(rootNs + "Value",
                    new XElement(uax + "ListOfExtensionObject",
                        new XAttribute(XNamespace.Xmlns + "uax", uax),
                        arguments)));

            // Add the InputArguments/OutputArguments container node
            BuildDataBlockElements.XElementDataBlocks.Add(inputOutputArgNode);
            // Append the child nodes to the associated node
            BuildDataBlockElements.XElementDataBlocks.AddRange(extraNodes);
        }

        /// <summary>
        /// Resolves the data type ID and whether the type is a UDT/Struct.
        /// </summary>
        /// <param name="isStruct"> </param>
        /// <param name="dataTypeId"> </param>
        /// <returns> Returns whether the type is a struct/UDT and the OPC UA data type NodeId </returns>

        private static (bool isStruct, string dataTypeId) ResolveDataTypeId(string cleanType)
        {
            string normalizedType = cleanType.Contains(':') ? cleanType.Substring(cleanType.LastIndexOf(':') + 1): cleanType;

            if (_dataTypeMap.TryGetValue(normalizedType, out string mapped))
                return (false, mapped);

            if (UserSystemDataTypes.UserDataTypes.Contains(normalizedType) ||
                UserSystemDataTypes.SystemDataTypes.Contains(normalizedType))
                return (true, "ns=2;s=DT_" + normalizedType);

            return (false, "i=24");
        }

        /// <summary>
        /// Parses a TIA Portal array type string into its components. For example: "Array[0..7] of Int".
        /// </summary>
        /// <param name="isArray"> </param>
        /// <param name="elementType"> </param>
        /// <param name="dimensions"> </param>
        /// <returns></returns>
        
        private static (bool isArray, string elementType, int[] dimensions) ParseArrayType(string rawType)
        {
            var match = _arrayTypeRegex.Match(rawType);

            if (!match.Success)
                return (false, rawType, null);

            string elementType = match.Groups[2].Value.Trim();
            string[] dimParts = match.Groups[1].Value.Split(',');

            var dims = new List<int>();
            foreach (string dim in dimParts)
            {
                var bounds = dim.Trim().Split(new[] { ".." }, StringSplitOptions.None);
                if (bounds.Length == 2
                    && int.TryParse(bounds[0].Trim(), out int lo)
                    && int.TryParse(bounds[1].Trim(), out int hi))
                {
                    dims.Add(hi - lo + 1); // size of this dimension
                }
                else
                {
                    dims.Add(0); // unknown dimension
                }
            }

            return (true, elementType, dims.ToArray());
        }

        /// <summary>
        /// Returns all fields (name + datatype) of a UDT from the already-built XElementUserSystemDataTypes list.
        /// </summary>

        private static List<(string name, string datatype)> GetUdtFields(string cleanType)
        {
            var result = new List<(string name, string datatype)>();
            string dataTypeNodeId = "ns=2;s=DT_" + cleanType;

            XElement dataTypeElement = UserSystemDataTypes.XElementUserSystemDataTypes
                .FirstOrDefault(e =>
                    e.Name.LocalName == "UADataType" &&
                    (string)e.Attribute("NodeId") == dataTypeNodeId);

            if (dataTypeElement == null)
                return result;

            XElement definition = dataTypeElement
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName == "Definition");

            if (definition == null)
                return result;

            foreach (XElement field in definition.Elements().Where(e => e.Name.LocalName == "Field"))
            {
                string name = (string)field.Attribute("Name");
                string dataType = (string)field.Attribute("DataType");

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(dataType))
                    result.Add((name, dataType));
            }

            return result;
        }
        /// <summary>
        /// Resolves the full list of members of a parameter struct member.
        /// </summary>

        private static List<XElement> ResolveMembers(XElement paramMember, XNamespace ns)
        {
            var result = new List<XElement>();
            var children = paramMember.Elements(ns + "Member").ToList();

            if (children.Count == 0)
            {
                result.Add(paramMember);
                return result;
            }

            foreach (XElement child in children)
            {
                string datatype = (string)child.Attribute("Datatype") ?? "";
                string cleanDatatype = datatype.Trim('"');
                var (isArray, _, _) = ParseArrayType(cleanDatatype);

                if (isArray)
                {
                    result.Add(child);
                    continue;
                }

                if (child.Elements(ns + "Member").Any())
                    result.AddRange(ResolveMembers(child, ns));
                else
                    result.Add(child);
            }

            return result;
        }

    }
}
        

