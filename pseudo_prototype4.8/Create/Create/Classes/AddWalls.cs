﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Create.Helpers
{
    internal class AddWalls
    {
        public static Result CreateWalls(Document doc)
        {
            string myCopyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "myCopy");

            string buildToolsPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "build_files", "build_tools");
            if (!Directory.Exists(buildToolsPath))
            {
                TaskDialog.Show("Error", "No se encontró la carpeta build_tools.");
                return Result.Failed;
            }

            var imageFiles = Directory.GetFiles(buildToolsPath, "exported_view - *.bmp");
            if (!imageFiles.Any())
            {
                TaskDialog.Show("Error", "No se encontraron imágenes.");
                return Result.Failed;
            }

            var floorPlans = JToken.Parse(File.ReadAllText(Path.Combine(myCopyFolder, "floorPlans.json")));
            var wallTypesJson = File.ReadAllText(Path.Combine(myCopyFolder, "wallTypes.json"));

            string wallConcreteId = Regex.Match(wallTypesJson, @"""name""\s*:\s*""Wall, Concrete"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
            string windowInteriorId = Regex.Match(wallTypesJson, @"""name""\s*:\s*""Window, Interior"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
            string doorInteriorId = Regex.Match(wallTypesJson, @"""name""\s*:\s*""Door, Interior Office"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;

            var viewInfo = JArray.Parse(File.ReadAllText(Path.Combine(buildToolsPath, "imageData.json")));

            var wallPointsList = new List<string>();
            var wallSegmentsList = new List<string>();

            foreach (var imageFile in imageFiles)
            {
                string imageFileName = Path.GetFileName(imageFile);
                string viewName = imageFileName.Replace("exported_view - Floor Plan - ", "").Replace(".bmp", "").Replace(" ", "_");

                var viewEntry = viewInfo.FirstOrDefault(v => (string)v["viewName"] == viewName.Replace("_", " "));
                if (viewEntry == null) continue;

                double minX = (double)viewEntry["min"]["x"];
                double maxX = (double)viewEntry["max"]["x"];
                double minY = (double)viewEntry["min"]["y"];
                double maxY = (double)viewEntry["max"]["y"];
                int imageWidth = (int)viewEntry["width"];
                int imageHeight = (int)viewEntry["height"];

                Func<double, double> convertX = (x) => (x - minX) / (maxX - minX) * imageWidth;
                Func<double, double> convertY = (y) => (maxY - y) / (maxY - minY) * imageHeight;

                string floorPlanId = floorPlans["floorPlans"]
                    .FirstOrDefault(fp => (string)fp["name"] == imageFileName)?["id"]?.ToString();
                if (string.IsNullOrEmpty(floorPlanId)) continue;

                // Procesar elementos originales
                string elementsPath = Path.Combine(buildToolsPath, $"elements_{viewName}.json");
                if (File.Exists(elementsPath))
                {
                    var elementsJson = JToken.Parse(File.ReadAllText(elementsPath));
                    //SubFunctions.WallNoOpen.ProcessWallNoOpen(elementsJson, floorPlanId, convertX, convertY, wallConcreteId, wallPointsList, wallSegmentsList);
                    SubFunctions.WallElements.ProcessWallElements(elementsJson, floorPlanId, convertX, convertY, windowInteriorId, doorInteriorId, wallPointsList, wallSegmentsList);
                }

                // Procesar elementos divididos (solo muros sin aberturas)
                string[] splitFiles = Directory.GetFiles(buildToolsPath, $"muros_divididos_{viewName}*.json");
                foreach (var splitFile in splitFiles)
                {
                    var splitJson = JToken.Parse(File.ReadAllText(splitFile));
                    SubFunctions.WallNoOpen.ProcessWallNoOpen(splitJson, floorPlanId, convertX, convertY, wallConcreteId, wallPointsList, wallSegmentsList);
                }
            }

            if (!wallPointsList.Any() || !wallSegmentsList.Any())
            {
                TaskDialog.Show("Aviso", "No se generaron elementos.");
                return Result.Succeeded;
            }

            File.WriteAllText(Path.Combine(myCopyFolder, "wallPoints.json"), "{\n  \"wallPoints\": [\n" + string.Join(",\n", wallPointsList) + "\n  ]\n}");
            File.WriteAllText(Path.Combine(myCopyFolder, "wallSegments.json"), "{\n  \"wallSegments\": [\n" + string.Join(",\n", wallSegmentsList) + "\n  ]\n}");

            return Result.Succeeded;
        }
    }
}

