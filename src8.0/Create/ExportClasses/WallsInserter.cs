﻿using Autodesk.Revit.DB;
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
        public static Result InsertWallAndOpeningsInEkahauFile(Document doc)
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
            string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
            string tempPath = Path.Combine(tempFolderPath, "Template");

            //string buildToolsPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "build_files", "tempFolder");
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

            // Get proper ID's
            string windowInteriorId = Getters.GetWindowId(wallTypesJson);
            string doorInteriorId = Getters.GetDoorId(wallTypesJson);
            //string requirementId = Getters.GetAreaId(requirementsJson);

            var viewInfo = JArray.Parse(File.ReadAllText(Path.Combine(tempFolderPath, "imageData.json")));

            // Build dictionary viewName -> stairs bool
            var stairsByViewName = viewInfo
                .ToDictionary(
                    v => ((string)v["viewName"]), // key normalized to underscore
                    v => v["stairs"] != null && (bool)v["stairs"]
                );

            var wallPointsList = new List<string>();
            var wallSegmentsList = new List<string>();
            //var areasList = new List<string>();

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

                string elementsPath = Path.Combine(tempFolderPath, $"elements_{viewName}.json");
                if (File.Exists(elementsPath))
                {
                    var elementsJson = JToken.Parse(File.ReadAllText(elementsPath));
                    //SubFunctions.WallNoOpen.ProcessWallNoOpen(elementsJson, floorPlanId, convertX, convertY, wallConcreteId, wallPointsList, wallSegmentsList);
                    // The WallElements.ProcessWallElements function adds the corresponding wallPoints and wallSegments 
                    // for each door and window to the appropriate lists, 
                    // using the required format for inclusion in the Ekahau JSON file.
                    OpeningsListCreator.FillOpeningsList(elementsJson, floorPlanId, convertX, convertY, windowInteriorId, doorInteriorId, wallPointsList, wallSegmentsList);
                    // The function Areas.ProcessAreas selects the list of segments 
                    // that enclose each room in the Revit model and creates an 'area' object 
                    // using the required format for inclusion in the corresponding Ekahau project JSON file.
                    // Only call ProcessStairs if stairs is true for this view
                    //if (stairsByViewName.TryGetValue(viewName.Replace("_", " "), out bool hasStairs) && hasStairs)
                    //{
                    //    StairsZoneListCreator.FillStairsZoneList(doc, viewName, floorPlanId, convertX, convertY, areasList);
                    //}

                }

                string[] splitFiles = Directory.GetFiles(tempFolderPath, $"empty_walls_{viewName}*.json");
                foreach (var splitFile in splitFiles)
                {
                    var splitJson = JToken.Parse(File.ReadAllText(splitFile));
                    // The WallNoOpen.ProcessWallNoOpen function performs the same operation as WallElements.ProcessWallElements,
                    // but with walls that have been previously split.
                    WallListCreator.FillWallList(splitJson, floorPlanId, convertX, convertY, tempPath, wallPointsList, wallSegmentsList);
                }
            }

            //if (!wallPointsList.Any() || !wallSegmentsList.Any())
            //{
            //    TaskDialog.Show("Aviso", "No se generaron elementos.");
            //    return Result.Succeeded;
            //}

            File.WriteAllText(Path.Combine(tempPath, "wallPoints.json"), "{\n  \"wallPoints\": [\n" + string.Join(",\n", wallPointsList) + "\n  ]\n}");
            File.WriteAllText(Path.Combine(tempPath, "wallSegments.json"), "{\n  \"wallSegments\": [\n" + string.Join(",\n", wallSegmentsList) + "\n  ]\n}");
            //File.WriteAllText(Path.Combine(tempPath, "exclusionAreas.json"), "{\n  \"exclusionAreas\": [\n" + string.Join(",\n", areasList) + "\n  ]\n}");

            return Result.Succeeded;
        }
    }
}

