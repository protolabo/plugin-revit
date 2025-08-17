using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Autodesk.Revit.DB.Structure;

using Create.ImportClasses;
using Create.ExportClasses;

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

            string accessPointID;
            string bluetoothBeaconID;

            try
            {
               
                // 1. Load JSON files from the .esx archive
                if (!JsonFilesExtractor.GetJsonFromESX(out List<JObject> floorPlans, 
                    out List<JObject> accessPoints,
                    out List<JObject> antennaTypes,
                    out List<JObject> simulatedRadios))
                {
                    TaskDialog.Show("Import", "Failed to load JSON files from the .esx archive.");
                    return Result.Failed;
                }

                AntennaGetters.GetAntennaIds(antennaTypes, out accessPointID, out bluetoothBeaconID);

                // 2. Load and activate the Access Point family
                FamilySymbol accessPointSymbol = GetFamilySymbol(doc, "Access_Point", "Access_Point_Type");
                if (accessPointSymbol == null)
                {
                    accessPointSymbol = AccessPointFamily.LoadAccessPointFamily(doc);
                    if (accessPointSymbol == null)
                    {
                        TaskDialog.Show("Error", "Failed to load or activate the 'Access_Point' family symbol.");
                        return Result.Failed;
                    }
                }

                FamilySymbol bluetoothBeaconSymbol = GetFamilySymbol(doc, "Bluetooth_Beacon", "Bluetooth_Beacon_Type");
                if (bluetoothBeaconSymbol == null)
                {
                    bluetoothBeaconSymbol = AccessPointFamily.LoadBluetoothBeaconFamily(doc);
                    if (bluetoothBeaconSymbol == null)
                    {
                        TaskDialog.Show("Error", "Failed to load or activate the 'Bluetooth_Beacon' family symbol.");
                        return Result.Failed;
                    }
                }

                // Activate the symbols if they are not active
                if (!accessPointSymbol.IsActive) accessPointSymbol.Activate();
                if (!bluetoothBeaconSymbol.IsActive) bluetoothBeaconSymbol.Activate();

                // --- Assign first two existing materials with default colors ---
                List<Material> materials = new FilteredElementCollector(doc)
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .Take(2)
                    .ToList();

                if (materials.Count < 2)
                {
                    throw new InvalidOperationException("There are fewer than 2 materials in the document.");
                }

                // Set default colors
                using (Transaction tMat = new Transaction(doc, "Set default colors for materials"))
                {
                    tMat.Start();

                    materials[0].Color = new Color(0, 255, 0); // green
                    materials[1].Color = new Color(0, 0, 255); // blue

                    // Assign materials to family symbols
                    if (accessPointSymbol.LookupParameter("Material_Pared") != null)
                        accessPointSymbol.LookupParameter("Material_Pared").Set(materials[0].Id);

                    if (bluetoothBeaconSymbol.LookupParameter("Material_Pared") != null)
                        bluetoothBeaconSymbol.LookupParameter("Material_Pared").Set(materials[1].Id);

                    tMat.Commit();
                }

                // 3. Delete current instances in model
                using (Transaction t = new Transaction(doc, "Delete existing family instances"))
                {
                    t.Start();

                    // Access Point
                    var accessPointIds = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilyInstance))
                        .Cast<FamilyInstance>()
                        .Where(fi => fi.Symbol.Id == accessPointSymbol.Id)
                        .Select(fi => fi.Id)
                        .ToList(); // <-- copiar a lista

                    doc.Delete(accessPointIds);

                    // Bluetooth Beacon
                    var bluetoothBeaconIds = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilyInstance))
                        .Cast<FamilyInstance>()
                        .Where(fi => fi.Symbol.Id == bluetoothBeaconSymbol.Id)
                        .Select(fi => fi.Id)
                        .ToList(); // <-- copiar a lista

                    doc.Delete(bluetoothBeaconIds);

                    t.Commit();
                }

                // 4. Place access points in the corresponding views
                AccesPointsPlacer.PlaceAccessPoints(doc, uiDoc, 
                    floorPlans, accessPoints, simulatedRadios, 
                    accessPointSymbol, bluetoothBeaconSymbol,
                    accessPointID, bluetoothBeaconID);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        // Function to get an existing FamilySymbol by name
        private FamilySymbol GetFamilySymbol(Document doc, string familyName, string typeName)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name == familyName && fs.Name == typeName);
        }

    }
}









