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

        private static ExclusiveAccess _exclusiveAccess;    // The exclusiveAccess variable is used to display messages in TIA
        public static ExclusiveAccess ExclusiveAccess   { get => _exclusiveAccess; set => _exclusiveAccess = value; }

        /// <summary>
        /// Displays an error message.
        /// </summary>
        /// <param name="message">The error message to be displayed.</param>
        public static void ErrorMessage(string message)
        {
            _exclusiveAccess.Text = "Error!" + Environment.NewLine + Environment.NewLine 
                                 + message  + Environment.NewLine + Environment.NewLine + Environment.NewLine
                                 + "Press Cancel to continue".PadLeft(60);
            while (_exclusiveAccess.IsCancellationRequested == false)
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
            _exclusiveAccess.Text = "Task completed successfully!" + Environment.NewLine + Environment.NewLine
                                 + message  + Environment.NewLine + Environment.NewLine + Environment.NewLine
                                 + "Press Cancel to continue".PadLeft(60);
            while (_exclusiveAccess.IsCancellationRequested == false)
            {
                Thread.Sleep(_oneSecond);
            }
            Environment.Exit(0);
        }
    }
}
