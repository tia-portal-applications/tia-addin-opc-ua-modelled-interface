using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AddInOPCUAInterface.UI.Actions;

namespace AddInOPCUAInterface.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ExtendCreateWindow : Window
    {
        public ExtendCreateWindow()
        {
            InitializeComponent();
            this.Topmost = true;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            DataContext = this;

            txt_InterfaceName.BorderBrush = Brushes.Red;
            txt_Path.BorderBrush = Brushes.Red;
            btn_Create.IsEnabled = false;

            txt_InterfaceName.BorderBrush = Brushes.Red;
            txt_Path.BorderBrush = Brushes.Red;

            // Initialize interface elements
            CheckBox_Optimized_Data.IsChecked   = true;
            CheckBox_String_Id.IsChecked        = true;
            CheckBox_NoKeepEmpty_DBs.IsChecked  = true;
            CheckBox_NoFolderStructure.IsChecked = true;
            InitializeSliders();
        }

        /// <summary>
        /// Stores the user input in the UserInputFields variables and closes the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Create_Click(object sender, RoutedEventArgs e)
        {
            UserInputFields.InterfaceName = txt_InterfaceName.Text;
            SetSliderValues();
            Close();
        }

        /// <summary>
        /// Updates the appearance of the GUI when a change is detected in the text box: "txt_InterfaceName".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_InterfaceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            UserInputFields.InterfaceNameTextChanged(txt_InterfaceName, txt_NamespaceURI, txt_Path, Error_NameExists, btn_Create);
        }

        /// <summary>
        /// Stores the path in which the server interface XML file will be exported.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            UserInputFields.SelectFilePath(txt_Path);
        }

        /// <summary>
        /// Updates the appearance of the GUI when a change is detected in the text box: "txt_Path".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_Path_TextChanged(object sender, TextChangedEventArgs e)
        {
            UserInputFields.PathTextChanged(txt_InterfaceName, txt_Path, btn_Create);
        }

        /// <summary>
        /// Stores the URI variable in the UserInputFields.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_NamespaceURI_TextChanged(object sender, TextChangedEventArgs e)
        {
            UserInputFields.URI = txt_NamespaceURI.Text;
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            UserInputFields.StopApplication = true;
            Close();
        }

        /// <summary>
        /// Displays suggestions to the user on how to name the server interface.
        /// These suggestions are based on already existing interfaces from the project.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_InterfaceName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            UserInputFields.InterfaceNamePreview(e, txt_InterfaceName);
        }

        /// <summary>
        /// Allows the window to be dragged around.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }


        #region OPTIMIZATION OF THE SERVER INTRFACE

        private void CheckBox_Optimize_Data_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox_Optimized_Data.IsChecked = true;
            CheckBox_NotOptimized_Data.IsChecked = false;
            UserInputFields.OptimizedData = true;
        }

        private void CheckBox_NotOptimize_Data_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox_Optimized_Data.IsChecked = false;
            CheckBox_NotOptimized_Data.IsChecked = true;
            UserInputFields.OptimizedData = false;
        }
        #endregion

        #region ACCESS LEVEL

        /// <summary>
        /// Resets the sliders to the default when the window is initialized.
        /// </summary>
        private void InitializeSliders()
        {
            Sld_AL_Inputs.Value             = 4;
            Sld_AL_Memory.Value             = 4;
            Sld_AL_Outputs.Value            = 4;
            Sld_AL_Counters.Value           = 4;
            Sld_AL_Timers.Value             = 4;
            Sld_AL_GlobalDBs.Value          = 4;
            Sld_AL_InstanceDBs.Value        = 4;
            Sld_AL_SafetyGlobalDBs.Value    = 1;
            Sld_AL_SafetyInstanceDBs.Value  = 1;
        }

        /// <summary>
        /// Stores the values from all sliders as UserInputFields variables.
        /// </summary>
        private void SetSliderValues()
        {
            UserInputFields.InputsAccessLevel               = (int)Sld_AL_Inputs.Value;
            UserInputFields.MemoryAccessLevel               = (int)Sld_AL_Memory.Value;
            UserInputFields.OutputsAccessLevel              = (int)Sld_AL_Outputs.Value;
            UserInputFields.CountersAccessLevel             = (int)Sld_AL_Counters.Value;
            UserInputFields.TimersAccessLevel               = (int)Sld_AL_Timers.Value;
            UserInputFields.GlobalDBsAccessLevel            = (int)Sld_AL_GlobalDBs.Value;
            UserInputFields.InstanceDBsAccessLevel          = (int)Sld_AL_InstanceDBs.Value;
            UserInputFields.SafetyGlobalDBsAccessLevel      = (int)Sld_AL_SafetyGlobalDBs.Value;
            UserInputFields.SafetyInstanceDBsAccessLevel    = (int)Sld_AL_SafetyInstanceDBs.Value;
        }

        /// <summary>
        /// Parses the int value of a slider to the corresponding access level string.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private string GetAccessLevel(int level)
        {
            // "Not Accessible"/"Read only"/"Write only"/"Read Write"/"Project's access levels"
            string accessLevel = "Project's access levels";

            if (level == 0)
            {
                accessLevel = "Not Accessible";
            }
            else if (level == 1)
            {
                accessLevel = "Read only";
            }
            else if (level == 2)
            {
                accessLevel = "Write only";
            }
            else if (level == 3)
            {
                accessLevel = "Read Write";
            }
            return accessLevel;
        }

        /// <summary>
        /// Snaps the slider's value to nearest interger when a change is detected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_Inputs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SnapValueSliderValue(Sld_AL_Inputs);
        }
        private void Slider_Memory_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SnapValueSliderValue(Sld_AL_Memory);
        }
        private void Slider_Outputs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SnapValueSliderValue(Sld_AL_Outputs);
        }
        private void Slider_Counters_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SnapValueSliderValue(Sld_AL_Counters);
        }
        private void Slider_Timers_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SnapValueSliderValue(Sld_AL_Timers);
        }
        private void Slider_GlobalDBs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SnapValueSliderValue(Sld_AL_GlobalDBs);
        }
        private void Slider_InstanceDBs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SnapValueSliderValue(Sld_AL_InstanceDBs);
        }
        private void Slider_SafetyGlobalDBs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SnapValueSliderValue(Sld_AL_SafetyGlobalDBs);
        }
        private void Slider_SafetyInstanceDBs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Snap the slider value to the nearest integer
            SnapValueSliderValue(Sld_AL_SafetyInstanceDBs);
        }

        /// <summary>
        /// Snaps the slider value to the nearest integer.
        /// </summary>
        /// <param name="slider"></param>
        private void SnapValueSliderValue(Slider slider)
        {
            double snappedValue = Math.Round(slider.Value);
            slider.Value = snappedValue;
        }
        #endregion

        /// <summary>
        /// Fills in the fields for the "Summary" tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tab_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (myTabs.SelectedItem != null)
            {
                TabItem selectedTab = (TabItem)myTabs.SelectedItem;
                string tabHeader = selectedTab.Header.ToString();

                if (tabHeader == "Summary")
                {
                    txt_Summary_interfaceName.Content = txt_InterfaceName.Text;
                    txt_Summary_namespaceUri.Content = txt_NamespaceURI.Text;
                    txt_Summary_exportPath.Content = txt_Path.Text;

                    SetSliderValues();

                    txt_Summary_InputsAccessLevel.Content               = GetAccessLevel((int)Sld_AL_Inputs.Value);
                    txt_Summary_MemoryAccessLevel.Content               = GetAccessLevel((int)Sld_AL_Memory.Value);
                    txt_Summary_OutputsAccessLevel.Content              = GetAccessLevel((int)Sld_AL_Outputs.Value);
                    txt_Summary_CountersAccessLevel.Content             = GetAccessLevel((int)Sld_AL_Counters.Value);
                    txt_Summary_TimersAccessLevel.Content               = GetAccessLevel((int)Sld_AL_Timers.Value);
                    txt_Summary_GlobalDBsAccessLevel.Content            = GetAccessLevel((int)Sld_AL_GlobalDBs.Value);
                    txt_Summary_InstanceDBsAccessLevel.Content          = GetAccessLevel((int)Sld_AL_InstanceDBs.Value);
                    txt_Summary_SafetyGlobalDBsAccessLevel.Content      = GetAccessLevel((int)Sld_AL_SafetyGlobalDBs.Value);
                    txt_Summary_SafetyInstanceDBsAccessLevel.Content    = GetAccessLevel((int)Sld_AL_SafetyInstanceDBs.Value);

                    if (CheckBox_Optimized_Data.IsChecked == true)
                    {
                        txt_Summary_Optimized.Content = "Yes";
                    }
                    else
                    {
                        txt_Summary_Optimized.Content = "No";
                    }
                    
                    if (CheckBox_String_Id.IsChecked == true)
                    {
                        txt_Summary_NodeIdIdentifier.Content = "String";
                    }
                    else
                    {
                        txt_Summary_NodeIdIdentifier.Content = "Numeric";
                    }

                    if (CheckBox_KeepEmpty_DBs.IsChecked == true)
                    {
                        txt_Summary_KeepEmptyDBs.Content = "Yes";
                    }
                    else
                    {
                        txt_Summary_KeepEmptyDBs.Content = "No";
                    }

                    if (CheckBox_KeepFolderStructure.IsChecked == true)
                    {
                        txt_Summary_KeepFolderStructure.Content = "Yes";
                    }
                    else
                    {
                        txt_Summary_KeepFolderStructure.Content = "No";
                    }
                }
            }
        }

        #region OTHER SETTINGS

        /// <summary>
        /// Checkboxes to remove or keep empty data blocks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_NoKeepEmpty_DBs_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox_NoKeepEmpty_DBs.IsChecked = true;
            CheckBox_KeepEmpty_DBs.IsChecked = false;
            UserInputFields.KeepEmptyDBs = false;
        }

        private void CheckBox_KeepEmpty_DBs_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox_NoKeepEmpty_DBs.IsChecked = false;
            CheckBox_KeepEmpty_DBs.IsChecked = true;
            UserInputFields.KeepEmptyDBs = true;
        }

        /// <summary>
        /// Checkboxes to keep or remove the folder structure of the project.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_NoFolderStrucutre_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox_NoFolderStructure.IsChecked = true;
            CheckBox_KeepFolderStructure.IsChecked = false;
            UserInputFields.KeepFolderStructure = false;
        }

        private void CheckBox_KeepFolderStrucutre_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox_NoFolderStructure.IsChecked = false;
            CheckBox_KeepFolderStructure.IsChecked = true;
            UserInputFields.KeepFolderStructure = true;
        }
        #endregion
    }
}
