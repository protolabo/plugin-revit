using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Create.ExportClasses
{
    internal class AddWalls
    {
        public static Result CreateWalls(Document doc)
        {
            string myCopyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "myCopy");

            string buildToolsPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "build_files", "build_tools");
            if (!Directory.Exists(buildToolsPath))
            {
                TaskDialog.Show("Error", "The build_tools folder was not found.");
                return Result.Failed;
            }

            var imageFiles = Directory.GetFiles(buildToolsPath, "exported_view - *.bmp");
            if (!imageFiles.Any())
            {
                TaskDialog.Show("Error", "No images were found.");
                return Result.Failed;
            }

            var floorPlans = JToken.Parse(File.ReadAllText(Path.Combine(myCopyFolder, "floorPlans.json")));
            var wallTypesJson = File.ReadAllText(Path.Combine(myCopyFolder, "wallTypes.json"));
            var requirementsJson = File.ReadAllText(Path.Combine(myCopyFolder, "requirements.json"));

            // Default values
            string wallConcreteId = Regex.Match(wallTypesJson, @"""name""\s*:\s*""Wall, Concrete"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
            string windowInteriorId = Regex.Match(wallTypesJson, @"""name""\s*:\s*""Window, Interior"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
            string doorInteriorId = Regex.Match(wallTypesJson, @"""name""\s*:\s*""Door, Interior Office"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
            string requirementId = Regex.Match(requirementsJson, @"""name""\s*:\s*""Ekahau Best Practices"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;

            var viewInfo = JArray.Parse(File.ReadAllText(Path.Combine(buildToolsPath, "imageData.json")));

            var wallPointsList = new List<string>();
            var wallSegmentsList = new List<string>();
            var areasList = new List<string>();

            foreach (var imageFile in imageFiles)
            {
                string imageFileName = Path.GetFileName(imageFile);
                string viewName = imageFileName.Replace("exported_view - Floor Plan - ", "").Replace(".bmp", "").Replace(" ", "_");

                var viewEntry = viewInfo.FirstOrDefault(v => (string)v["viewName"] == viewName.Replace("_", " "));
                if (viewEntry == null) continue;

                // Retrieves the positions of the four corners of the Crop Region for each view,
                // using the data stored in the imageData.json file.
                double minX = (double)viewEntry["min"]["x"];
                double maxX = (double)viewEntry["max"]["x"];
                double minY = (double)viewEntry["min"]["y"];
                double maxY = (double)viewEntry["max"]["y"];
                int imageWidth = (int)viewEntry["width"];
                int imageHeight = (int)viewEntry["height"];

                // Converts from Revit's internal units (feet) to Ekahau's units (pixels).
                // This is required for proper spatial scaling in Ekahau maps.
                // Reference: https://protolabo.github.io/plugin-revit
                Func<double, double> convertX = (x) => (x - minX) / (maxX - minX) * imageWidth;
                Func<double, double> convertY = (y) => (maxY - y) / (maxY - minY) * imageHeight;

                string floorPlanId = floorPlans["floorPlans"]
                    .FirstOrDefault(fp => (string)fp["name"] == imageFileName)?["id"]?.ToString();
                if (string.IsNullOrEmpty(floorPlanId)) continue;

                string elementsPath = Path.Combine(buildToolsPath, $"elements_{viewName}.json");
                if (File.Exists(elementsPath))
                {
                    var elementsJson = JToken.Parse(File.ReadAllText(elementsPath));
                    //SubFunctions.WallNoOpen.ProcessWallNoOpen(elementsJson, floorPlanId, convertX, convertY, wallConcreteId, wallPointsList, wallSegmentsList);
                    // The WallElements.ProcessWallElements function adds the corresponding wallPoints and wallSegments 
                    // for each door and window to the appropriate lists, 
                    // using the required format for inclusion in the Ekahau JSON file.
                    SubClasses.WallElements.ProcessWallElements(elementsJson, floorPlanId, convertX, convertY, windowInteriorId, doorInteriorId, wallPointsList, wallSegmentsList);
                    // The function Areas.ProcessAreas selects the list of segments 
                    // that enclose each room in the Revit model and creates an 'area' object 
                    // using the required format for inclusion in the corresponding Ekahau project JSON file.
                    SubClasses.Areas.ProcessAreas(doc, viewName, floorPlanId, requirementId, convertX, convertY, areasList);

                }

                string[] splitFiles = Directory.GetFiles(buildToolsPath, $"empty_walls_{viewName}*.json");
                foreach (var splitFile in splitFiles)
                {
                    var splitJson = JToken.Parse(File.ReadAllText(splitFile));
                    // The WallNoOpen.ProcessWallNoOpen function performs the same operation as WallElements.ProcessWallElements,
                    // but with walls that have been previously split.
                    SubClasses.WallNoOpen.ProcessWallNoOpen(splitJson, floorPlanId, convertX, convertY, wallConcreteId, wallPointsList, wallSegmentsList);
                }
            }

            //if (!wallPointsList.Any() || !wallSegmentsList.Any())
            //{
            //    TaskDialog.Show("Aviso", "No se generaron elementos.");
            //    return Result.Succeeded;
            //}

            File.WriteAllText(Path.Combine(myCopyFolder, "wallPoints.json"), "{\n  \"wallPoints\": [\n" + string.Join(",\n", wallPointsList) + "\n  ]\n}");
            File.WriteAllText(Path.Combine(myCopyFolder, "wallSegments.json"), "{\n  \"wallSegments\": [\n" + string.Join(",\n", wallSegmentsList) + "\n  ]\n}");
            File.WriteAllText(Path.Combine(myCopyFolder, "areas.json"), "{\n  \"areas\": [\n" + string.Join(",\n", areasList) + "\n  ]\n}");

            return Result.Succeeded;
        }
    }
}

