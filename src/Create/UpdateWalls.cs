using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Create
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class UpdateWalls : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            string assemblyFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string wallDataPath = Path.Combine(assemblyFolder, "build_files", "build_tools", "wall_data.json");

            if (!File.Exists(wallDataPath))
            {
                TaskDialog.Show("Update Walls", "The file 'wall_data.json' was not found.");
                return Result.Failed;
            }

            // Read wall_data.json
            string jsonContent = File.ReadAllText(wallDataPath);
            var rootList = JsonConvert.DeserializeObject<List<Dictionary<string, List<JObject>>>>(jsonContent);

            // Retrieve types from Revit
            var wallTypes = GetWallTypes(doc);
            var doorTypes = GetSymbolTypes(doc, BuiltInCategory.OST_Doors);
            var windowTypes = GetSymbolTypes(doc, BuiltInCategory.OST_Windows);

            var updated = false;
            var messages = new List<string>();

            // Process categories
            updated |= ProcessCategory("walls", wallTypes, rootList, messages);
            updated |= ProcessCategory("Doors", doorTypes, rootList, messages);
            updated |= ProcessCategory("Windows", windowTypes, rootList, messages);

            // Save if changes were made
            if (updated)
            {
                File.WriteAllText(wallDataPath, JsonConvert.SerializeObject(rootList, Formatting.Indented));
                TaskDialog.Show("Update Walls", $"New types added:\n\n{string.Join("\n", messages)}");
            }
            else
            {
                TaskDialog.Show("Update Walls", "No new wall, door, or window types were found.");
            }

            return Result.Succeeded;
        }

        // Retrieve wall type names
        private List<string> GetWallTypes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Select(wt => wt.Name) 
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Retrieve door or window names
        private List<string> GetSymbolTypes(Document doc, BuiltInCategory category)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(category)
                .Cast<FamilySymbol>()
                .Select(fs => fs.Name) 
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private bool ProcessCategory(string category, List<string> revitTypes, List<Dictionary<string, List<JObject>>> rootList, List<string> messages)
        {
            var categoryDict = rootList.FirstOrDefault(d => d.ContainsKey(category));
            if (categoryDict == null)
                return false;

            var existingItems = categoryDict[category];
            var existingRevitNames = new HashSet<string>(existingItems.Select(i => i["Revit"]?.ToString()), StringComparer.OrdinalIgnoreCase);

            var newTypes = revitTypes.Where(t => !existingRevitNames.Contains(t)).ToList();

            if (!newTypes.Any())
                return false;

            foreach (var type in newTypes)
            {
                existingItems.Add(new JObject
                {
                    ["Revit"] = type,
                    ["Ekahau"] = "Unknown",
                    ["Attenuation"] = 0
                });
            }

            messages.Add($"{category}: {newTypes.Count} new type(s) added");
            return true;
        }
    }
}


