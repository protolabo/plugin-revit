using Newtonsoft.Json;
using Create.ExportClasses;
using System.Reflection;

using Newtonsoft.Json.Linq;

using Create.ExportClasses;

public static class ImageJsonFileCreatorTester
{
    public static void RunTest(string model)
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        string viewInfoJson = File.ReadAllText(Path.Combine(tempFolderPath, "imageData.json"));
        var viewInfo = JsonConvert.DeserializeObject<List<ViewData>>(viewInfoJson);

        string testFilePath = $"./TestFiles/Models/{model}";

        string modelInfoContent = File.ReadAllText(testFilePath);
        JObject modelInfo = JObject.Parse(modelInfoContent);

        string wallFileName = (string)modelInfo["walls"];
        string wallFilePath = $"./TestFiles/Walls/{wallFileName}.json";

        string jsonPathModelData = File.ReadAllText(wallFilePath);
        Dictionary<string, ModelData> modelDataFile =
        JsonConvert.DeserializeObject<Dictionary<string, ModelData>>(jsonPathModelData);
        
        // Call to Plugin Class 
        ImageJsonFileCreator.FormatImagesAndCreateJsonFile(tempPath, modelDataFile, viewInfo);

        string jsonContent = File.ReadAllText(testFilePath);
        ModelInfo testInfo = JsonConvert.DeserializeObject<ModelInfo>(jsonContent);
        
        bool allImagesExist = true;

        foreach (var expectedImage in testInfo.images_expected)
        {
            string imagePath = Path.Combine(tempFolderPath, expectedImage + ".bmp");

            if (File.Exists(imagePath))
            {
                // Console.WriteLine($"✅ Found: {expectedImage}.bmp");
            }
            else
            {
                Console.WriteLine($"❌ Missing: {expectedImage}.bmp");
                allImagesExist = false;
            }
        }

        if (allImagesExist)
        {
            Console.WriteLine("✅ All expected images were found.");
        }
        else
        {
            Console.WriteLine("⚠️ Some expected images are missing.");
        }


        // Search for files starting by "image-" in Template folder
        string[] imageFiles = Directory.GetFiles(tempPath, "image-*");

        // verify files (images) quantity
        int expectedCount = testInfo.images_expected.Count;
        if (imageFiles.Length != expectedCount)
        {
            Console.WriteLine($"❌ Image count mismatch: expected {expectedCount}, found {imageFiles.Length}");
        }
        else
        {
            Console.WriteLine($"✅ Correct image count: {imageFiles.Length}");
        }

        // verify each files name
        bool allValidNames = true;
        foreach (string filePath in imageFiles)
        {
            string fileName = Path.GetFileName(filePath);
            if (IsValidGuidFromName(fileName))
            {
                // Console.WriteLine($"✅ Valid image name: {fileName}");
            }
            else
            {
                Console.WriteLine($"❌ Invalid image name: {fileName}");
                allValidNames = false;
            }
        }

        if (imageFiles.Length == expectedCount && allValidNames)
        {
            Console.WriteLine("✅ All image files are present and correctly named with GUIDs.");
        }
        else
        {
            Console.WriteLine("⚠️ Some image files are missing or incorrectly named.");
        }


        // string imageDataJson = File.ReadAllText(Path.Combine(tempFolderPath, "imageData.json"));
        // string configJson = File.ReadAllText(Path.Combine(tempFolderPath, "config.json"));
        string imagesJson = File.ReadAllText(Path.Combine(tempPath, "images.json")); 

        var viewInfoArray = JArray.Parse(viewInfoJson);
        var configData = JObject.Parse(jsonContent);
        var cropRegions = configData["cropRegions"] as JArray;

        var imagesData = JObject.Parse(imagesJson);
        var imagesArray = imagesData["images"] as JArray;

        bool allValid = true;
        string errorDetails = "";

        if (cropRegions == null || cropRegions.Count != viewInfoArray.Count)
        {
            allValid = false;
            errorDetails = "Mismatch in number of cropRegions and imageData entries.";
        }
        else
        {
            for (int i = 0; i < cropRegions.Count; i++)
            {
                var crop = cropRegions[i];

                // Match BMP image with same resolution
                var matchingImage = imagesArray?.FirstOrDefault(img =>
                    img["imageFormat"]?.ToString() == "BMP" &&
                    img["resolutionWidth"]?.ToObject<double>() == crop["width"]?.ToObject<double>() &&
                    img["resolutionHeight"]?.ToObject<double>() == crop["height"]?.ToObject<double>());

                if (matchingImage == null)
                {
                    allValid = false;
                    errorDetails = $"No matching BMP image found for cropRegion at index {i}.";
                    break;
                }

                // Validate image ID is a valid GUID
                string imageId = matchingImage["id"]?.ToString();
                if (!IsValidGuid(imageId))
                {
                    allValid = false;
                    errorDetails = $"Image ID '{imageId}' is not a valid GUID at index {i}.";
                    break;
                }
            }
        }

        if (allValid)
            Console.WriteLine("✅ All cropRegions and BMP image entries match and have valid GUIDs.");
        else
            Console.WriteLine($"❌ Validation failed: {errorDetails}");



        string jsonFloorPath = Path.Combine(tempPath, "floorPlans.json");
        string floorPlanContent = File.ReadAllText(jsonFloorPath);

        JObject parsedJsonFloor = JObject.Parse(floorPlanContent);
        JArray floorPlans = (JArray)parsedJsonFloor["floorPlans"];

        bool allValidFloor = true;

        foreach (var region in testInfo.cropRegions)
        {
            string levelNameFormatted = region.viewName.Replace("_", " ");
            string fileName = $"exported_view - Floor Plan - {levelNameFormatted}.bmp";

            // Search for floorPlan matching that name
            var matchingFloor = floorPlans.FirstOrDefault(f => (string)f["name"] == fileName);

            if (matchingFloor == null)
            {
                Console.WriteLine($"❌ No floorPlan entry found for image: {fileName}");
                allValidFloor = false;
                continue;
            }

            // Verify required fields
            var width = matchingFloor["width"]?.ToObject<double?>();
            var height = matchingFloor["height"]?.ToObject<double?>();
            var cropMinX = matchingFloor["cropMinX"]?.ToObject<double?>();
            var cropMinY = matchingFloor["cropMinY"]?.ToObject<double?>();
            var cropMaxX = matchingFloor["cropMaxX"]?.ToObject<double?>();
            var cropMaxY = matchingFloor["cropMaxY"]?.ToObject<double?>();
            var metersPerUnit = matchingFloor["metersPerUnit"]?.ToObject<double?>();
            var imageId = matchingFloor["imageId"]?.ToString();
            var id = matchingFloor["id"]?.ToString();

            if (width == null || height == null || cropMinX == null || cropMinY == null ||
                cropMaxX == null || cropMaxY == null || metersPerUnit == null ||
                imageId == null || id == null)
            {
                Console.WriteLine($"❌ Missing required fields in floorPlan: {fileName}");
                allValidFloor = false;
                continue;
            }

            // Verify width/height
            if (width != region.width || height != region.height)
            {
                Console.WriteLine($"❌ Mismatch in size for {fileName}: expected {region.width}x{region.height}, got {width}x{height}");
                allValidFloor = false;
            }

            // Verify cropMaxX/Y
            if (cropMaxX != region.width || cropMaxY != region.height)
            {
                Console.WriteLine($"❌ Mismatch in cropMax for {fileName}: expected ({region.width},{region.height}), got ({cropMaxX},{cropMaxY})");
                allValidFloor = false;
            }

            // Verify cropMinX/Y equals 0
            if (cropMinX != 0.0 || cropMinY != 0.0)
            {
                Console.WriteLine($"❌ cropMinX/Y should be 0.0 for {fileName}, got ({cropMinX},{cropMinY})");
                allValidFloor = false;
            }

            // Verify metersPerUnit ≈ scale
            if (Math.Abs(metersPerUnit.Value - region.scale) > 0.001)
            {
                Console.WriteLine($"❌ Mismatch in scale for {fileName}: expected ~{region.scale}, got {metersPerUnit}");
                allValidFloor = false;
            }

            // Verify imageId and id to be valid GUID
            if (!IsValidGuid(imageId))
            {
                Console.WriteLine($"❌ Invalid imageId GUID for {fileName}: {imageId}");
                allValidFloor = false;
            }

            if (!IsValidGuid(id))
            {
                Console.WriteLine($"❌ Invalid id GUID for {fileName}: {id}");
                allValidFloor = false;
            }
        }

        if (allValidFloor)
            Console.WriteLine("✅ All floorPlans match their cropRegion definitions.");
        else
            Console.WriteLine("⚠️ One or more floorPlans are missing or invalid.");


    }

    private static bool IsValidGuidFromName(string fileName)
    {
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        if (nameWithoutExtension.StartsWith("image-"))
        {
            string possibleGuid = nameWithoutExtension.Substring("image-".Length);
            return IsValidGuid(possibleGuid);
        }

        return false;
    }
    

    private static bool IsValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }


}