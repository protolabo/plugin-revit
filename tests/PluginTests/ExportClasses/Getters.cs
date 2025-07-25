using System;
using System.IO;
using System.Text.RegularExpressions;
// using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;

namespace Create.ExportClasses
{
    internal class Getters
    {
        private static string GetEkahauNameFromWallData(string revitName, string categoryFilter = null)
        {
            string dataFilePath = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "wall_data.json"
            );

            string wallDataJson = File.ReadAllText(dataFilePath);
            var wallDataArray = JArray.Parse(wallDataJson);

            foreach (var entry in wallDataArray.Children<JObject>())
            {
                foreach (var category in new[] { "walls", "Doors", "Windows" })
                {
                    if (categoryFilter != null && category != categoryFilter) continue;

                    if (entry.TryGetValue(category, out var elements) && elements is JArray elementArray)
                    {
                        foreach (var item in elementArray)
                        {
                            if ((string)item["Revit"] == revitName)
                                return (string)item["Ekahau"];
                        }
                    }
                }
            }

            return "Unknown";
        }

        public static string GetWallId(string revitWallName, string wallTypesJson)
        {
            try
            {
                string ekahauName = GetEkahauNameFromWallData(revitWallName, "walls");

                var match = Regex.Match(
                    wallTypesJson,
                    $@"""name""\s*:\s*""{Regex.Escape(ekahauName)}"".*?""id""\s*:\s*""([^""]+)""",
                    RegexOptions.Singleline
                );

                return match.Success ? match.Groups[1].Value : "";
            }
            catch (Exception ex)
            {
                // TaskDialog.Show("Error", $"Error in GetWallId: {ex.Message}");
                return "";
            }
        }

        public static string GetWindowId(string revitName, string wallTypesJson)
        {
            try
            {
                string ekahauName = GetEkahauNameFromWallData(revitName, "Windows");

                var match = Regex.Match(
                    wallTypesJson,
                    $@"""name""\s*:\s*""{Regex.Escape(ekahauName)}"".*?""id""\s*:\s*""([^""]+)""",
                    RegexOptions.Singleline
                );

                return match.Success ? match.Groups[1].Value : "";
            }
            catch (Exception ex)
            {
                // TaskDialog.Show("Error", $"Error in GetWindowId: {ex.Message}");
                return "";
            }
        }

        public static string GetDoorId(string revitName, string wallTypesJson)
        {
            try
            {
                string ekahauName = GetEkahauNameFromWallData(revitName, "Doors");

                var match = Regex.Match(
                    wallTypesJson,
                    $@"""name""\s*:\s*""{Regex.Escape(ekahauName)}"".*?""id""\s*:\s*""([^""]+)""",
                    RegexOptions.Singleline
                );

                return match.Success ? match.Groups[1].Value : "";
            }
            catch (Exception ex)
            {
                // TaskDialog.Show("Error", $"Error in GetDoorId: {ex.Message}");
                return "";
            }
        }

    }
}

