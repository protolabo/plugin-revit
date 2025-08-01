using Create.ExportClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

class GettersTester
{
    public static void RunTest()
    {
        string testFilePath = "./TestFiles/UnitTests/WallDatas/tests.json";

        // Get the path to wallTypes.json inside build_files/tempFolder/Template
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        // Load wallTypes.json content once
        string wallTypesJson = File.ReadAllText(Path.Combine(tempPath, "wallTypes.json"));

        var testJson = JObject.Parse(File.ReadAllText(testFilePath));
        var testsArray = testJson["tests"] as JArray;
        if (testsArray == null || testsArray.Count == 0)
        {
            Console.WriteLine("[ERROR] No tests found in tests.json.");
            return;
        }

        int testIndex = 0;
        foreach (var testEntry in testsArray)
        {
            testIndex++;
            Console.WriteLine($"Running test #{testIndex}");

            string wallDataFileName = testEntry["wallData"]?.ToString();
            if (string.IsNullOrWhiteSpace(wallDataFileName))
            {
                Console.WriteLine("[ERROR] 'wallData' filename not found in test input.");
                continue; // go to next test
            }

            var wallDataJson = JArray.Parse(File.ReadAllText($"./TestFiles/UnitTests/WallDatas/{wallDataFileName}.json"));
            var ekahauMap = BuildEkahauMap(wallDataJson);

            var groupedByEkahau = new Dictionary<string, List<(string Name, string Id)>>();
            var testIdLookup = new Dictionary<string, string>(); // name => id

            foreach (var category in new[] { "walls", "Doors", "Windows" })
            {
                var items = testEntry[category];
                if (items == null) continue;

                foreach (var item in items)
                {
                    string name = item.ToString();
                    bool hasEkahau = ekahauMap.TryGetValue((category, name), out string ekahauType);
                    if (!hasEkahau)
                    {
                        ekahauType = "Unknown";
                        // Console.WriteLine($"[WARNING] No Ekahau type found for {category} '{name}'. Defaulting to 'Unknown'");
                    }

                    string id = category switch
                    {
                        "walls" => Getters.GetWallId(name, wallTypesJson),
                        "Doors" => Getters.GetDoorId(name, wallTypesJson),
                        "Windows" => Getters.GetWindowId(name, wallTypesJson),
                        _ => "UNKNOWN"
                    };

                    // Console.WriteLine($"[TEST] Category: {category}, Name: {name}, Ekahau: {ekahauType}, ID: {id}");

                    if (!groupedByEkahau.ContainsKey(ekahauType))
                        groupedByEkahau[ekahauType] = new List<(string, string)>();

                    groupedByEkahau[ekahauType].Add((name, id));
                    testIdLookup[name] = id;
                }
            }

            bool allIdsMatch = true;
            foreach (var kvp in groupedByEkahau)
            {
                // Console.WriteLine($"Ekahau type: {kvp.Key}");
                foreach (var (name, id) in kvp.Value)
                {
                    // Console.WriteLine($"  - {name} (ID: {id})");

                    if (!testIdLookup.TryGetValue(name, out string expectedId))
                    {
                        Console.WriteLine($"❌ Error: Missing expected ID for '{name}' in testIdLookup.");
                        allIdsMatch = false;
                    }
                    else if (id != expectedId)
                    {
                        Console.WriteLine($"❌ Mismatch: {name} → Obtained ID = {id}, Expected ID = {expectedId}");
                        allIdsMatch = false;
                    }
                    else
                    {
                        Console.WriteLine($"- {name} → Obtained ID = {id}, Expected ID = {expectedId}");
                    }
                }
            }

            if (allIdsMatch)
            {
                Console.WriteLine("✅ All IDs match their expected values.");
            }
            else
            {
                Console.WriteLine("❌ One or more IDs did not match the expected values.");
            }
        }
    }

    static Dictionary<(string category, string revit), string> BuildEkahauMap(JArray wallDataJson)
    {
        var map = new Dictionary<(string, string), string>();

        foreach (var section in wallDataJson)
        {
            foreach (var category in section.Children<JProperty>())
            {
                string categoryName = category.Name;

                foreach (var entry in category.Value)
                {
                    string revit = entry["Revit"]?.ToString();
                    string ekahau = entry["Ekahau"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(revit) && !string.IsNullOrWhiteSpace(ekahau))
                    {
                        map[(categoryName, revit)] = ekahau;
                    }
                }
            }
        }

        return map;
    }

}

