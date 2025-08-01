using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Create.ExportClasses;

class WallSplitterUnitTester
{
    public static void RunTestFromModels(List<(Dictionary<string, ModelData> input, Dictionary<string, ModelData> expected)> testCases)
    {
        int testIndex = 1;

        foreach (var (inputData, expectedData) in testCases)
{
    // Console.WriteLine($"🔍 Running test {testIndex}...");

    var resultData = new Dictionary<string, ModelData>();
    WallSplitter.SplitWallByOpening(inputData, resultData);

    NormalizeIds(resultData);

    bool isMatch = CompareModelData(resultData, expectedData);
    if (isMatch)
    {
        Console.WriteLine($"✅ Test {testIndex} passed");
    }
    else
    {
        string expectedShorthand = ShorthandParser.ToShorthand(expectedData);
        string resultShorthand = ShorthandParser.ToShorthand(resultData);

        Console.WriteLine($"❌ Test {testIndex} failed:");
        Console.WriteLine($"Expected shorthand:\n{expectedShorthand}");
        Console.WriteLine($"Got shorthand:\n{resultShorthand}");
    }

    // Print full JSON of resultData
    // string fullResultJson = JsonConvert.SerializeObject(resultData, Formatting.Indented);
    // Console.WriteLine($"Full result JSON for test {testIndex}:\n{fullResultJson}");

    testIndex++;
}

    }

    static void NormalizeIds(Dictionary<string, ModelData> data)
    {
        foreach (var model in data.Values)
        {
            foreach (var wall in model.walls)
            {
                wall.id = 0;
                foreach (var opening in wall.openings)
                {
                    opening.id = 0;
                }
            }
        }
    }

    static bool CompareModelData(Dictionary<string, ModelData> actual, Dictionary<string, ModelData> expected)
    {
        string actualJson = NormalizeModelData(actual);
        string expectedJson = NormalizeModelData(expected);

        return actualJson == expectedJson;
    }

    static string NormalizeModelData(Dictionary<string, ModelData> data)
    {
        var normalized = new Dictionary<string, object>();

        foreach (var kvp in data)
        {
            var viewName = kvp.Key;
            var model = kvp.Value;

            var simplifiedWalls = model.walls.Select(w => new
            {
                type = w.type,
                id = w.id,
                name = w.name,
                points = new[] {
                    new[] { w.start.x, w.start.y, w.start.z },
                    new[] { w.end.x, w.end.y, w.end.z }
                },
                openings = w.openings?.Select(o => new
                {
                    type = o.type,
                    id = o.id,
                    points = new[] {
                        new[] { o.start_point.x, o.start_point.y, o.start_point.z },
                        new[] { o.end_point.x, o.end_point.y, o.end_point.z }
                    }
                }).ToList()
            }).ToList();

            normalized[viewName] = new { walls = simplifiedWalls };
        }

        return JsonConvert.SerializeObject(normalized, Formatting.Indented);
    }
}
