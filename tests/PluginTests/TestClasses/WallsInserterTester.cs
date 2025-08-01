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

        // Step 1: Read configuration file
        string configPath = $"./TestFiles/Integration/Models/{model}";
        JObject config = JObject.Parse(File.ReadAllText(configPath));

        // Step 2: Get walls filename from config
        string wallsFileName = (string)config["walls"];
        string wallsFilePath = Path.Combine("./TestFiles/Integration/Walls", wallsFileName + ".json");

        // Step 3: Read walls JSON content
        if (!File.Exists(wallsFilePath))
        {
            Console.WriteLine($"❌ Walls file not found: {wallsFilePath}");
            return; 
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

        WallPointsTester.RunTest(model, config);
        WallSegmentsTester.RunTest(model, config);

    }

    
}

