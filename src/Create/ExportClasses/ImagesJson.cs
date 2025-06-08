using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Create.ExportClasses
{
    internal class ImagesJson
    {
        public static Result ProcessExportedBmp(string destDir)
        {
            string buildFilesPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exportDir = Path.Combine(buildFilesPath, "build_files", "build_tools");
            string[] exportedBmps = Directory.GetFiles(exportDir, "exported_view - *.bmp");

            if (exportedBmps.Length == 0)
            {
                TaskDialog.Show("Error", "No exported BMP images were found.");
                return Result.Failed;
            }

            JArray imagesArray = new JArray();
            JArray floorPlansArray = new JArray();

            foreach (var sourceImagePath in exportedBmps)
            {
                string originalImageName = Path.GetFileName(sourceImagePath);
                string imageId = Guid.NewGuid().ToString();
                string imageName = $"image-{imageId}";
                string destImagePath = Path.Combine(destDir, imageName);

                File.Copy(sourceImagePath, destImagePath, true);

                using (var img = Image.FromFile(destImagePath))
                {
                    double width = img.Width;
                    double height = img.Height;

                    // Add to images.json
                    JObject imageEntry = new JObject
                    {
                        ["imageFormat"] = "BMP",
                        ["resolutionWidth"] = width,
                        ["resolutionHeight"] = height,
                        ["id"] = imageId,
                        ["status"] = "CREATED"
                    };
                    imagesArray.Add(imageEntry);

                    // Add to floorPlans.json
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
    }
}
