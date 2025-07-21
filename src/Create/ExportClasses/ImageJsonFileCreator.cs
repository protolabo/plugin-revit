using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Create.ExportClasses
{
    internal class ImageJsonFileCreator
    {
        public static Result FormatImagesAndCreateJsonFile(string destDir)
        {
            // Path to files
            string buildFilesPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exportDir = Path.Combine(buildFilesPath, "build_files", "tempFolder");
            string[] exportedBmps = Directory.GetFiles(exportDir, "exported_view - *.bmp");
            var viewInfo = JArray.Parse(File.ReadAllText(Path.Combine(exportDir, "imageData.json")));

            if (exportedBmps.Length == 0)
            {
                TaskDialog.Show("Error", "No exported BMP images were found.");
                return Result.Failed;
            }

            JArray imagesArray = new JArray();
            JArray floorPlansArray = new JArray();

            foreach (var sourceImagePath in exportedBmps)
            {
                // All images name in Ekahau must follow the format: "name-GUID".
                // This code renames each image to follow the specific format and moves it to the folder containing the rest of the Ekahau JSON files.
                // Then updates the images.json file, which holds the list of all images included in the Ekahau file.
                // Finally, updates the floorPlans.json file, which contains information about each floor (Revit view),
                // referencing the respective image name so that it appears as the background (map) in the Ekahau project.
                string originalImageName = Path.GetFileName(sourceImagePath);
                string imageId = Guid.NewGuid().ToString();
                string imageName = $"image-{imageId}";
                string destImagePath = Path.Combine(destDir, imageName);

                File.Copy(sourceImagePath, destImagePath, true);

                using (var img = Image.FromFile(destImagePath))
                {
                    double width = img.Width;
                    double height = img.Height;

                    // Add to Ekahau json file images.json
                    JObject imageEntry = new JObject
                    {
                        ["imageFormat"] = "BMP",
                        ["resolutionWidth"] = width,
                        ["resolutionHeight"] = height,
                        ["id"] = imageId,
                        ["status"] = "CREATED"
                    };
                    imagesArray.Add(imageEntry);

                    // Add to Ekahau json file floorPlans.json
                    JObject floorEntry = new JObject
                    {
                        ["name"] = originalImageName,
                        ["width"] = width,
                        ["height"] = height,
                        ["metersPerUnit"] = GetMetersPerUnit(originalImageName, exportDir, viewInfo),
                        ["imageId"] = imageId,
                        ["gpsReferencePoints"] = new JArray(),
                        ["floorPlanType"] = "FSPL",
                        ["cropMinX"] = 0.0,
                        ["cropMinY"] = 0.0,
                        ["cropMaxX"] = width,
                        ["cropMaxY"] = height,
                        ["rotateUpDirection"] = "UP",
                        ["tags"] = new JArray(),
                        ["id"] = Guid.NewGuid().ToString(),
                        ["status"] = "CREATED"
                    };
                    floorPlansArray.Add(floorEntry);
                }
            }

            // Write images.json
            JObject imagesJson = new JObject { ["images"] = imagesArray };
            string imagesJsonPath = Path.Combine(destDir, "images.json");
            File.WriteAllText(imagesJsonPath, imagesJson.ToString(Newtonsoft.Json.Formatting.Indented));

            // Write floorPlans.json
            JObject floorPlansJson = new JObject { ["floorPlans"] = floorPlansArray };
            string floorPlansPath = Path.Combine(destDir, "floorPlans.json");
            File.WriteAllText(floorPlansPath, floorPlansJson.ToString(Newtonsoft.Json.Formatting.Indented));

            return Result.Succeeded;
        }

        // This function is used to automatically specify the model scale in Ekahau
        private static double GetMetersPerUnit(string imageName, string directoryPath, JArray viewInfo)
        {
            // Convert image name to view name and construct the path for elements JSON
            string viewName = imageName.Replace("exported_view - Floor Plan - ", "").Replace(".bmp", "").Replace(" ", "_");
            string elementsFilePath = Path.Combine(directoryPath, $"elements_{viewName}.json");

            // Read and parse elements JSON
            string jsonContent = File.ReadAllText(elementsFilePath);
            JObject root = JObject.Parse(jsonContent);
            JToken firstWall = root["walls"]?.First;

            // Extract start and end points (in feet)
            var start = firstWall["start"];
            var end = firstWall["end"];

            double startX = start.Value<double>("x");
            double startY = start.Value<double>("y");
            // Z coordinate is ignored for 2D calculation

            double endX = end.Value<double>("x");
            double endY = end.Value<double>("y");

            // Convert feet to meters (1 foot = 0.3048 meters)
            const double feetToMeters = 0.3048;
            double startX_m = startX * feetToMeters;
            double startY_m = startY * feetToMeters;

            double endX_m = endX * feetToMeters;
            double endY_m = endY * feetToMeters;

            // Calculate 2D distance in meters
            double deltaX_m = endX_m - startX_m;
            double deltaY_m = endY_m - startY_m;
            double distanceMeters = Math.Sqrt(deltaX_m * deltaX_m + deltaY_m * deltaY_m);

            // Find the corresponding view entry in viewInfo using viewName (replace underscores with spaces)
            string searchViewName = viewName.Replace("_", " ");
            var viewEntry = viewInfo.FirstOrDefault(v => (string)v["viewName"] == searchViewName);

            // Get bounding box and image size info
            double minX = (double)viewEntry["min"]["x"];
            double maxX = (double)viewEntry["max"]["x"];
            double minY = (double)viewEntry["min"]["y"];
            double maxY = (double)viewEntry["max"]["y"];
            int imageWidth = (int)viewEntry["width"];
            int imageHeight = (int)viewEntry["height"];

            // Conversion functions: from Revit feet coordinates to Ekahau pixel coordinates
            Func<double, double> convertX = x => (x - minX) / (maxX - minX) * imageWidth;
            Func<double, double> convertY = y => (maxY - y) / (maxY - minY) * imageHeight;

            // Convert start and end points to pixel coordinates
            double startX_px = convertX(startX);
            double startY_px = convertY(startY);

            double endX_px = convertX(endX);
            double endY_px = convertY(endY);

            // Calculate 2D pixel distance
            double deltaX_px = endX_px - startX_px;
            double deltaY_px = endY_px - startY_px;
            double distancePixels = Math.Sqrt(deltaX_px * deltaX_px + deltaY_px * deltaY_px);

            // Prevent division by zero
            if (distancePixels == 0)
            {
                return 0.026190351367214426;
            }

            // Calculate meters per pixel
            double metersPerUnit = distanceMeters / distancePixels;
            return metersPerUnit;
        }

    }
}
