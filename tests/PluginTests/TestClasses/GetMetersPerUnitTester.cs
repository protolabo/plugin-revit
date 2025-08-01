using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Create.ExportClasses;

public static class GetMetersPerUnitTester
{
    public static void RunTests(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"❌ Folder not found: {folderPath}");
            return;
        }

        var jsonFiles = Directory.GetFiles(folderPath, "*.json");

        if (jsonFiles.Length == 0)
        {
            Console.WriteLine($"⚠️ No JSON test files found in: {folderPath}");
            return;
        }

        foreach (var file in jsonFiles)
        {
            Console.WriteLine($"📂 Running tests from: {Path.GetFileName(file)}");

            try
            {
                string json = File.ReadAllText(file);
                var tests = JsonConvert.DeserializeObject<List<TestCase>>(json);

                foreach (var testCase in tests)
                {
                    var viewInfo = new List<ViewData> { testCase.test };

                    // Simulated image name
                    string imageName = $"exported_view - Floor Plan - {testCase.test.viewName}.bmp";

                    double actual = ImageJsonFileCreator.GetMetersPerUnit(imageName, viewInfo);
                    double expected = testCase.result;

                    bool passed = Math.Abs(actual - expected) < 0.001;

                    Console.WriteLine($"{(passed ? "✅" : "❌")} {testCase.test.viewName}: expected {expected:F4}, got {actual:F4}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing file '{file}': {ex.Message}");
            }
        }
    }

}
