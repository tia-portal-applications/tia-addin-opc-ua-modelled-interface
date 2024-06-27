using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AddInOPCUAInterface.UI.Actions
{
    public static class UserInputFields
    {
        // Variables for both "Create" and "Extend Create" windows
        public static string NewestInterface;                       // Name of the newest interface in the project: it will be used as the default value
        public static string InterfaceName;                         // Name of the new server interface set by the user
        public static string Path;                                  // Path to store the xml file
        public static string URI;                                   // Namespace URI of the server interface
        public static bool StopApplication;                         // Boolean value to inform if the Add-In was close

        private static List<string> _existingInterfaceNames;        // Names of existing interfaces in the project
        private static List<string> _existingInterfaceNamesToLower; // Names of existing interfaces in the project in lower case
        public static List<string> ExistingInterfaceNames
        {
            get => _existingInterfaceNames;
            set
            {
                _existingInterfaceNames = value;
                _existingInterfaceNamesToLower = _existingInterfaceNames.Select(s => s.ToLower()).ToList();
            }
        }

        // Exclusive variables for the "Extend Create" window

        // Values for the Access Level filter: "Not Accessible"/"Read only"/"Write only"/"Read Write"/"Project's access levels"
        public static int InputsAccessLevel;                        // Access level for input variables
        public static int MemoryAccessLevel;                        // Access level for memory variables
        public static int OutputsAccessLevel;                       // Access level for output variables
        public static int CountersAccessLevel;                      // Access level for counters
        public static int TimersAccessLevel;                        // Access level for timers
        public static int GlobalDBsAccessLevel;                     // Access level for Global DB variables
        public static int InstanceDBsAccessLevel;                   // Access level for Instance DB variables
        public static int SafetyGlobalDBsAccessLevel;               // Access level for Safety Global DB variables
        public static int SafetyInstanceDBsAccessLevel;             // Access level for Safety Instance DB variables
        
        public static string NodeIdentifier;                        // Use "string"/"numeric" node identifiers
        public static bool OptimizedData;                           // Use "Not optimized"/"optimized" server interface
        public static bool KeepEmptyDBs;                            // Keep empty Data Blocks
        public static bool KeepFolderStructure;                     // Keep the folder structure present in the project

        // Common methods for "Create" and "Extend Create"

        /// <summary>
        /// Updates the appearance of the GUI when a change is detected in the text box: "txt_InterfaceName".
        /// </summary>
        /// <param name="txt_InterfaceName"></param>
        /// <param name="txt_NamespaceURI"></param>
        /// <param name="txt_Path"></param>
        /// <param name="Error_NameExists"></param>
        /// <param name="btn_Create"></param>
        public static void InterfaceNameTextChanged(TextBox txt_InterfaceName, TextBox txt_NamespaceURI, TextBox txt_Path, Label Error_NameExists, Button btn_Create)
        {
            if (txt_InterfaceName.Text == string.Empty)
            {
                txt_InterfaceName.BorderBrush = Brushes.Red;
            }
            else
            {
                txt_InterfaceName.BorderBrush = Brushes.Green;
                txt_NamespaceURI.Text = "http://" + txt_InterfaceName.Text + ".com";
            }
            if (_existingInterfaceNames.Count != 0)
            {
                if (_existingInterfaceNamesToLower.Contains(txt_InterfaceName.Text.ToLower()))
                {
                    txt_InterfaceName.BorderBrush = Brushes.Red;
                    Error_NameExists.Visibility = Visibility.Visible;
                }
                else
                {
                    txt_InterfaceName.BorderBrush = Brushes.Green;
                    Error_NameExists.Visibility = Visibility.Hidden;
                }
            }
            if (txt_Path.BorderBrush == Brushes.Green && txt_InterfaceName.BorderBrush == Brushes.Green)
            {
                btn_Create.IsEnabled = true;
            }
            else
            {
                btn_Create.IsEnabled = false;
            }
        }

        /// <summary>
        /// Stores the path in which the server interface XML file will be exported.
        /// </summary>
        /// <param name="txt_Path"></param>
        public static void SelectFilePath(TextBox txt_Path)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "XML file (*.xml)|*.xml";

            bool? response = saveFileDialog.ShowDialog();

            if (response == true)
            {
                Path = txt_Path.Text = saveFileDialog.FileName;
            }
        }

        /// <summary>
        /// Updates the appearance of the GUI when a change is detected in the text box: "txt_Path".
        /// </summary>
        /// <param name="txt_Path"></param>
        public static void PathTextChanged(TextBox txt_InterfaceName, TextBox txt_Path, Button btn_Create)
        {
            if (txt_Path.Text == string.Empty)
            {
                txt_InterfaceName.BorderBrush = Brushes.Red;
            }
            else
            {
                txt_Path.BorderBrush = Brushes.Green;
            }
            if (txt_Path.BorderBrush == Brushes.Green && txt_InterfaceName.BorderBrush == Brushes.Green)
            {
                btn_Create.IsEnabled = true;
            }
            else
            {
                btn_Create.IsEnabled = false;
            }
        }

        /// <summary>
        /// Displays suggestions to the user on how to name the server interface.
        /// These suggestions are based on already existing interfaces from the project.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="txt_InterfaceName"></param>
        public static void InterfaceNamePreview(KeyEventArgs e, TextBox txt_InterfaceName)
        {

            if (e.Key == Key.Tab && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                // Trigger suggestion logic when Ctrl + Tab is pressed
                UserInputFields.GetBestSuggestions(txt_InterfaceName);
                e.Handled = true;
            }
            if ((e.Key == Key.Right || e.Key == Key.Left) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                // Trigger suggestion cycling logic when Ctrl + Left/Right arrow is pressed
                try
                {
                    int totalCount = UserInputFields.ExistingInterfaceNames.Count;
                    UserInputFields.MoveThroughSuggestions(txt_InterfaceName, e.Key == Key.Right, totalCount);
                    e.Handled = true;
                }
                catch { }
            }
        }

        private static int _currentSuggestionIndex = 0;

        /// <summary>
        /// Displays the name of an existing server interface that starts with the same letters as the one introduced in the "txt_InterfaceName".
        /// </summary>
        /// <param name="txt_InterfaceName"></param>
        public static void GetBestSuggestions(TextBox txt_InterfaceName)
        {
            string userInput = txt_InterfaceName.Text.ToLower();

            if (NewestInterface.ToLower().StartsWith(userInput))
            {
                txt_InterfaceName.Text = NewestInterface;
            }
            else
            {
                foreach (string suggestion in ExistingInterfaceNames)
                {
                    if (suggestion.ToLower().StartsWith(userInput))
                    {
                        txt_InterfaceName.Text = suggestion;
                    }
                }
            }
            txt_InterfaceName.CaretIndex = txt_InterfaceName.Text.Length;
        }

        /// <summary>
        /// Allows the user to cycle through all the names of existing server interfaces.
        /// </summary>
        /// <param name="txt_InterfaceName"></param>
        /// <param name="moveForward"></param>
        /// <param name="totalCount"></param>
        public static void MoveThroughSuggestions(TextBox txt_InterfaceName, bool moveForward, int totalCount)
        {
            if (moveForward)
            {
                // Move to the next suggestion
                _currentSuggestionIndex = (_currentSuggestionIndex + 1) % totalCount;
            }
            else
            {
                // Move to the previous suggestion
                _currentSuggestionIndex = (_currentSuggestionIndex - 1 + totalCount) % totalCount;
            }

            txt_InterfaceName.Text = UserInputFields.ExistingInterfaceNames[_currentSuggestionIndex];
            txt_InterfaceName.CaretIndex = txt_InterfaceName.Text.Length;
        }
    }
}
