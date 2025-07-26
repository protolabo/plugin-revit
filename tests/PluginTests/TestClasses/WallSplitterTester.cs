using Newtonsoft.Json;
using Create.ExportClasses;
using System.Reflection;

using Create.ExportClasses;

public static class WallSplitterTester
{
    public static void RunTest(string model)
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        // Load test configuration
        string testConfigPath = $"./TestFiles/Models/{model}";
        string configJson = File.ReadAllText(testConfigPath);
        ModelInfo testInfo = JsonConvert.DeserializeObject<ModelInfo>(configJson);

        // Dynamically construct paths from config
        string inputPath = $"./TestFiles/Walls/{testInfo.walls}.json";
        string expectedPath = $"./TestFiles/Walls/{testInfo.walls_expected}.json";
        string outputPath = Path.Combine(tempFolderPath, "walls_segments.json");

        try
        {
            // Delete existing output
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            // Read input
            string json = File.ReadAllText(inputPath);
            var modelData = JsonConvert.DeserializeObject<Dictionary<string, ModelData>>(json);
            var modelDataSegments = new Dictionary<string, ModelData>();

            // Process
            WallSplitter.SplitWallByOpening(modelData, modelDataSegments);

            // Save output
            string outputJson = JsonConvert.SerializeObject(modelDataSegments, Formatting.Indented);
            File.WriteAllText(outputPath, outputJson);

            Console.WriteLine("✅ Segmentation completed.");

            // Read expected
            if (File.Exists(expectedPath))
            {
                string expectedJson = File.ReadAllText(expectedPath);

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
    }

}
