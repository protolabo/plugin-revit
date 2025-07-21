using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace Create.ExportClasses
{
    internal class Getters
    {
        // These functions return the corresponding ID for each wall type from the Ekahau wallTypes JSON file.
        public static string GetWallId(string wall, string wallTypesJson)
        {
            try
            {
                // Get the corresponding Ekahau wall type for the choosen Revit wall from wall_data.json
                string dataFilePath = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "wall_data.json"
                );

                string wallDataJson = File.ReadAllText(dataFilePath);
                // parse the JSON directly with JObject
                var wallDataArray = Newtonsoft.Json.Linq.JArray.Parse(wallDataJson);
                string ekahauName = "Unknown"; // default valkue

                foreach (var item in wallDataArray)
                {
                    if ((string)item["Revit"] == wall)
                    {
                        ekahauName = (string)item["Ekahau"];
                        break;
                    }
                }

                // search for id in wallTypesJson
                var match = Regex.Match(
                    wallTypesJson,
                    $@"""name""\s*:\s*""{Regex.Escape(ekahauName)}"".*?""id""\s*:\s*""([^""]+)""",
                    RegexOptions.Singleline
                );

                return match.Groups[1].Value;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error in GetWallId: {ex.Message}");
                return "";
            }
        }


        public static string GetWindowId(string wallTypesJson)
        {
            return Regex.Match(wallTypesJson, @"""name""\s*:\s*""Window, Interior"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
        }

        public static string GetDoorId(string wallTypesJson)
        {
            return Regex.Match(wallTypesJson, @"""name""\s*:\s*""Door, Interior Office"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
        }

        public static string GetAreaId(string requirementsJson)
        {
            return Regex.Match(requirementsJson, @"""name""\s*:\s*""Ekahau Best Practices"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
        }
    }
}
