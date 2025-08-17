using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Create.ExportClasses
{
    internal class WallsInserter
    {
        public static Result InsertWallAndOpeningsInEkahauFile(Dictionary<string, ModelData> modelDataSegments, List<ViewData> viewInfo)
        {
            // Loads neccesary files
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
            string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
            string tempPath = Path.Combine(tempFolderPath, "Template");

            if (!Directory.Exists(tempFolderPath))
            {
                TaskDialog.Show("Error", "The build_tools folder was not found.");
                return Result.Failed;
            }

            var imageFiles = Directory.GetFiles(tempFolderPath, "exported_view - *.bmp");
            if (!imageFiles.Any())
            {
                TaskDialog.Show("Error", "No images were found.");
                return Result.Failed;
            }

            var floorPlans = JToken.Parse(File.ReadAllText(Path.Combine(tempPath, "floorPlans.json")));
            var wallTypesJson = File.ReadAllText(Path.Combine(tempPath, "wallTypes.json"));
            var requirementsJson = File.ReadAllText(Path.Combine(tempPath, "requirements.json"));
            // var viewInfo = JArray.Parse(File.ReadAllText(Path.Combine(tempFolderPath, "imageData.json")));

            var wallPointsList = new List<string>();
            var wallSegmentsList = new List<string>();

            var wallPointObjectsByFloor = new Dictionary<string, List<WallPoint>>();

            foreach (var imageFile in imageFiles)
            {
                string imageFileName = Path.GetFileName(imageFile);
                string viewName = imageFileName.Replace("exported_view - Floor Plan - ", "").Replace(".bmp", "").Replace(" ", "_");

                var viewEntry = viewInfo.FirstOrDefault(v => v.viewName == viewName.Replace("_", " "));
                if (viewEntry == null) continue;

                // Retrieves the positions of the four corners of the Crop Region for each view,
                // using the data stored in the imageData.json file.
                double minX = viewEntry.min.x;
                double maxX = viewEntry.max.x;
                double minY = viewEntry.min.y;
                double maxY = viewEntry.max.y;
                int imageWidth = viewEntry.width;
                int imageHeight = viewEntry.height;

                // Converts from Revit's internal units (feet) to Ekahau's units (pixels).
                // This is required for proper spatial scaling in Ekahau maps.
                // Reference: https://protolabo.github.io/plugin-revit
                Func<double, double> convertX = (x) => (x - minX) / (maxX - minX) * imageWidth;
                Func<double, double> convertY = (y) => (maxY - y) / (maxY - minY) * imageHeight;

                string floorPlanId = floorPlans["floorPlans"]
                    .FirstOrDefault(fp => (string)fp["name"] == imageFileName)?["id"]?.ToString();
                if (string.IsNullOrEmpty(floorPlanId)) continue;

                if (!wallPointObjectsByFloor.ContainsKey(floorPlanId))
                {
                    wallPointObjectsByFloor[floorPlanId] = new List<WallPoint>();
                }

                var wallPointObjects = wallPointObjectsByFloor[floorPlanId];

                // The SegmentsListCreator.FillOpeningsList function adds the corresponding wallPoints and wallSegments 
                // for each door, window and wall segment to the appropriate lists, using the required format for inclusion in the Ekahau JSON file.
                SegmentsListCreator.FillSegmentsList(modelDataSegments[viewName].walls, floorPlanId, convertX, convertY, tempPath, wallPointsList, wallSegmentsList, wallPointObjects);

            }

            File.WriteAllText(Path.Combine(tempPath, "wallPoints.json"), "{\n  \"wallPoints\": [\n" + string.Join(",\n", wallPointsList) + "\n  ]\n}");
            File.WriteAllText(Path.Combine(tempPath, "wallSegments.json"), "{\n  \"wallSegments\": [\n" + string.Join(",\n", wallSegmentsList) + "\n  ]\n}");
            //File.WriteAllText(Path.Combine(tempPath, "exclusionAreas.json"), "{\n  \"exclusionAreas\": [\n" + string.Join(",\n", areasList) + "\n  ]\n}");

            // FloorAligner.AlignFloorsAndGenerateJson(wallPointObjectsByFloor, viewInfo, tempPath);

            return Result.Succeeded;
        }
    }
}

