using Siemens.Engineering;
using Siemens.Engineering.AddIn;
using System.Collections.Generic;
using Siemens.Engineering.AddIn.Menu;

namespace AddInOpcUaInterface
{
    /// <summary>
    /// This class Provides your Add-Ins to the TIA Portal
    /// By inheriting from one of the different Add-In Providers you define the area in the TIA Portal where your Add-In will be shown
    /// </summary>
    public sealed class AddInProvider : ProjectTreeAddInProvider
    {
        /// <summary>
        /// Reference to tiaPortal in which the add-in is executed
        /// </summary>
        private readonly TiaPortal _tiaPortal;
        
        /// <summary>
        /// The constructor of the ProjectTreeProvider
        /// </summary>
        /// <param name="tiaPortal">Represents the actual used TIA Portal instance</param>
        public AddInProvider(TiaPortal tiaPortal)
        {
            _tiaPortal = tiaPortal;
        }

        /// <summary>
        /// Provides a list of all add-ins to show in the context menu. 
        /// </summary>
        /// <returns>
        /// A list of all provided add-ins
        /// </returns>
        protected override IEnumerable<ContextMenuAddIn> GetContextMenuAddIns()
        {
            yield return new AddIn(_tiaPortal);
        }
    }
}
