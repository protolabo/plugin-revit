using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Create.ExportClasses;
using System.Reflection;
using System.Text.RegularExpressions;

using System.Linq;
using Newtonsoft.Json.Linq;

class Program
{
    static void Main(string[] args)
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        // ImageJsonFileCreator TESTS =================================================================

        bool allValid = true;

        // Get Model info (json file instead of true revit model)
        string jsonPathModelData = File.ReadAllText("./TestFiles/Models/model_data.json");
        Dictionary<string, ModelData> modelDataFile =
        JsonConvert.DeserializeObject<Dictionary<string, ModelData>>(jsonPathModelData);

        // template image (for simulating image export)
        string sourceImagePath = Path.Combine(buildFilesDir, "build_tools", "template-image.bmp");

        var imageDataList = new List<ImageData>();

        // 🧹 Delete all .bmp files in tempFolderPath
        foreach (string file in Directory.GetFiles(tempFolderPath, "*.bmp"))
        {
            File.Delete(file);
        }

       // 🧹 Delete all files starting with "image-" in tempFolderPath
        foreach (string file in Directory.GetFiles(tempPath))
        {
            if (Path.GetFileName(file).StartsWith("image-"))
            {
                File.Delete(file);
            }
        }

        // 🧹 Delete imageData.json if it exists
        string imageDataPath = Path.Combine(tempFolderPath, "imageData.json");
        if (File.Exists(imageDataPath))
        {
            File.Delete(imageDataPath);
        }
       
        // simulates ModelDataExporter class
        foreach (var kvp in modelDataFile)
        {
            string levelKey = kvp.Key; // Ex: "Level_1"
            string levelNameFormatted = levelKey.Replace("_", " "); // Ex: "Level 1"
            string fileName = $"exported_view - Floor Plan - {levelNameFormatted}.bmp";

            string fullPath = Path.Combine(tempFolderPath, fileName);

            File.Copy(sourceImagePath, fullPath, overwrite: true);

            Console.WriteLine($"✅ Copied to: {fullPath}");

            imageDataList.Add(new ImageData
            {
                viewName = levelNameFormatted,
                min = new Point { x = -51.196990982960649, y = -72.002867143342812, z = -100.0 },
                max = new Point { x = 80.801157300258353, y = 39.52816143607194, z = 100.0 },
                width = 1500,
                height = 1267
            });
        }

        string imageDataJson = JsonConvert.SerializeObject(imageDataList, Formatting.Indented);
        File.WriteAllText(imageDataPath, imageDataJson);

        Console.WriteLine("✅ All files generated.");

        string viewInfoJson = File.ReadAllText(System.IO.Path.Combine(tempFolderPath, "imageData.json"));
        var viewInfo = JsonConvert.DeserializeObject<List<ViewData>>(viewInfoJson);

        // Call to Plugin Class 
        ImageJsonFileCreator.FormatImagesAndCreateJsonFile(tempPath, modelDataFile, viewInfo);

        // 🔍 Validate presence and format of image files and check the structure of 'imageData.json'.
        // 1. Look for all files named "image-*" in the temporary path and verify that each one follows the "image-{GUID}" format.
        // 2. Read and parse 'imageData.json' located in the temporary folder.
        // 3. Validate that each view entry contains required properties: viewName, min/max coordinates (x, y, z), width, and height.
        // 4. Output diagnostic messages based on validation results.

        string[] files = Directory.GetFiles(tempPath, "image-*");

        if (files.Length == 0)
        {
            Console.WriteLine("❌ No image file found with a name starting with 'image-'.");
        }
        else
        {
            // Console.WriteLine($"🔍 Found {files.Length} image file(s):");
            bool allValidNames = true;

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                Console.WriteLine($"  • {fileName}");

                if (IsValidGuidFromName(fileName))
                {
                    // Console.WriteLine("    ✅ Valid format: image-{GUID}");
                }
                else
                {
                    Console.WriteLine("    ❌ Invalid format (expected: image-{GUID}.bmp)");
                    allValidNames = false;
                }
            }

            if (allValidNames)
                Console.WriteLine("✅ All image filenames have a valid GUID format.");
            else
                Console.WriteLine("❌ Some image filenames do not match the expected format.");
        }

        string jsonImageData = Path.Combine(tempFolderPath, "imageData.json");
        string imageData = File.ReadAllText(jsonImageData);

        JArray viewInfoArray = JArray.Parse(imageData);

        bool allValidImageData = true;

        foreach (var view in viewInfoArray)
        {
            var viewName = view["viewName"];
            var min = view["min"];
            var max = view["max"];
            var width = view["width"];
            var height = view["height"];

            if (viewName == null || min == null || max == null || width == null || height == null)
            {
                allValid = false;
                break;
            }

            var minX = min["x"];
            var minY = min["y"];
            var minZ = min["z"];
            var maxX = max["x"];
            var maxY = max["y"];
            var maxZ = max["z"];

            if (minX == null || minY == null || minZ == null || maxX == null || maxY == null || maxZ == null)
            {
                allValid = false;
                break;
            }
        }

        if (allValidImageData)
            Console.WriteLine("✅ All viewInfo entries have the expected structure.");
        else
            Console.WriteLine("❌ Some viewInfo entries are missing required fields.");


        string jsonFloorPath = Path.Combine(tempPath, "floorPlans.json");
        string floorPlan = File.ReadAllText(jsonFloorPath);

        JObject parsedJsonFloor = JObject.Parse(floorPlan);
        JArray floorPlans = (JArray)parsedJsonFloor["floorPlans"];

        bool allValidFloor = true;

        foreach (var floor in floorPlans)
        {
            var name = floor["name"];
            var width = floor["width"];
            var height = floor["height"];
            var metersPerUnit = floor["metersPerUnit"];
            var imageId = floor["imageId"];
            var gpsRef = floor["gpsReferencePoints"];
            var floorPlanType = floor["floorPlanType"];
            var cropMinX = floor["cropMinX"];
            var cropMinY = floor["cropMinY"];
            var cropMaxX = floor["cropMaxX"];
            var cropMaxY = floor["cropMaxY"];
            var rotateUp = floor["rotateUpDirection"];
            var tags = floor["tags"];
            var id = floor["id"];
            var status = floor["status"];

            if (name == null || width == null || height == null || metersPerUnit == null ||
                imageId == null || gpsRef == null || floorPlanType == null ||
                cropMinX == null || cropMinY == null || cropMaxX == null || cropMaxY == null ||
                rotateUp == null || tags == null || id == null || status == null)
            {
                allValidFloor = false;
                break;
            }
        }

        if (allValidFloor)
            Console.WriteLine("✅ All floorPlans entries have the expected structure.");
        else
            Console.WriteLine("❌ Some floorPlans entries are missing required fields.");


        // WallSplitter TESTS ===========================================================

        // Performs wall segmentation and compares the result with a file containing the expected output
        string inputPath = @"./TestFiles/Models/model_data.json";
        string outputPath = @"./TestFiles/Models/model_data_segments.json";
        string expectedPath = @"./TestFiles/Models/expected_model_data_segments.json";

        try
        {
            // Delete the existing segmented output file if it exists
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
                // Console.WriteLine($"🗑️ Deleted existing file: {outputPath}");
            }

            // Read input and deserialize
            string json = File.ReadAllText(inputPath);
            var modelData = JsonConvert.DeserializeObject<Dictionary<string, ModelData>>(json);
            var modelDataSegments = new Dictionary<string, ModelData>();

            // Process wall segmentation
            WallSplitter.SplitWallByOpening(modelData, modelDataSegments);

            // Serialize current output
            string outputJson = JsonConvert.SerializeObject(modelDataSegments, Formatting.Indented);
            File.WriteAllText(outputPath, outputJson);

            Console.WriteLine("✅ Segmentation completed.");

            // Read expected output file
            if (File.Exists(expectedPath))
            {
                string expectedJson = File.ReadAllText(expectedPath);

                // Normalize both texts for comparison
                var actualNormalized = JsonConvert.DeserializeObject<object>(outputJson);
                var expectedNormalized = JsonConvert.DeserializeObject<object>(expectedJson);

                if (JsonConvert.SerializeObject(actualNormalized) == JsonConvert.SerializeObject(expectedNormalized))
                {
                    Console.WriteLine("✅ The result matches the expected file.");
                }
                else
                {
                    Console.WriteLine("❌ The result DOES NOT match the expected file.");
                }
            }
            else
            {
                Console.WriteLine("⚠️ Expected file not found: " + expectedPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }


        // Getters TESTS ===========================================================

        // Verify that the value obtained from the Getters class corresponds to a valid GUID

        var wallTypesJson = File.ReadAllText(System.IO.Path.Combine(tempPath, "wallTypes.json"));
        string wallId = Getters.GetWallId("Exterior - Brick on Mtl. Stud", wallTypesJson);

        if (Guid.TryParse(wallId, out Guid parsedGuid))
        {
            Console.WriteLine($"✅ Wall ID is a valid GUID: {parsedGuid}");
        }
        else
        {
            Console.WriteLine($"❌ Wall ID is NOT a valid GUID: {wallId}");
        }


        // SegmentsListCreator TESTS =================================================

        var wallPointsList = new List<string>();
        var wallSegmentsList = new List<string>();

        string jsonPath = File.ReadAllText("./TestFiles/Models/model_data_segments.json");
        Dictionary<string, ModelData> modelSegments =
        JsonConvert.DeserializeObject<Dictionary<string, ModelData>>(jsonPath);

        
        foreach (var viewEntry in viewInfo)
        {
            string viewName = viewEntry.viewName.Replace(" ", "_");

            double minX = viewEntry.min.x;
            double maxX = viewEntry.max.x;
            double minY = viewEntry.min.y;
            double maxY = viewEntry.max.y;
            int imageWidth = viewEntry.width;
            int imageHeight = viewEntry.height;

            Func<double, double> convertX = (x) => (x - minX) / (maxX - minX) * imageWidth;
            Func<double, double> convertY = (y) => (maxY - y) / (maxY - minY) * imageHeight;

            SegmentsListCreator.FillSegmentsList(modelSegments[viewName].walls, Guid.NewGuid().ToString(), convertX, convertY, 
            tempPath, wallPointsList, wallSegmentsList);
        }

        var regexPoints = new Regex(@"
            ""location""\s*:\s*{[^}]*      # location object
                ""floorPlanId""\s*:\s*""[^""]+""\s*,\s*  # floorPlanId string
                ""coord""\s*:\s*{[^}]*      # coord object
                    ""x""\s*:\s*[\d\.-]+,\s*
                    ""y""\s*:\s*[\d\.-]+
                \s*}
            \s*},\s*
            ""id""\s*:\s*""[^""]+"",\s*
            ""status""\s*:\s*""[^""]+""
        ", RegexOptions.IgnorePatternWhitespace);

        bool allValidpoints = wallPointsList.All(entry => regexPoints.IsMatch(entry));

        if (allValidpoints)
        {
           Console.WriteLine("✅ All strings Points match the expected WallSegment structure.");
        }
        else
        {
            Console.WriteLine("❌ Some strings Points do NOT match the expected WallSegment structure.");
        }

        var regexSegments = new Regex(@"
            ""wallPoints""\s*:\s*\[
                \s*""[0-9a-fA-F\-]+""\s*,\s*  # Primer GUID
                ""[0-9a-fA-F\-]+""\s*        # Segundo GUID mínimo (pueden ser más)
            \]\s*,\s*
            ""wallTypeId""\s*:\s*""[0-9a-fA-F\-]+""\s*,\s*
            ""originType""\s*:\s*""[^""]+""\s*,\s*
            ""id""\s*:\s*""[0-9a-fA-F\-]+""\s*,\s*
            ""status""\s*:\s*""[^""]+""
        ", RegexOptions.IgnorePatternWhitespace);

        bool allValidSegments = wallSegmentsList.All(segment => regexSegments.IsMatch(segment));

        if (allValidSegments)
        {
            Console.WriteLine("✅ All strings Segments match the expected WallSegment structure.");
        }
        else
        {
            Console.WriteLine("❌ Some strings Segments do NOT match the expected WallSegment structure.");
        }


        // WallsInserter TESTS =================================================

        // Verify that the writing of generated points and segments is correctly performed in the corresponding Ekahau JSON files

        // 🧹 Delete existing output JSON files if they exist
        string wallPointsJson = Path.Combine(tempPath, "wallPoints.json");
        string wallSegmentsJson = Path.Combine(tempPath, "wallSegments.json");

        if (File.Exists(wallPointsJson))
        {
            File.Delete(wallPointsJson);
            // Console.WriteLine("🗑️ Deleted existing wallPoints.json");
        }

        if (File.Exists(wallSegmentsJson))
        {
            File.Delete(wallSegmentsJson);
            // Console.WriteLine("🗑️ Deleted existing imageData.json");
        }

        // Call to Plugin Class
        WallsInserter.InsertWallAndOpeningsInEkahauFile(modelSegments, viewInfo);

        // 🔎 Validate wallPoints.json
        string wallPointsPath = Path.Combine(tempPath, "wallPoints.json");
        bool allValidPointsJson = false;

        if (!File.Exists(wallPointsPath))
        {
            Console.WriteLine("❌ wallPoints.json not found.");
        }
        else
        {
            string jsonText = File.ReadAllText(wallPointsPath);
            JObject parsedJson = JObject.Parse(jsonText);

            if (parsedJson["wallPoints"] is JArray wallPoints)
            {
                allValidPointsJson = true;

                foreach (var point in wallPoints)
                {
                    var location = point["location"];
                    var id = point["id"];
                    var status = point["status"];

                    if (location == null || id == null || status == null)
                    {
                        allValidPointsJson = false;
                        break;
                    }

                    var floorPlanId = location["floorPlanId"];
                    var coord = location["coord"];
                    var x = coord?["x"];
                    var y = coord?["y"];

                    if (floorPlanId == null || coord == null || x == null || y == null)
                    {
                        allValidPointsJson = false;
                        break;
                    }
                }

                if (allValidPointsJson)
                    Console.WriteLine("✅ All wallPoints have the expected structure.");
                else
                    Console.WriteLine("❌ Some wallPoints are missing required fields.");
            }
            else
            {
                Console.WriteLine("❌ 'wallPoints' property is missing or not an array.");
            }
        }


        // 🔎 Validate wallSegments.json
        string wallSegmentsPath = Path.Combine(tempPath, "wallSegments.json");
        bool allValidSegmentsJson = false;

        if (!File.Exists(wallSegmentsPath))
        {
            Console.WriteLine("❌ wallSegments.json not found.");
        }
        else
        {
            string segmentsJson = File.ReadAllText(wallSegmentsPath);
            JObject parsedSegmentsJson = JObject.Parse(segmentsJson);

            if (parsedSegmentsJson["wallSegments"] is JArray wallSegments)
            {
                allValidSegmentsJson = true;

                foreach (var segment in wallSegments)
                {
                    var wallPointsArray = segment["wallPoints"] as JArray;
                    var wallTypeId = segment["wallTypeId"];
                    var originType = segment["originType"];
                    var id = segment["id"];
                    var status = segment["status"];

                    if (wallPointsArray == null || wallPointsArray.Count < 2 ||
                        wallTypeId == null || originType == null || id == null || status == null)
                    {
                        allValidSegmentsJson = false;
                        break;
                    }
                }

                if (allValidSegmentsJson)
                    Console.WriteLine("✅ All wallSegments have the expected structure.");
                else
                    Console.WriteLine("❌ Some wallSegments are missing required fields.");
            }
            else
            {
                Console.WriteLine("❌ 'wallSegments' property is missing or not an array.");
            }
        }
        
        // AttenuationUpdater TESTS ==================================================================

        // Verify that the attenuation update is correctly performed from the file provided by the user to the corresponding Ekahau JSON file

        AttenuationUpdater.UpdateEkahauValues();

        string wallTypesFilePath = Path.Combine(tempPath, "wallTypes.json");

        JArray mappingArray = JArray.Parse(File.ReadAllText(Path.Combine(assemblyFolder, "wall_data.json")));
        JObject wallTypesJsonFile = JObject.Parse(File.ReadAllText(wallTypesFilePath));

        JArray wallTypes = (JArray)wallTypesJsonFile["wallTypes"];

        bool allMatched = true;
        int totalChecked = 0;

        foreach (var group in mappingArray)
        {
            foreach (var prop in group.Children<JProperty>())
            {
                var array = prop.Value as JArray;
                if (array == null) continue;

                foreach (var item in array.Children<JObject>())
                {
                    string ekahauName = item["Ekahau"]?.ToString();
                    double actualAttenuation = item["Attenuation"]?.ToObject<double>() ?? -1;

                    if (string.IsNullOrWhiteSpace(ekahauName))
                        continue;

                    var wallType = wallTypes.FirstOrDefault(wt => wt["name"]?.ToString() == ekahauName);
                    if (wallType == null)
                    {
                        Console.WriteLine($"⚠️ No wallType found for Ekahau name: '{ekahauName}'");
                        continue;
                    }

                    double thickness = wallType["thickness"]?.ToObject<double>() ?? 0;
                    var firstPropagation = wallType["propagationProperties"]?.FirstOrDefault();
                    double attenuationFactor = firstPropagation?["attenuationFactor"]?.ToObject<double>() ?? 0;

                    double expected = thickness * attenuationFactor;

                    totalChecked++;

                    double diff = Math.Abs(expected - actualAttenuation);
                    if (diff <= 0.1)
                    {
                        // Console.WriteLine($"✅ Match for '{ekahauName}': expected ~{expected:F4}, got {actualAttenuation}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Mismatch for '{ekahauName}': expected ~{expected:F4}, got {actualAttenuation}, diff = {diff:F6}");
                        allMatched = false;
                    }
                }
            }
        }

        if (totalChecked == 0)
        {
            Console.WriteLine("⚠️ No valid entries found to check attenuation.");
        }
        else if (allMatched)
        {
            Console.WriteLine($"✅ All {totalChecked} attenuation values match expected values.");
        }
        else
        {
            Console.WriteLine("❌ Some attenuation values do not match expected values.");
        }

    }


    private static bool IsValidGuidFromName(string fileName)
    {
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        if (nameWithoutExtension.StartsWith("image-"))
        {
            string possibleGuid = nameWithoutExtension.Substring("image-".Length);
            return Guid.TryParse(possibleGuid, out _);
        }

        return false;
    }

   
    public class ImageData
    {
        public string viewName { get; set; }
        public Point min { get; set; }
        public Point max { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

}

