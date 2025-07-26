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
        public static Result ExportModelData(ExternalCommandData commandData, 
            Dictionary<string, ModelData> modelData, Dictionary<string, ModelData> modelDataSegments,
            List<ViewData> viewInfo)
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

                List<WallData> wallList = new List<WallData>();

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
                    Point start = null, end = null;
                    if (wall is Wall w && w.Location is LocationCurve lc)
                    {
                        XYZ p0 = lc.Curve.GetEndPoint(0);
                        start = new Point { x = p0.X, y = p0.Y, z = p0.Z };
                    }
                    if (wall is Wall w2 && w2.Location is LocationCurve lc2)
                    {
                        XYZ p1 = lc2.Curve.GetEndPoint(1);
                        end = new Point { x = p1.X, y = p1.Y, z = p1.Z };
                    }

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
                            Element el = doc.GetElement(id);
                            if (el != null && !(el is FamilyInstance))
                            {
                                openingElements.Add(el);
                            }
                        }
                    }

                    // Final list of openings
                    List<OpeningData> openings = new List<OpeningData>();

                    // WindowDoorDimensions.GetWindowDoorDimensions returns an object containing
                    // position and dimensions for windows and doors
                    foreach (var inst in hostedInWall)
                    {
                        openings.Add(WindowDoorDimensions.GetWindowDoorDimensions(
                            inst,
                            start != null ? start.x : 0,
                            start != null ? start.y : 0,
                            start != null ? start.z : 0,
                            end != null ? end.x : 0,
                            end != null ? end.y : 0,
                            end != null ? end.z : 0
                        ));
                    }

                    // Get the position and dimensions for General Openings and creates and object 
                    // with these informatoins
                    foreach (var el in openingElements)
                    {
                        BoundingBoxXYZ bbox = el.get_BoundingBox(null);
                        if (bbox == null) continue;

                        double midX = (bbox.Min.X + bbox.Max.X) / 2;
                        double midY = (bbox.Min.Y + bbox.Max.Y) / 2;
                        double midZ = (bbox.Min.Z + bbox.Max.Z) / 2;

                        bool esHorizontal = start != null && end != null &&
                                            Math.Abs(end.x - start.x) > Math.Abs(end.y - start.y);

                        double alignedX = (start != null) ? start.x : midX;
                        double alignedY = (start != null) ? start.y : midY;

                        openings.Add(new OpeningData
                        {
                            type = "Opening",
                            name = "Standard",
                            id = el.Id.IntegerValue,
                            start_point = new Point
                            {
                                x = esHorizontal ? bbox.Min.X : alignedX,
                                y = esHorizontal ? alignedY : bbox.Min.Y,
                                z = bbox.Min.Z
                            },
                            end_point = new Point
                            {
                                x = esHorizontal ? bbox.Max.X : alignedX,
                                y = esHorizontal ? alignedY : bbox.Max.Y,
                                z = bbox.Max.Z
                            },
                            position = new Point
                            {
                                x = esHorizontal ? midX : alignedX,
                                y = esHorizontal ? alignedY : midY,
                                z = midZ
                            }
                        });
                    }

                    WallData wallData = new WallData
                    {
                        type = wall.Category != null ? wall.Category.Name : "Wall",
                        id = wall.Id.IntegerValue,
                        name = wall.Name,
                        start = start,
                        end = end,
                        openings = openings
                    };

                    wallList.Add(wallData);
                }

                modelData[viewName] = new ModelData();
                modelData[viewName].walls = wallList;
 
            }

            // The 'WallSplitter.SplitWallByOpening' function splits walls in segments according to the openings
            // in the wall and add them to the list of openings in the wall from the original file.
            // For example, if a wall contains a door, this function creates one wall segment from the original start point
            // to one end of the door, and another segment from the other end of the door to the original wall's endpoint.
            // In the end, it generates a copy of the original JSON file adding the new segments.
            WallSplitter.SplitWallByOpening(modelData, modelDataSegments);

            // The 'ImageCreator.PrepareImageAndFiles' function exports BMP images for each view
            // and creates the corresponding JSON file containing metadata about those images.
            ImageExporter.CreateViewImagesAndReport(commandData, tempFolderPath, window.SelectedViewIds, viewInfo);


            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //// Save viewInfo as JSON in desktop
            //try
            //{
            //    string viewInfoJson = JsonConvert.SerializeObject(viewInfo, Formatting.Indented);
            //    string outputPath = Path.Combine(desktopPath, "view_info.json");

            //    File.WriteAllText(outputPath, viewInfoJson);
            //    Console.WriteLine($"✅ Archivo view_info.json guardado en: {outputPath}");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"❌ Error al guardar view_info.json: {ex.Message}");
            //}


            //// save file
            //string outputFilePath = Path.Combine(desktopPath, "model_data.json");

            //var options = new System.Text.Json.JsonSerializerOptions
            //{
            //    WriteIndented = true,
            //    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            //};

            //try
            //{
            //    string json = Newtonsoft.Json.JsonConvert.SerializeObject(modelData, Newtonsoft.Json.Formatting.Indented);
            //    File.WriteAllText(outputFilePath, json);
            //    //TaskDialog.Show("Export", $"Saved file:\n{outputFilePath}");
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("Error", $"No JSON:\n{ex.Message}");
            //}//string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //// Save viewInfo as JSON in desktop
            //try
            //{
            //    string viewInfoJson = JsonConvert.SerializeObject(viewInfo, Formatting.Indented);
            //    string outputPath = Path.Combine(desktopPath, "view_info.json");

            //    File.WriteAllText(outputPath, viewInfoJson);
            //    Console.WriteLine($"✅ Archivo view_info.json guardado en: {outputPath}");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"❌ Error al guardar view_info.json: {ex.Message}");
            //}


            //// save file
            //string outputFilePath = Path.Combine(desktopPath, "model_data.json");

            //var options = new System.Text.Json.JsonSerializerOptions
            //{
            //    WriteIndented = true,
            //    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            //};

            //try
            //{
            //    string json = Newtonsoft.Json.JsonConvert.SerializeObject(modelData, Newtonsoft.Json.Formatting.Indented);
            //    File.WriteAllText(outputFilePath, json);
            //    //TaskDialog.Show("Export", $"Saved file:\n{outputFilePath}");
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("Error", $"No JSON:\n{ex.Message}");
            //}

            return Result.Succeeded;
        }

    }
}




