using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Create.ExportClasses;

public static class WallsInserterTester
{
    public static void RunTest(string model)
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        // Delete existing output JSON files if they exist
        string wallPointsJson = Path.Combine(tempPath, "wallPoints.json");
        string wallSegmentsJson = Path.Combine(tempPath, "wallSegments.json");

        if (File.Exists(wallPointsJson)) File.Delete(wallPointsJson);
        if (File.Exists(wallSegmentsJson)) File.Delete(wallSegmentsJson);

        // Step 1: Read configuration file
        string configPath = $"./TestFiles/Models/{model}";
        JObject config = JObject.Parse(File.ReadAllText(configPath));

        // Step 2: Get walls filename from config
        string wallsFileName = (string)config["walls"];
        string wallsFilePath = Path.Combine("./TestFiles/Walls", wallsFileName + ".json");

        // Step 3: Read walls JSON content
        if (!File.Exists(wallsFilePath))
        {
            Console.WriteLine($"❌ Walls file not found: {wallsFilePath}");
            return; // Or handle error as needed
        }
        string wallsJson = File.ReadAllText(wallsFilePath);

        // Step 4: Deserialize to list of WallSegment
        string jsonPath = File.ReadAllText(Path.Combine(tempFolderPath, "walls_segments.json"));
        Dictionary<string, ModelData> modelSegments =
        JsonConvert.DeserializeObject<Dictionary<string, ModelData>>(jsonPath);

        // Create viewInfo object (replace with your actual implementation)
        string viewInfoJson = File.ReadAllText(System.IO.Path.Combine(tempFolderPath, "imageData.json"));
        var viewInfo = JsonConvert.DeserializeObject<List<ViewData>>(viewInfoJson);

        // Call to Plugin Class
        WallsInserter.InsertWallAndOpeningsInEkahauFile(modelSegments, viewInfo);

        // Load expected filenames from config
        string expectedPointsPath = Path.Combine("./TestFiles/WallPoints", (string)config["wallPoints_expected"] + ".json");
        // string expectedSegmentsPath = Path.Combine("./TestFiles/WallSegments", (string)config["wallSegments_expected"] + ".json");

        // ✅ Validate wallPoints.json
        bool allValidPointsJson = false;

        if (!File.Exists(wallPointsJson))
        {
            Console.WriteLine("❌ wallPoints.json not found.");
        }
        else if (!File.Exists(expectedPointsPath))
        {
            Console.WriteLine($"❌ Expected file not found: {expectedPointsPath}");
        }
        else
        {
            string actualJson = File.ReadAllText(wallPointsJson);
            string expectedJson = File.ReadAllText(expectedPointsPath);

            JToken actual = JToken.Parse(actualJson);
            JToken expected = JToken.Parse(expectedJson);

            JArray actualArray = (JArray)actual["wallPoints"];
            JArray expectedArray = (JArray)expected["wallPoints"];

            bool sameCount = actualArray.Count == expectedArray.Count;
            bool allCoordsMatch = true;
            bool allGuidsValid = true;
            double tolerance = 1e-9;

            if (!sameCount)
            {
                Console.WriteLine($"❌ Different number of entries: actual={actualArray.Count}, expected={expectedArray.Count}");
                allCoordsMatch = false;
            }

            List<JToken> remainingActualPoints = actualArray.ToList();

            foreach (var expectedPoint in expectedArray)
            {
                var expectedCoord = expectedPoint["location"]["coord"];
                double expectedX = (double)expectedCoord["x"];
                double expectedY = (double)expectedCoord["y"];

                var match = remainingActualPoints.FirstOrDefault(actualPoint =>
                {
                    var actualCoord = actualPoint["location"]["coord"];
                    double actualX = (double)actualCoord["x"];
                    double actualY = (double)actualCoord["y"];

                    return Math.Abs(actualX - expectedX) < tolerance &&
                        Math.Abs(actualY - expectedY) < tolerance;
                });

                if (match != null)
                {
                    // Validate GUID
                    string id = (string)match["id"];
                    string floorPlanId = (string)match["location"]["floorPlanId"];

                    if (!IsValidGuid(id))
                    {
                        allGuidsValid = false;
                        Console.WriteLine($"❌ Invalid GUID for 'id': {id}");
                    }

                    if (!IsValidGuid(floorPlanId))
                    {
                        allGuidsValid = false;
                        Console.WriteLine($"❌ Invalid GUID for 'floorPlanId': {floorPlanId}");
                    }

                    remainingActualPoints.Remove(match);
                }
                else
                {
                    allCoordsMatch = false;
                    Console.WriteLine("❌ Expected point not found in actual:");
                    Console.WriteLine($"   Expected: {expectedCoord}");
                }
            }

            allValidPointsJson = sameCount && allCoordsMatch && allGuidsValid;

            if (allValidPointsJson)
            {
                Console.WriteLine("✅ wallPoints.json is valid: coordinates match and all GUIDs are valid.");
            }
            else
            {
                Console.WriteLine("❌ wallPoints.json has validation errors.");
            }
        }


        // ✅ Validate wallSegments.json with GUID checks (no expected comparison)
        bool allValidSegmentsJson = false;

        if (!File.Exists(wallSegmentsJson))
        {
            Console.WriteLine("❌ wallSegments.json not found.");
        }
        else
        {
            string actualJson = File.ReadAllText(wallSegmentsJson);
            JToken actual = JToken.Parse(actualJson);

            JArray actualSegments = (JArray)actual["wallSegments"];
            bool allGuidsValid = true;

            if (actualSegments == null)
            {
                Console.WriteLine("❌ wallSegments array is missing in wallSegments.json.");
            }
            else
            {
                for (int i = 0; i < actualSegments.Count; i++)
                {
                    var segment = actualSegments[i];

                    // Validate GUIDs in wallPoints (array of strings)
                    var wallPoints = segment["wallPoints"] as JArray;
                    if (wallPoints != null)
                    {
                        for (int j = 0; j < wallPoints.Count; j++)
                        {
                            string wpId = (string)wallPoints[j];
                            if (!IsValidGuid(wpId))
                            {
                                allGuidsValid = false;
                                Console.WriteLine($"❌ Invalid GUID in wallPoints[{j}] at segment index {i}: {wpId}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Warning: wallPoints is missing or not an array at segment index {i}");
                    }

                    // Validate GUID in wallTypeId
                    string wallTypeId = (string)segment["wallTypeId"];
                    if (!IsValidGuid(wallTypeId))
                    {
                        allGuidsValid = false;
                        Console.WriteLine($"❌ Invalid GUID for wallTypeId at segment index {i}: {wallTypeId}");
                    }

                    // Validate GUID in id
                    string id = (string)segment["id"];
                    if (!IsValidGuid(id))
                    {
                        allGuidsValid = false;
                        Console.WriteLine($"❌ Invalid GUID for id at segment index {i}: {id}");
                    }
                }

                if (allGuidsValid)
                {
                    allValidSegmentsJson = true;
                    Console.WriteLine("✅ wallSegments.json GUIDs are all valid.");
                }
                else
                {
                    Console.WriteLine("❌ wallSegments.json validation failed due to invalid GUIDs.");
                }
            }
        }

    }
    
    private static bool IsValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}

