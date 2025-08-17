using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    internal class BuildingFloorCreator
    {
        public static Result CreateBuildingFloorsJsonFiles(string destDir)
        {
            string floorPlansPath = Path.Combine(destDir, "floorPlans.json");
            string buildingsPath = Path.Combine(destDir, "buildings.json");
            string floorTypesPath = Path.Combine(destDir, "floorTypes.json");
            string outputPath = Path.Combine(destDir, "buildingFloors.json");

            if (!File.Exists(floorPlansPath) || !File.Exists(buildingsPath) || !File.Exists(floorTypesPath))
            {
                throw new FileNotFoundException("One or more required files (floorPlans.json, buildings.json, floorTypes.json) are missing.");
            }

            // Load floorPlanIds from Ekahau json file floorPlans to match avery floor plan to the corresponding
            // building floor
            var floorPlansJson = JObject.Parse(File.ReadAllText(floorPlansPath));
            var floorPlans = floorPlansJson["floorPlans"] as JArray;
            var floorPlanIds = floorPlans.Select(fp => fp["id"]?.ToString()).Where(id => !string.IsNullOrEmpty(id)).ToList();

            // Get buildingId to macth every building floor with te corresponding building
            // (for this code there will be only one building)
            var buildingsJson = JObject.Parse(File.ReadAllText(buildingsPath));
            var buildingId = buildingsJson["buildings"]?[0]?["id"]?.ToString();
            if (string.IsNullOrEmpty(buildingId))
                throw new Exception("No buildingId found in buildings.json");

            // Get floorTypeId from floorTypes.json by searching for key = 'FloorTypes.OfficeUS'
            // by default this code will always use 'FloorTypes.OfficeUS'
            var floorTypesJson = JObject.Parse(File.ReadAllText(floorTypesPath));
            var floorTypes = floorTypesJson["floorTypes"] as JArray;
            var floorType = floorTypes?.FirstOrDefault(ft => ft["key"]?.ToString() == "FloorTypes.OfficeUS");
            var floorTypeId = floorType?["id"]?.ToString();
            if (string.IsNullOrEmpty(floorTypeId))
                throw new Exception("FloorType with key 'FloorTypes.OfficeUS' not found.");

            // Create one building floor for every view in Revit Model
            var buildingFloors = new JArray();
            for (int i = 0; i < floorPlanIds.Count; i++)
            {
                var entry = new JObject
                {
                    ["floorPlanId"] = floorPlanIds[i],
                    ["buildingId"] = buildingId,
                    ["floorTypeId"] = floorTypeId,
                    ["floorNumber"] = i,
                    ["height"] = 2.5,           // this value must be updated with actual value in Revit model!!!!!
                    ["thickness"] = 0.5,
                    ["id"] = Guid.NewGuid().ToString(),
                    ["status"] = "CREATED"
                };
                buildingFloors.Add(entry);
            }

            // Save file
            var finalObject = new JObject
            {
                ["buildingFloors"] = buildingFloors
            };
            File.WriteAllText(outputPath, finalObject.ToString(Newtonsoft.Json.Formatting.Indented));

            return Result.Succeeded;
        }
    }
}
