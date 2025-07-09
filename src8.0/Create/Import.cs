using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Autodesk.Revit.DB.Structure;

using Create.ImportClasses;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace Create
{
    [Transaction(TransactionMode.Manual)]
    public class Import : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
               
                // 1. Load JSON files from the .esx archive
                if (!EsxJsons.LoadEsxJsons(out List<JObject> floorPlans, out List<JObject> accessPoints))
                {
                    TaskDialog.Show("Import", "Failed to load JSON files from the .esx archive.");
                    return Result.Failed;
                }

                // 2. Load and activate the Access Point family
                FamilySymbol accessPointSymbol = AccesPointFamily.LoadAccessPointFamily(doc);
                if (accessPointSymbol == null)
                {
                    TaskDialog.Show("Error", "Failed to load or activate the 'Access_Point' family symbol.");
                    return Result.Failed;
                }

                // 3. Place access points in the corresponding views
                int totalPlaced = AccesPoints.PlaceAccessPoints(doc, floorPlans, accessPoints, accessPointSymbol);

                TaskDialog.Show("Import", $"{totalPlaced} access points were placed successfully.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        
    }
}









