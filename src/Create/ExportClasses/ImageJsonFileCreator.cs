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
        public static Result FormatImagesAndCreateJsonFile(string destDir, Dictionary<string, ModelData> modelData, List<ViewData> viewInfo)
        {
            // Path to files
            string buildFilesPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exportDir = Path.Combine(buildFilesPath, "build_files", "tempFolder");
            string[] exportedBmps = Directory.GetFiles(exportDir, "exported_view - *.bmp");
            //var viewInfo = JArray.Parse(File.ReadAllText(Path.Combine(exportDir, "imageData.json")));

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
                    double metersPerUnit = GetMetersPerUnit(originalImageName, exportDir, viewInfo, modelData);

                    // Scale calculated successfully
                    if (metersPerUnit == 0.0)
                    {
                        JObject floorEntry = new JObject
                        {
                            ["name"] = originalImageName,
                            ["width"] = width,
                            ["height"] = height,
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
                    else
                    {
                        JObject floorEntry = new JObject
                        {
                            ["name"] = originalImageName,
                            ["width"] = width,
                            ["height"] = height,
                            ["metersPerUnit"] = metersPerUnit,
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
        private static double GetMetersPerUnit(string imageName, string directoryPath, List<ViewData> viewInfo, Dictionary<string, ModelData> modelData)
        {
            // Extract view name from image file name
            string viewName = imageName
                .Replace("exported_view - Floor Plan - ", "")
                .Replace(".bmp", "")
                .Replace(" ", "_");

            // Match against viewInfo using formatted view name
            string searchViewName = viewName.Replace("_", " ");
            var viewEntry = viewInfo.FirstOrDefault(v => v.viewName == searchViewName);
            if (viewEntry == null)
            {
                Console.WriteLine($"❌ View '{searchViewName}' not found in viewInfo.");
                return 0.0;
            }

            // Get crop box corners in Revit coordinates (in feet)
            double minX = viewEntry.min.x;
            double minY = viewEntry.min.y;
            double maxX = viewEntry.max.x;
            double maxY = viewEntry.max.y;

            // Compute the diagonal distance (in feet) between crop box corners
            double deltaX_ft = maxX - minX;
            double deltaY_ft = maxY - minY;
            double diagonalFeet = Math.Sqrt(deltaX_ft * deltaX_ft + deltaY_ft * deltaY_ft);

            // Convert diagonal to meters
            const double feetToMeters = 0.3048;
            double diagonalMeters = diagonalFeet * feetToMeters;

            // Get image dimensions in pixels
            int imageWidth = viewEntry.width;
            int imageHeight = viewEntry.height;

            // Compute diagonal of the image in pixels
            double diagonalPixels = Math.Sqrt(imageWidth * imageWidth + imageHeight * imageHeight);

            if (diagonalPixels == 0)
            {
                Console.WriteLine("❌ Image diagonal is zero. Invalid width/height.");
                return 0.0;
            }

            // Calculate meters per pixel
            double metersPerUnit = diagonalMeters / diagonalPixels;
            return metersPerUnit;
        }

    }
}
