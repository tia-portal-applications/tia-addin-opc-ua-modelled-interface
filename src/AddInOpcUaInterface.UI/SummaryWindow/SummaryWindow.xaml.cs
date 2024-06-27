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

namespace AddInOPCUAInterface.UI
{
    public partial class SummaryWindow : Window
    {
        public SummaryWindow()
        {
            InitializeComponent();
            this.Topmost = true;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            DataContext = this;
        }

        /// <summary>
        /// Displays relevant information about the program's execution in the summary window.
        /// </summary>
        /// <param name="interfaceName"></param>
        /// <param name="elapsedSeconds"></param>
        /// <param name="totalNodes"></param>
        public void SetSummary(string interfaceName, double elapsedSeconds, int totalNodes) 
        {
            label_Line1.Content = $@"New user interface ""{interfaceName}"" has been added to the project.";
            label_Line2.Content = $@"The new server interface includes a total of {totalNodes} nodes.";
            label_Line3.Content = $@"Execution time: {elapsedSeconds:F2} seconds.";
        }

        /// <summary>
        /// Displays the number of nodes that have been created in the summary window.
        /// </summary>
        /// <param name="numberDefaultNodes"></param>
        /// <param name="numberUserSystemDataTypes"></param>
        /// <param name="numberTags"></param>
        /// <param name="numberGlobalDBs"></param>
        /// <param name="numberInstanceDB"></param>
        public void SetNumberNodes(string numberDefaultNodes, string numberUserSystemDataTypes, string  numberTags, string numberGlobalDBs, string numberInstanceDB)
        {
            label_numberDefaultNodes.Content    = numberDefaultNodes;
            label_numberDataTypes.Content       = numberUserSystemDataTypes;
            label_numberTags.Content            = numberTags;
            label_numberGlobalDBs.Content       = numberGlobalDBs;
            label_numberInstanceDBs.Content     = numberInstanceDB;
        }

        /// <summary>
        /// Closes the WPF window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Continue_Click(object sender, RoutedEventArgs e)
        {
            Close();  
        }

        /// <summary>
        /// Closes the WPF window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        } 
    }
}
