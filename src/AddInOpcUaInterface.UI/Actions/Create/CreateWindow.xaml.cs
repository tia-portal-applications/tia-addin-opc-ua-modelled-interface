using System;
using System.Collections;
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
    public partial class CreateWindow : Window
    {
        public CreateWindow()
        {
            InitializeComponent();
            this.Topmost = true;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            DataContext = this;
            
            txt_InterfaceName.BorderBrush = Brushes.Red;
            txt_Path.BorderBrush = Brushes.Red;
            btn_Create.IsEnabled = false;
        }

        /// <summary>
        /// Stores the user input in the UserInputFields variables and closes the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Create_Click(object sender, RoutedEventArgs e)
        {
            UserInputFields.InterfaceName = txt_InterfaceName.Text;
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
    }
}
