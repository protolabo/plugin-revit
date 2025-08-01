using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Create.ExportClasses;

public static class WallSegmentsTester
{
    public static void RunTest(string model, JObject config)
    {

        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        // Step 1: Read configuration file
        string configPath = $"./TestFiles/Integration/Models/{model}";
        // JObject config = JObject.Parse(File.ReadAllText(configPath));

        // Step 2: Get walls filename from config
        string wallsFileName = (string)config["walls"];
        string wallsFilePath = Path.Combine("./TestFiles/Integration/Walls", wallsFileName + ".json");

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

        string wallPointsJson = Path.Combine(tempPath, "wallPoints.json");
        string wallSegmentsJson = Path.Combine(tempPath, "wallSegments.json");

        // Load expected filenames from config
        string expectedPointsPath = Path.Combine("./TestFiles/Integration/WallPoints", (string)config["wallPoints_expected"] + ".json");
        string expectedSegmentsPath = Path.Combine("./TestFiles/Integration/WallSegments", (string)config["wallSegments_expected"] + ".json");

        double tolerance = 1e-9;
        
        // Validate wallSegments.json 
        var expectedSegmentPointsList = new List<(string segmentId, (double x, double y) point1, (double x, double y) point2)>();
        var segmentPointsList = new List<(string segmentId, (double x, double y) point1, (double x, double y) point2)>();

        if (!File.Exists(expectedSegmentsPath))
        {
            Console.WriteLine("❌ wallSegments_expected.json not found.");
        }
        else if (!File.Exists(expectedPointsPath))
        {
            Console.WriteLine("❌ wallPoints_expected.json not found.");
        }
        else
        {
            string expectedSegmentsJson = File.ReadAllText(expectedSegmentsPath);
            string expectedPointsJson = File.ReadAllText(expectedPointsPath);

            JToken expectedSegmentsToken = JToken.Parse(expectedSegmentsJson);
            JToken expectedPointsToken = JToken.Parse(expectedPointsJson);

            JArray expectedSegments = (JArray)expectedSegmentsToken["wallSegments"];
            JArray expectedPointsArray = (JArray)expectedPointsToken["wallPoints"];

            // Build a lookup: wallPointId → (x, y) coordinates
            Dictionary<string, (double x, double y)> expectedPointCoords = expectedPointsArray
                .Where(wp => wp["location"]?["coord"] != null)
                .ToDictionary(
                    wp => (string)wp["id"],
                    wp =>
                    {
                        var coord = wp["location"]["coord"];
                        return ((double)coord["x"], (double)coord["y"]);
                    }
                );

            // Build list of expected segments and their associated point coordinates
            foreach (var segment in expectedSegments)
            {
                string segmentId = (string)segment["id"];
                var wallPoints = segment["wallPoints"] as JArray;

                if (wallPoints != null && wallPoints.Count == 2)
                {
                    string wp1 = (string)wallPoints[0];
                    string wp2 = (string)wallPoints[1];

                    if (expectedPointCoords.ContainsKey(wp1) && expectedPointCoords.ContainsKey(wp2))
                    {
                        expectedSegmentPointsList.Add((segmentId, expectedPointCoords[wp1], expectedPointCoords[wp2]));
                    }
                }
            }

            // // Print expected segment IDs and their corresponding point coordinates
            // foreach (var entry in expectedSegmentPointsList)
            // {
            //     Console.WriteLine($"📐 Expected Segment GUID: {entry.segmentId}");
            //     Console.WriteLine($"    ↳ Point 1: ({entry.point1.x}, {entry.point1.y})");
            //     Console.WriteLine($"    ↳ Point 2: ({entry.point2.x}, {entry.point2.y})");
            // }
        }


        if (!File.Exists(wallSegmentsJson))
        {
            Console.WriteLine("❌ wallSegments.json not found.");
        }
        else if (!File.Exists(wallPointsJson))
        {
            Console.WriteLine("❌ wallPoints.json not found.");
        }
        else
        {
            string actualSegmentsJson = File.ReadAllText(wallSegmentsJson);
            string wallPointsContent = File.ReadAllText(wallPointsJson);

            JToken segmentsToken = JToken.Parse(actualSegmentsJson);
            JToken wallPointsToken = JToken.Parse(wallPointsContent);

            JArray actualSegments = (JArray)segmentsToken["wallSegments"];
            JArray wallPointsArray = (JArray)wallPointsToken["wallPoints"];

            // Build a lookup: wallPointId → (x, y) coordinates
            Dictionary<string, (double x, double y)> wallPointCoords = wallPointsArray
                .Where(wp => wp["location"]?["coord"] != null)
                .ToDictionary(
                    wp => (string)wp["id"],
                    wp =>
                    {
                        var coord = wp["location"]["coord"];
                        return ((double)coord["x"], (double)coord["y"]);
                    }
                );

            // Build list of segments and their associated point coordinates
            foreach (var segment in actualSegments)
            {
                string segmentId = (string)segment["id"];
                string wallTypeId = (string)segment["wallTypeId"];

                // Validate GUIDs here
                if (!IsValidGuid(segmentId))
                {
                    Console.WriteLine($"❌ Invalid GUID for segment id: {segmentId}");
                    continue; // Skip invalid segment
                }
                if (!IsValidGuid(wallTypeId))
                {
                    Console.WriteLine($"❌ Invalid GUID for wallTypeId in segment id: {segmentId}");
                    continue; // Skip invalid segment
                }

                var wallPoints = segment["wallPoints"] as JArray;

                if (wallPoints != null && wallPoints.Count == 2)
                {
                    string wp1 = (string)wallPoints[0];
                    string wp2 = (string)wallPoints[1];

                    if (wallPointCoords.ContainsKey(wp1) && wallPointCoords.ContainsKey(wp2))
                    {
                        segmentPointsList.Add((segmentId, wallPointCoords[wp1], wallPointCoords[wp2]));
                    }
                }
            }

            // // Print segment IDs and their corresponding point coordinates
            // foreach (var entry in segmentPointsList)
            // {
            //     Console.WriteLine($"🧱 Segment GUID: {entry.segmentId}");
            //     Console.WriteLine($"   ↳ Point 1: ({entry.point1.x}, {entry.point1.y})");
            //     Console.WriteLine($"   ↳ Point 2: ({entry.point2.x}, {entry.point2.y})");
            // }
        }

        bool PointsMatch((double x, double y) p1, (double x, double y) p2)
        {
            return Math.Abs(p1.x - p2.x) <= tolerance && Math.Abs(p1.y - p2.y) <= tolerance;
        }

        bool SegmentsMatch(
            (double x, double y) e1p1, (double x, double y) e1p2,
            (double x, double y) a1p1, (double x, double y) a1p2)
        {
            // Match if points equal in same order OR reversed order
            return (PointsMatch(e1p1, a1p1) && PointsMatch(e1p2, a1p2))
                || (PointsMatch(e1p1, a1p2) && PointsMatch(e1p2, a1p1));
        }

        // Test: Check length first
        if (expectedSegmentPointsList.Count != segmentPointsList.Count)
        {
            Console.WriteLine($"❌ Segment count mismatch: expected {expectedSegmentPointsList.Count}, actual {segmentPointsList.Count}");
        }
        else
        {
            Console.WriteLine($"✅ Segment counts match: {expectedSegmentPointsList.Count} segments");

            bool allMatched = true;

            // Create a copy of actual segments to remove matched ones
            var actualSegmentsCopy = segmentPointsList.ToList();

            foreach (var expectedSegment in expectedSegmentPointsList)
            {
                bool foundMatch = false;

                for (int i = 0; i < actualSegmentsCopy.Count; i++)
                {
                    var actualSegment = actualSegmentsCopy[i];
                    if (SegmentsMatch(expectedSegment.point1, expectedSegment.point2, actualSegment.point1, actualSegment.point2))
                    {
                        // Match found - remove to avoid duplicates
                        actualSegmentsCopy.RemoveAt(i);
                        foundMatch = true;
                        // Console.WriteLine($"✅ Match found for expected segment: {expectedSegment.segmentId}");
                        break;
                    }
                }

                if (!foundMatch)
                {
                    Console.WriteLine($"❌ No matching segment found for expected segment: {expectedSegment.segmentId}");
                    allMatched = false;
                }
            }

            if (allMatched)
            {
                Console.WriteLine("✅ All expected segments matched successfully.");
            }
            else
            {
                Console.WriteLine("⚠️ Some expected segments were not matched.");
            }
        }
    }

    private static bool IsValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}