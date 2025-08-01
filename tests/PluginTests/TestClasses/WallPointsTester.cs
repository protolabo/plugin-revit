using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Create.ExportClasses;

public static class WallPointsTester
{
    public static void RunTest(string model, JObject config)
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        string expectedPointsPath = Path.Combine("./TestFiles/Integration/WallPoints", (string)config["wallPoints_expected"] + ".json");
        if (!File.Exists(expectedPointsPath))
        {
            Console.WriteLine($"❌ Expected file not found: {expectedPointsPath}");
            return;
        }

        // Load expected wallPoints
        var expectedJson = File.ReadAllText(expectedPointsPath);
        var expected = JToken.Parse(expectedJson);
        var expectedPoints = (JArray)expected["wallPoints"];

        // Extract cropRegions from config
        var cropRegions = (JArray)config["cropRegions"];
        var expectedLists = new List<List<JToken>>();

        foreach (var region in cropRegions)
        {
            string viewName = (string)region["viewName"];
            string viewNameID = (string)region["viewNameID"];

            var points = expectedPoints
                .Where(p => (string)p["location"]["floorPlanId"] == viewNameID)
                .ToList();

            Console.WriteLine($"📌 Expected - View: {viewName} (ID: {viewNameID}) → {points.Count} points");
            expectedLists.Add(points);
        }

        // Load actual wallPoints
        var wallPointsJson = File.ReadAllText(Path.Combine(tempPath, "wallPoints.json"));
        var wallPointsRoot = JToken.Parse(wallPointsJson);
        var actualPoints = (JArray)wallPointsRoot["wallPoints"];

        // Validate GUIDs in actualPoints
        bool allGuidsValid = true;
        foreach (var point in actualPoints)
        {
            string id = (string)point["id"];
            string floorPlanId = (string)point["location"]["floorPlanId"];

            if (!IsValidGuid(id))
            {
                Console.WriteLine($"❌ Invalid GUID for 'id': {id}");
                allGuidsValid = false;
            }

            if (!IsValidGuid(floorPlanId))
            {
                Console.WriteLine($"❌ Invalid GUID for 'floorPlanId': {floorPlanId}");
                allGuidsValid = false;
            }
        }

        if (!allGuidsValid)
        {
            Console.WriteLine("❌ GUID validation failed. Please check invalid IDs.");
        }
        else
        {
            Console.WriteLine("✅ All GUIDs in actual points are valid.");
        }

        // Load floorPlans
        var floorPlansJson = File.ReadAllText(Path.Combine(tempPath, "floorPlans.json"));
        var floorPlansRoot = JToken.Parse(floorPlansJson);
        var floorPlans = (JArray)floorPlansRoot["floorPlans"];
        var actualLists = new List<List<JToken>>();

        foreach (var floorPlan in floorPlans)
        {
            string viewName = (string)floorPlan["name"];
            string viewId = (string)floorPlan["id"];

            var points = actualPoints
                .Where(p => (string)p["location"]["floorPlanId"] == viewId)
                .ToList();

            Console.WriteLine($"📌 Actual - FloorPlan: {viewName} (ID: {viewId}) → {points.Count} points");
            actualLists.Add(points);
        }

        // Compare lists in parallel (expected[i] vs actual[i])
        double tolerance = 1e-9;
        int pairCount = Math.Min(expectedLists.Count, actualLists.Count);

        for (int i = 0; i < pairCount; i++)
        {
            var expectedList = expectedLists[i];
            var actualList = actualLists[i];

            Console.WriteLine($"🔍 Comparing list #{i + 1}: expected = {expectedList.Count}, actual = {actualList.Count}");

            foreach (var expectedPoint in expectedList)
            {
                var expectedCoord = expectedPoint["location"]["coord"];
                double expectedX = (double)expectedCoord["x"];
                double expectedY = (double)expectedCoord["y"];

                var match = actualList.FirstOrDefault(actualPoint =>
                {
                    var actualCoord = actualPoint["location"]["coord"];
                    double actualX = (double)actualCoord["x"];
                    double actualY = (double)actualCoord["y"];

                    return Math.Abs(actualX - expectedX) < tolerance &&
                           Math.Abs(actualY - expectedY) < tolerance;
                });

                if (match != null)
                {
                    actualList.Remove(match); // Remove match to avoid duplicates
                }
                else
                {
                    Console.WriteLine($"❌ No match found for expected point: ({expectedX}, {expectedY}) in list #{i + 1}");
                }
            }

            Console.WriteLine($"✅ Finished list #{i + 1}: remaining unmatched actual points = {actualList.Count}");
        }

        // Final validation
        int totalRemaining = actualLists.Sum(list => list.Count);

        if (totalRemaining == 0)
        {
            Console.WriteLine("✅ All expected points were matched successfully. No duplicates remaining.");
        }
        else
        {
            Console.WriteLine($"⚠️ Validation failed: {totalRemaining} actual points were not matched.");
        }
    }

    private static bool IsValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}
