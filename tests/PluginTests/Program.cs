using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Create.ExportClasses;

class Program
{
    static void Main(string[] args)
    {
        string inputPath = @"tests/PluginTests/test1/model_data.json";
        string outputPath = @"tests/PluginTests/test1/model_data_segments.json";
        string expectedPath = @"tests/PluginTests/test1/expected_model_data_segments.json";

        try
        {
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
    }
}

