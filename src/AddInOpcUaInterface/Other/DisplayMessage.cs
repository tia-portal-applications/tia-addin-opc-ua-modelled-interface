using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Siemens.Engineering;

namespace AddInOpcUaInterface.Other
{
    internal static class DisplayMessage
    {
        private static int _oneSecond = 1000;

        private static object _exclusiveAccess;    // Stored as object to satisfy TIA Portal V20+ Publisher
        public static void SetExclusiveAccess(ExclusiveAccess value) => _exclusiveAccess = value;
        internal static ExclusiveAccess GetExclusiveAccess() => (ExclusiveAccess)_exclusiveAccess;

        /// <summary>
        /// Displays an error message.
        /// </summary>
        /// <param name="message">The error message to be displayed.</param>
        public static void ErrorMessage(string message)
        {
            GetExclusiveAccess().Text = "Error!" + Environment.NewLine + Environment.NewLine
                                 + message + Environment.NewLine + Environment.NewLine + Environment.NewLine
                                 + "Press Cancel to continue".PadLeft(60);
            while (GetExclusiveAccess().IsCancellationRequested == false)
            {
                Thread.Sleep(_oneSecond);
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Displays a success message.
        /// </summary>
        /// <param name="message">The success message to be displayed.</param>
        public static void SuccessMessage(string message)
        {
            GetExclusiveAccess().Text = "Task completed successfully!" + Environment.NewLine + Environment.NewLine
                                 + message + Environment.NewLine + Environment.NewLine + Environment.NewLine
                                 + "Press Cancel to continue".PadLeft(60);
            while (GetExclusiveAccess().IsCancellationRequested == false)
            {
                Thread.Sleep(_oneSecond);
            }
            Environment.Exit(0);
        }
    }
}
