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
    internal class ExportInfo
    {
        public static Result ProcessElements(ExternalCommandData commandData)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            SelectionWindow window = new SelectionWindow(doc);
            bool? result = window.ShowDialog();

            if (result != true || window.SelectedCategories.Count == 0 || window.SelectedViewIds.Count == 0)
            {
                TaskDialog.Show("Notice", "No category or view was selected.");
                return Result.Cancelled;
            }

            // Valid categories
            var allowedCats = new HashSet<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows
            };

            var selectedCats = window.SelectedCategories
                                      .Select(id => (BuiltInCategory)id)
                                      .Where(c => allowedCats.Contains(c))
                                      .ToHashSet();

            if (!selectedCats.Contains(BuiltInCategory.OST_Walls))
            {
                TaskDialog.Show("Notice", "You must select walls to proceed.");
                return Result.Cancelled;
            }

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputDir = Path.Combine(assemblyFolder, "build_files", "build_tools");
            Directory.CreateDirectory(outputDir);

            foreach (var viewId in window.SelectedViewIds)
            {
                View view = doc.GetElement(viewId) as View;
                if (view == null) continue;

                string viewName = view.Name.Replace(":", "_").Replace(" ", "_");
                string filePath = Path.Combine(outputDir, $"elements_{viewName}.json");

                var walls = new FilteredElementCollector(doc, view.Id)
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsNotElementType()
                    .ToElements();

                var doorsAndWindows = new List<FamilyInstance>();
                if (selectedCats.Contains(BuiltInCategory.OST_Doors))
                {
                    doorsAndWindows.AddRange(new FilteredElementCollector(doc, view.Id)
                        .OfCategory(BuiltInCategory.OST_Doors)
                        .WhereElementIsNotElementType()
                        .OfType<FamilyInstance>());
                }

                if (selectedCats.Contains(BuiltInCategory.OST_Windows))
                {
                    doorsAndWindows.AddRange(new FilteredElementCollector(doc, view.Id)
                        .OfCategory(BuiltInCategory.OST_Windows)
                        .WhereElementIsNotElementType()
                        .OfType<FamilyInstance>());
                }

                var output = new List<object>();
                foreach (var wall in walls)
                {
                    var start = wall is Wall w && w.Location is LocationCurve lc
                        ? new { x = lc.Curve.GetEndPoint(0).X, y = lc.Curve.GetEndPoint(0).Y, z = lc.Curve.GetEndPoint(0).Z }
                        : null;

                    var end = wall is Wall w2 && w2.Location is LocationCurve lc2
                        ? new { x = lc2.Curve.GetEndPoint(1).X, y = lc2.Curve.GetEndPoint(1).Y, z = lc2.Curve.GetEndPoint(1).Z }
                        : null;

                    var wallObj = new
                    {
                        type = wall.Category?.Name ?? "Wall",
                        id = wall.Id.IntegerValue,
                        name = wall.Name,
                        start = start,
                        end = end,
                        openings = doorsAndWindows
                            .Where(inst => inst.Host?.Id == wall.Id)
                            .Select(inst => SubClasses.GetOpenInfo.ExtractOpeningInfo(
                                inst,
                                start?.x ?? 0, start?.y ?? 0, start?.z ?? 0,
                                end?.x ?? 0, end?.y ?? 0, end?.z ?? 0
                            ))
                            .ToList()
                    };

                    output.Add(wallObj);
                }

                File.WriteAllText(filePath, JsonConvert.SerializeObject(new { walls = output }, Formatting.Indented));

                SubClasses.WallOpen.ProcessWallOpen($"elements_{viewName}", $"empty_walls_{viewName}");

                SubClasses.ImageCreator.PrepareImageAndFiles(commandData, outputDir, window.SelectedViewIds);
            }

            return Result.Succeeded;
        }
    }
}



