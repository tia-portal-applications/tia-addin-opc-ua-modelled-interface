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
            var ctx = AddInExecutionContext.Current;
            try
            {
                ServerInterface serverInterface = ctx.GetServerInterfaces().Create(ctx.InterfaceName);
                serverInterface.Author = "Siemens Add-In";
                try
                {
                    serverInterface.Import(new FileInfo(ctx.FilePath));

                    // Get the elapsed time in milliseconds
                    stopwatch.Stop();
                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                    string numberDefaultNodes = ctx.NumberDefaultNodes.ToString();
                    string numberUserSystemDataTypes = ctx.NumberUserSystemDataTypes.ToString();
                    string numberTags = ctx.NumberTags.ToString();
                    string numberGlobalDBs = ctx.NumberGlobalDBs.ToString();
                    string numberInstanceDBs = ctx.NumberInstanceDBs.ToString();

                    int totalInterfaceElements = ctx.NumberDefaultNodes + ctx.NumberUserSystemDataTypes + ctx.NumberTags
                                               + ctx.NumberGlobalDBs + ctx.NumberInstanceDBs;

                    SummaryWindow summaryWindow = new SummaryWindow();
                    summaryWindow.SetSummary(ctx.InterfaceName, elapsedSeconds, totalInterfaceElements);
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
