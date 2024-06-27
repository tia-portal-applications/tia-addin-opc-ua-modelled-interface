using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddInOpcUaInterface.Other
{
    internal static class LogMessages
    {
        private static string _logFilePath;

        private static int _numberOfLog = 0;

        /// <summary>
        /// Creates a new log file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="interfaceName"></param>
        public static void CreateLogFile()
        {
            _logFilePath = AddInFields.FilePath.Replace(".xml", "_log.txt");
            try
            {
                // Check if the file exists
                if (File.Exists(_logFilePath))
                {
                    // If the file exists, delete it to reset
                    File.Delete(_logFilePath);
                }

                // Create the file and write the initial content
                File.WriteAllText(_logFilePath, "Add-In logs."
                    + Environment.NewLine + $@"Server interface: {AddInFields.InterfaceName}"
                    + Environment.NewLine + $@"Creation date: {System.DateTime.Now.ToString("yyyy-MM-dd THH:MM:ss")}"
                    + Environment.NewLine + Environment.NewLine);
            }
            catch 
            {
                DisplayMessage.ErrorMessage("Unexpected error ocurred while creating the log file.");
            }
        }

        /// <summary>
        /// Publishes a new log entry in the log file.
        /// </summary>
        /// <param name="message">Message for the new log entry.</param>
        public static void PublishLog(string message) 
        {
            _numberOfLog += 1;
            try
            {
                File.AppendAllText(_logFilePath, Environment.NewLine + $"#{_numberOfLog} ".PadLeft(10) + message);
            }
            catch
            {
                DisplayMessage.ErrorMessage("Unexpected error ocurred while adding a new entry to the log file.");
            } 
        }
    }
}
