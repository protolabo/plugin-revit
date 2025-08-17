using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using Autodesk.Revit.UI;

namespace Create.ImportClasses
{
    internal class AccesPointsPlacer
    {
        public static void PlaceAccessPoints(
            Document doc,
            UIDocument uidoc,
            List<JObject> floorPlans,
            List<JObject> accessPoints,
            List<JObject> simulatedRadios,
            FamilySymbol accessPointSymbol,
            FamilySymbol bluetoothBeaconSymbol,
            string accessPointID,
            string bluetoothBeaconID)
        {
            // ===============================
            // Activate the first Floor Plan view in the model
            // ===============================

            // Get the first ViewPlan whose ViewType is FloorPlan and is not a template
            ViewPlan firstFloorPlanView = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .FirstOrDefault(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan);

            if (firstFloorPlanView != null)
            {
                try
                {
                    // Inform the user which view is being activated
                    //TaskDialog.Show("Info", $"Activating first Floor Plan view: {firstFloorPlanView.Name}");

                    // Activate the view through UIDocument (cannot be done through Document)
                    uidoc.ActiveView = firstFloorPlanView;
                }
                catch (Exception ex)
                {
                    // Show a message if the view cannot be activated
                    TaskDialog.Show("Warning", $"Could not activate Floor Plan view '{firstFloorPlanView.Name}': {ex.Message}");
                }
            }
            else
            {
                TaskDialog.Show("Warning", "No Floor Plan views found in the current document.");
            }
            // ===============================

            int placedCount = 0;
            Dictionary<string, (int APs, int BBs)> placedByView = new Dictionary<string, (int, int)>();

            // 1️ Remove duplicates from simulatedRadios by accessPointId
            var uniqueRadios = simulatedRadios
                .Where(r => r["accessPointId"] != null)
                .GroupBy(r => r["accessPointId"].ToString())
                .Select(g => g.First())
                .ToList();

            // 2️ Map unique radios to their corresponding accessPoints
            var filteredAccessPoints = uniqueRadios
                .Select(radio =>
                {
                    string apId = radio["accessPointId"].ToString();
                    return accessPoints.FirstOrDefault(ap => ap["id"] != null && ap["id"].ToString() == apId);
                })
                .Where(ap => ap != null && ap["location"]?["floorPlanId"] != null)
                .ToList();

            // 3️ Group accessPoints by the corresponding view name
            var groupedByView = filteredAccessPoints
                .GroupBy(ap =>
                {
                    string floorPlanId = ap["location"]["floorPlanId"].ToString();
                    string viewName = floorPlans
                        .FirstOrDefault(f => f["id"]?.ToString() == floorPlanId)?["name"]?.ToString();

                    return string.IsNullOrEmpty(viewName) ? null : ViewNameGetter.ExtractViewName(viewName);
                })
                .Where(g => g.Key != null);

            // 4️ Process each group of accessPoints per view
            foreach (var group in groupedByView)
            {
                string extractedViewName = group.Key;

                ViewPlan view = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewPlan))
                    .Cast<ViewPlan>()
                    .FirstOrDefault(v => v.Name == extractedViewName);

                if (view == null) continue;

                Level level = view.GenLevel != null ? doc.GetElement(view.GenLevel.Id) as Level : null;
                if (level == null) continue;

                BoundingBoxXYZ cropBox = view.CropBox;
                if (cropBox == null) continue;

                double minX = cropBox.Min.X;
                double maxX = cropBox.Max.X;
                double minY = cropBox.Min.Y;
                double maxY = cropBox.Max.Y;

                var floorPlan = floorPlans.FirstOrDefault(f => f["name"]?.ToString().Contains(extractedViewName) == true);
                if (floorPlan == null) continue;

                double imageWidth = floorPlan["width"].Value<double>();
                double imageHeight = floorPlan["height"].Value<double>();

                int placedAPs = 0;
                int placedBBs = 0;

                using (Transaction tx = new Transaction(doc, $"Place Radios in {extractedViewName}"))
                {
                    tx.Start();

                    foreach (var ap in group)
                    {
                        double x_ekahau = ap["location"]["coord"]["x"].Value<double>();
                        double y_ekahau = ap["location"]["coord"]["y"].Value<double>();

                        // Convert Ekahau coordinates (pixels) to Revit coordinates (ft)
                        double x_revit = (x_ekahau / imageWidth) * (maxX - minX) + minX;
                        double y_revit = maxY - (y_ekahau / imageHeight) * (maxY - minY);

                        XYZ location = new XYZ(x_revit, y_revit, 0);

                        // Select symbol based on antennaTypeId
                        var radio = uniqueRadios.FirstOrDefault(r => r["accessPointId"].ToString() == ap["id"].ToString());
                        if (radio == null) continue;

                        string antennaTypeId = radio["antennaTypeId"]?.ToString();
                        FamilySymbol symbolToPlace = null;

                        if (antennaTypeId == accessPointID)
                        {
                            symbolToPlace = accessPointSymbol;
                            placedAPs++;
                        }
                        else if (antennaTypeId == bluetoothBeaconID)
                        {
                            symbolToPlace = bluetoothBeaconSymbol;
                            placedBBs++;
                        }
                        else
                        {
                            continue; // skip unknown antenna type
                        }

                        doc.Create.NewFamilyInstance(location, symbolToPlace, level, StructuralType.NonStructural);
                    }

                    tx.Commit();
                }

                if (placedAPs + placedBBs > 0)
                    placedByView[extractedViewName] = (placedAPs, placedBBs);
            }

            // ===============================
            // Show summary message
            // ===============================
            if (placedByView.Count > 0)
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendLine("Radios placed in views:");
                foreach (var kv in placedByView)
                {
                    msg.AppendLine($"- {kv.Key}: {kv.Value.APs} APs, {kv.Value.BBs} BBs");
                }

                int totalAPs = placedByView.Values.Sum(v => v.APs);
                int totalBBs = placedByView.Values.Sum(v => v.BBs);

                msg.AppendLine($"\nTotal: {totalAPs} APs, {totalBBs} BBs");

                TaskDialog.Show("Placement Summary", msg.ToString());
            }
            else
            {
                TaskDialog.Show("Placement Summary", "No radios were placed.");
            }


        }

    }
}
