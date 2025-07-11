using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Autodesk.Revit.UI;
using System.Linq;

namespace Create.ExportClasses
{
    internal class AttenuationUpdater
    {
        public static Result UpdateEkahauValues()
        {
            try
            {
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // wall_data.json
                string wallDataPath = Path.Combine(assemblyFolder, "wall_data.json");
                if (!File.Exists(wallDataPath))
                    return Result.Failed;

                var wallDataJson = File.ReadAllText(wallDataPath);
                var wallDataArray = JArray.Parse(wallDataJson);

                // wallTypes.json
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

                // process each wall in wall_data
                foreach (var wall in wallDataArray)
                {
                    string ekahauName = (string)wall["Ekahau"];
                    double structuralValue = (double)wall["Structural"];

                    var wallType = wallTypesArray.FirstOrDefault(wt => (string)wt["name"] == ekahauName);
                    if (wallType == null) continue;

                    double thickness = (double)wallType["thickness"];
                    if (thickness == 0) continue;

                    int newAttenuationFactor = (int)Math.Round(structuralValue / thickness);

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

