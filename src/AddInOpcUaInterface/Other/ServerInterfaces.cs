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
        /// <summary>
        /// Return a list of strings containing the names of all server interfaces in the project,
        /// along with the name of the most recently created interface.
        /// </summary>
        /// <param name="serverInterfaces"></param>
        /// <param name="simaticInterfaces"></param>
        /// <returns>A tuple containing (list of interface names, name of newest interface).</returns>
        public static (List<string> Names, string NewestInterface) InterfaceNames(ServerInterfaceComposition serverInterfaces, SimaticInterfaceComposition simaticInterfaces)
        {
            DateTime newestTimeStamp = DateTime.MinValue;
            string newestInterface = string.Empty;
            List<string> serverInterfaceNames = new List<string>();

            // Loop of server interfaces (blue)
            foreach (ServerInterface serverInterface in serverInterfaces)
            {
                serverInterfaceNames.Add(serverInterface.Name);
                if (newestTimeStamp < serverInterface.CreationTime)
                {
                    newestTimeStamp = serverInterface.CreationTime;
                    newestInterface = serverInterface.Name;
                }
            }

            // Loop of simatic interfaces (purple)
            foreach (SimaticInterface simaticInterface in simaticInterfaces)
            {
                serverInterfaceNames.Add(simaticInterface.Name);
                if (newestTimeStamp < simaticInterface.CreationTime)
                {
                    newestTimeStamp = simaticInterface.CreationTime;
                    newestInterface = simaticInterface.Name;
                }
            }
            return (serverInterfaceNames, newestInterface);
        }
    }
}
