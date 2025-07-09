using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Create
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class EditOpenings : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                EditOpeningsWindow window = new EditOpeningsWindow();
                window.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to open window:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
