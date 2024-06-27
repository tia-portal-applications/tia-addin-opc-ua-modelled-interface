using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Siemens.Engineering.SW.OpcUa;
using Siemens.Engineering;
using AddInOpcUaInterface.Other;
using AddInOPCUAInterface.UI;

namespace AddInOpcUaInterface.Phases
{
    internal static class ImportServerInterface
    {
        /// <summary>
        /// Imports the server's XML file back into TIA's project.
        /// </summary>
        /// <param name="stopwatch"></param>
        public static void Import(Stopwatch stopwatch)
        {
            try
            {
                ServerInterface serverInterface = ProjectFields.ServerInterfaces.Create(AddInFields.InterfaceName);
                serverInterface.Author = "Siemens Add-In";
                try
                {
                    serverInterface.Import(new FileInfo(AddInFields.FilePath));

                    // Get the elapsed time in milliseconds
                    stopwatch.Stop();
                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                    string numberDefaultNodes = AddInFields.NumberDefaultNodes.ToString();
                    string numberUserSystemDataTypes = AddInFields.NumberUserSystemDataTypes.ToString();
                    string numberTags = AddInFields.NumberTags.ToString();
                    string numberGlobalDBs = AddInFields.NumberGlobalDBs.ToString();
                    string numberInstanceDBs = AddInFields.NumberInstanceDBs.ToString();

                    int totalInterfaceElements = AddInFields.NumberDefaultNodes + AddInFields.NumberUserSystemDataTypes + AddInFields.NumberTags
                                               + AddInFields.NumberGlobalDBs + AddInFields.NumberInstanceDBs;

                    SummaryWindow summaryWindow = new SummaryWindow();
                    summaryWindow.SetSummary(AddInFields.InterfaceName, elapsedSeconds, totalInterfaceElements);
                    summaryWindow.SetNumberNodes(numberDefaultNodes, numberUserSystemDataTypes, numberTags, numberGlobalDBs, numberInstanceDBs);
                    summaryWindow.ShowDialog();
                }
                catch
                {
                    serverInterface.Delete();
                    DisplayMessage.ErrorMessage("Interface import failed.");
                }
            }
            catch
            {
                DisplayMessage.ErrorMessage("Maximum number of server interfaces has been reached!");
            }
        }
    }
}
