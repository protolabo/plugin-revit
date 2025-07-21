using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Autodesk.Revit.UI;
using System.Linq;
using System.Collections.Generic;

namespace Create.ExportClasses
{
    internal class AttenuationUpdater
    {
        public static Result UpdateEkahauValues()
        {
            try
            {
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // read wall_data.json
                string wallDataPath = Path.Combine(assemblyFolder, "wall_data.json");
                if (!File.Exists(wallDataPath))
                    return Result.Failed;

                var wallDataJson = File.ReadAllText(wallDataPath);
                var wallDataArray = JArray.Parse(wallDataJson);

                // Combine all walls, doors and windows into one list of JObject
                var combinedWalls = new List<JObject>();

                foreach (var entry in wallDataArray.Children<JObject>())
                {
                    if (entry.ContainsKey("walls"))
                    {
                        var wallsArray = entry["walls"] as JArray;
                        if (wallsArray != null)
                            combinedWalls.AddRange(wallsArray.Children<JObject>());
                    }
                    if (entry.ContainsKey("Doors"))
                    {
                        var doorsArray = entry["Doors"] as JArray;
                        if (doorsArray != null)
                            combinedWalls.AddRange(doorsArray.Children<JObject>());
                    }
                    if (entry.ContainsKey("Windows"))
                    {
                        var windowsArray = entry["Windows"] as JArray;
                        if (windowsArray != null)
                            combinedWalls.AddRange(windowsArray.Children<JObject>());
                    }
                }

                // read wallTypes.json
                string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
                string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
                string tempPath = Path.Combine(tempFolderPath, "Template");
                string wallTypesPath = Path.Combine(tempPath, "wallTypes.json");
                if (!File.Exists(wallTypesPath))
                    return Result.Failed;

                var wallTypesJson = File.ReadAllText(wallTypesPath);

                // parse the root object
                var wallTypesObject = JObject.Parse(wallTypesJson);
                var wallTypesArray = (JArray)wallTypesObject["wallTypes"];

                // update values from combinedWalls to wallTypesArray
                foreach (var wall in combinedWalls)
                {
                    string ekahauName = (string)wall["Ekahau"];
                    if (string.IsNullOrEmpty(ekahauName))
                        continue;

                    double? attenuationValue = wall["Attenuation"]?.Value<double?>();
                    if (attenuationValue == null)
                        continue;

                    var wallType = wallTypesArray.FirstOrDefault(wt => (string)wt["name"] == ekahauName);
                    if (wallType == null) continue;

                    double thickness = (double)(wallType["thickness"] ?? 0.0);
                    if (thickness == 0) continue;

                    int newAttenuationFactor = (int)Math.Round(attenuationValue.Value / thickness);

                    // update propagationProperties
                    var propagationProperties = (JArray)wallType["propagationProperties"];
                    foreach (var prop in propagationProperties)
                    {
                        prop["attenuationFactor"] = newAttenuationFactor;

                        double totalAttenuation = Math.Round(newAttenuationFactor * thickness, 1);
                        prop["totalAttenuation"] = totalAttenuation;
                    }
                }

                // save the entire object (not just the array)
                File.WriteAllText(wallTypesPath, wallTypesObject.ToString(Formatting.Indented));

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error updating Ekahau values:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}



