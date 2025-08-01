﻿using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    internal class EkahauBuildingCreator
    {
        public static Result CreateEkahauBuildingFile(string destDir)
        {
            // Creates the single building that will contain all the building floors, one per view.
            string buildingId = Guid.NewGuid().ToString();

            JObject buildingEntry = new JObject
            {
                ["name"] = "Building 1",
                ["tags"] = new JArray(),
                ["id"] = buildingId,
                ["status"] = "CREATED"
            };

            JObject buildingsJson = new JObject
            {
                ["buildings"] = new JArray { buildingEntry }
            };

            string buildingsJsonPath = Path.Combine(destDir, "buildings.json");
            File.WriteAllText(buildingsJsonPath, buildingsJson.ToString(Newtonsoft.Json.Formatting.Indented));

            return Result.Succeeded;
        }
    }
}
