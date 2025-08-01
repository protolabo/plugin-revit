using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Create.ExportClasses;

class UnitTests
{
    public static void RunTests()
    {
        string modelsFolder = "./TestFiles/UnitTests/WallList";

        var selectedModels = new HashSet<string>
        {
            "horizontalWalls.txt",
            "verticalWalls.txt",
            "inclinedWalls.txt"
        };

        // WallSplitterUnit TEST
        Console.WriteLine("\n🧪 Running WallSplitterUnit test...");

        foreach (string file in selectedModels)
        {
            string path = Path.Combine(modelsFolder, file);
            if (!File.Exists(path))
            {
                Console.WriteLine($"⚠️ File not found: {file}");
                continue;
            }

            Console.WriteLine($"\n- Running test for: {file}");

            string[] lines = File.ReadAllLines(path)
                     .Where(line =>
                         !string.IsNullOrWhiteSpace(line) &&
                         !line.TrimStart().StartsWith("//"))
                     .ToArray();

            var tests = ShorthandParser.LoadShorthandTests(lines);
            WallSplitterUnitTester.RunTestFromModels(tests);
        }

        string filePath = "./TestFiles/UnitTests/ImageDatas";

        /// GetMetersPerUnit TEST
        Console.WriteLine("\n🧪 Running GetMetersPerUnit test...");
        GetMetersPerUnitTester.RunTests(filePath);
        
        // Getters TEST
        Console.WriteLine("\n🧪 Running Getters test...");
        GettersTester.RunTest();

    }
}



