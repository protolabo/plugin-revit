using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Create.ExportClasses
{
    internal class ModelDataExporter
    {
        public static Result ExportModelData(ExternalCommandData commandData)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            SelectionWindow window = new SelectionWindow(doc);
            bool? result = window.ShowDialog();

            // Only check that at least one view was selected
            if (result != true || window.SelectedViewIds.Count == 0)
            {
                TaskDialog.Show("Notice", "No view was selected.");
                return Result.Cancelled;
            }

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
            string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");

            // Delete temporary folder from previous exports if exists.
            if (Directory.Exists(tempFolderPath))
            {
                TemporaryFilesCollector.DeleteTemporaryFiles();
            }

            Directory.CreateDirectory(tempFolderPath);

            // Extract all the pertinent information from the REvit Model
            foreach (var viewId in window.SelectedViewIds)
            {
                View view = doc.GetElement(viewId) as View;
                if (view == null) continue;

                string viewName = view.Name.Replace(":", "_").Replace(" ", "_");
                string filePath = Path.Combine(tempFolderPath, $"elements_{viewName}.json");

                // Collect all walls in the view
                var walls = new FilteredElementCollector(doc, view.Id)
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsNotElementType()
                    .ToElements();

                // Collect doors and windows in the view
                var doorsAndWindows = new List<FamilyInstance>();

                doorsAndWindows.AddRange(new FilteredElementCollector(doc, view.Id)
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .WhereElementIsNotElementType()
                    .OfType<FamilyInstance>());

                doorsAndWindows.AddRange(new FilteredElementCollector(doc, view.Id)
                    .OfCategory(BuiltInCategory.OST_Windows)
                    .WhereElementIsNotElementType()
                    .OfType<FamilyInstance>());

                var output = new List<object>();

                // Iterate over each wall in the 'walls' collection.
                // For each wall, extract the start and end points of its location curve if available.
                //   - Check if the current element is a Wall and if its Location is a LocationCurve.
                //   - If so, get the 3D coordinates (X, Y, Z) of the start point (index 0) of the curve.
                //   - Otherwise, assign null to the start point.
                // Similarly, extract the end point coordinates (index 1) of the location curve.

                // Creates the 'Wall' object with its information and builds the 'openings' list,
                // which contains the 'door' and 'window' objects embedded in that wall.
                foreach (var wall in walls)
                {
                    var start = wall is Wall w && w.Location is LocationCurve lc
                        ? new { x = lc.Curve.GetEndPoint(0).X, y = lc.Curve.GetEndPoint(0).Y, z = lc.Curve.GetEndPoint(0).Z }
                        : null;

                    var end = wall is Wall w2 && w2.Location is LocationCurve lc2
                        ? new { x = lc2.Curve.GetEndPoint(1).X, y = lc2.Curve.GetEndPoint(1).Y, z = lc2.Curve.GetEndPoint(1).Z }
                        : null;

                    // Find doors/windows hosted in the wall 
                    var hostedInWall = doorsAndWindows
                        .Where(inst => inst.Host?.Id == wall.Id)
                        .ToList();

                    // Find generic openings (not FamilyInstance) using FindInserts.
                    var openingElements = new List<Element>();
                    if (wall is Wall actualWall)
                    {
                        var insertIds = actualWall.FindInserts(true, true, true, true);
                        foreach (var id in insertIds)
                        {
                            var el = doc.GetElement(id);
                            if (el != null && !(el is FamilyInstance))
                            {
                                openingElements.Add(el);
                            }
                        }
                    }

                    // Final list of openings
                    var openings = new List<object>();

                    // WindowDoorDimensions.GetWindowDoorDimensions returns an object containing
                    // position and dimensions for windows and doors
                    openings.AddRange(
                        hostedInWall.Select(inst =>
                            WindowDoorDimensions.GetWindowDoorDimensions(
                                inst,
                                start?.x ?? 0, start?.y ?? 0, start?.z ?? 0,
                                end?.x ?? 0, end?.y ?? 0, end?.z ?? 0
                            )
                        )
                    );

                    // Get the position and dimensions for General Openings and creates and object 
                    // with these informatoins
                    openings.AddRange(
                        openingElements.Select(el =>
                        {
                            var bbox = el.get_BoundingBox(null);
                            if (bbox == null) return null;

                            double midX = (bbox.Min.X + bbox.Max.X) / 2;
                            double midY = (bbox.Min.Y + bbox.Max.Y) / 2;
                            double midZ = (bbox.Min.Z + bbox.Max.Z) / 2;

                            bool esHorizontal = start != null && end != null &&
                                                Math.Abs(end.x - start.x) > Math.Abs(end.y - start.y);

                            double alignedY = start?.y ?? midY;
                            double alignedX = start?.x ?? midX;

                            return new
                            {
                                type = "Opening",
                                name = "Standard",
                                id = el.Id.IntegerValue,
                                start_point = new
                                {
                                    x = esHorizontal ? bbox.Min.X : alignedX,
                                    y = esHorizontal ? alignedY : bbox.Min.Y,
                                    z = bbox.Min.Z
                                },
                                end_point = new
                                {
                                    x = esHorizontal ? bbox.Max.X : alignedX,
                                    y = esHorizontal ? alignedY : bbox.Max.Y,
                                    z = bbox.Max.Z
                                },
                                position = new
                                {
                                    x = esHorizontal ? midX : alignedX,
                                    y = esHorizontal ? alignedY : midY,
                                    z = midZ
                                }
                            };
                        }).Where(o => o != null)
                    );

                    var wallObj = new
                    {
                        type = wall.Category?.Name ?? "Wall",
                        id = wall.Id.IntegerValue,
                        name = wall.Name,
                        start = start,
                        end = end,
                        openings = openings
                    };

                    output.Add(wallObj);
                }

                File.WriteAllText(filePath, JsonConvert.SerializeObject(new { walls = output }, Formatting.Indented));

                // The 'WallSplitter.SplitWallByOpening' function splits walls in segments according to the openings
                // in the wall and add them to the list of openings in the wall from the original file.
                // For example, if a wall contains a door, this function creates one wall segment from the original start point
                // to one end of the door, and another segment from the other end of the door to the original wall's endpoint.
                // In the end, it generates a copy of the original JSON file adding the new segments.
                WallSplitter.SplitWallByOpening($"elements_{viewName}", $"segmented_walls_{viewName}");

                // The 'ImageCreator.PrepareImageAndFiles' function exports BMP images for each view
                // and creates the corresponding JSON file containing metadata about those images.
                ImageExporter.CreateViewImagesAndReport(commandData, tempFolderPath, window.SelectedViewIds);

            }

            return Result.Succeeded;
        }
    }
}




