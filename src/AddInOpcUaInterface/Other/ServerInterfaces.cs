using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Siemens.Engineering.SW.OpcUa;
using Siemens.Engineering;
using System.Windows.Shapes;

namespace AddInOpcUaInterface.Phases
{
    internal static class ServerInterfaces
    {
        private static string _newestInterface = string.Empty;
        public static string NewestInterface { get => _newestInterface; }

        /// <summary>
        /// Return a list of strings containing the names of all server interfaces in the project.
        /// Additionally, it updates the private variable "newestInterface" with the name of the last created interface.
        /// </summary>
        /// <param name="serverInterfaces"></param>
        /// <param name="simaticInterfaces"></param>
        /// <returns>A list of strings containing the names of all server interfaces.</returns>
        public static List<string> InterfaceNames(ServerInterfaceComposition serverInterfaces, SimaticInterfaceComposition simaticInterfaces)
        {
            DateTime newestTimeStamp = DateTime.MinValue;
            List<string> serverInterfaceNames = new List<string>();

            // Loop of server interfaces (blue)
            foreach (ServerInterface serverInterface in serverInterfaces)
            {
                serverInterfaceNames.Add(serverInterface.Name);
                if (newestTimeStamp < serverInterface.CreationTime)
                {
                    newestTimeStamp = serverInterface.CreationTime;
                    _newestInterface = serverInterface.Name;
                }
            }

            // Loop of simatic interfaces (purple)
            foreach (SimaticInterface simaticInterface in simaticInterfaces)
            {
                serverInterfaceNames.Add(simaticInterface.Name);
                if (newestTimeStamp < simaticInterface.CreationTime)
                {
                    newestTimeStamp = simaticInterface.CreationTime;
                    _newestInterface = simaticInterface.Name;
                }
            }
            return serverInterfaceNames;
        }
    }
}
