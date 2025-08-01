using Newtonsoft.Json;
using Create.ExportClasses;
using System.Reflection;
using System.Text.RegularExpressions;

using Create.ExportClasses;

public static class SegmentsListCreatorTester
{
    public static void RunTest(string model)
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        var wallPointsList = new List<string>();
        var wallSegmentsList = new List<string>();

        var wallPointObjectsByFloor = new Dictionary<string, List<WallPoint>>();

        string infoPath = $"./TestFiles/Integration/Models/{model}"; 
        string infoJson = File.ReadAllText(infoPath);
        ModelInfo modelInfo = JsonConvert.DeserializeObject<ModelInfo>(infoJson);

        string wallsPath = Path.Combine("./TestFiles/Integration/Walls", modelInfo.walls + ".json");

        if (!File.Exists(wallsPath))
        {
            Console.WriteLine($"❌ File not found: {wallsPath}");
            return;
        }

        string jsonPath = File.ReadAllText(wallsPath);
        Dictionary<string, ModelData> modelSegments = JsonConvert.DeserializeObject<Dictionary<string, ModelData>>(jsonPath);

        foreach (var viewEntry in modelInfo.cropRegions)
        {
            string viewName = viewEntry.viewName.Replace(" ", "_");

            if (!modelSegments.ContainsKey(viewName))
            {
                Console.WriteLine($"⚠️ View '{viewName}' not found in modelSegments.");
                continue;
            }

            double minX = viewEntry.min.x;
            double maxX = viewEntry.max.x;
            double minY = viewEntry.min.y;
            double maxY = viewEntry.max.y;
            int imageWidth = viewEntry.width;
            int imageHeight = viewEntry.height;

            Func<double, double> convertX = (x) => (x - minX) / (maxX - minX) * imageWidth;
            Func<double, double> convertY = (y) => (maxY - y) / (maxY - minY) * imageHeight;

            if (!wallPointObjectsByFloor.ContainsKey(viewEntry.viewNameID))
            {
                wallPointObjectsByFloor[viewEntry.viewNameID] = new List<WallPoint>();
            }

            var wallPointObjects = wallPointObjectsByFloor[viewEntry.viewNameID];

            SegmentsListCreator.FillSegmentsList(modelSegments[viewName].walls, Guid.NewGuid().ToString(), convertX, convertY,
            tempPath, wallPointsList, wallSegmentsList, wallPointObjects);
            
        }

        // Check wallPointObjectsByFloor Content
        // foreach (var floorEntry in wallPointObjectsByFloor)
        // {
        //     string floorId = floorEntry.Key;
        //     List<WallPoint> wallPoints = floorEntry.Value;

        //     Console.WriteLine($"Floor ID: {floorId}");

        //     foreach (var point in wallPoints)
        //     {
        //         Console.WriteLine($"  WallPoint ID: {point.Id}");
        //         Console.WriteLine($"    Location: X={point.X}, Y={point.Y}");
        //     }
        // }

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
            Console.WriteLine("🔎 Example:\n" + wallPointsList.FirstOrDefault(entry => !regexPoints.IsMatch(entry)));
        }

        var regexSegments = new Regex(@"
            ""wallPoints""\s*:\s*\[
                \s*""[0-9a-fA-F\-]+""\s*,\s*  
                ""[0-9a-fA-F\-]+""\s*        
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
            Console.WriteLine("🔎 Example:\n" + wallSegmentsList.FirstOrDefault(entry => !regexSegments.IsMatch(entry)));
        }

    }
}