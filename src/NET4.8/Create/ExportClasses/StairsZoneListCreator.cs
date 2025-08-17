using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace Create.ExportClasses
{
    internal class StairsZoneListCreator
    {
        public static void FillStairsZoneList(Document doc, string viewName, string floorPlanId, Func<double, double> convertX, Func<double, double> convertY, List<string> areasList)
        {
            View view = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .FirstOrDefault(v => v.Name == viewName.Replace("_", " "));

            if (view == null) return;

            var stairs = new FilteredElementCollector(doc, view.Id)
                .OfCategory(BuiltInCategory.OST_Stairs)
                .WhereElementIsNotElementType()
                .ToElements();

            int stairIndex = 1;

            foreach (var stair in stairs)
            {
                BoundingBoxXYZ bbox = stair.get_BoundingBox(view);
                if (bbox == null) continue;

                XYZ min = bbox.Min;
                XYZ max = bbox.Max;

                // Define 4 base corners of the bounding box (XY plane only)
                List<XYZ> corners = new List<XYZ>
                {
                    new XYZ(min.X, min.Y, 0),
                    new XYZ(max.X, min.Y, 0),
                    new XYZ(max.X, max.Y, 0),
                    new XYZ(min.X, max.Y, 0)
                };

                // Convert to image coordinates
                var imagePoints = corners.Select(p => new
                {
                    x = convertX(p.X),
                    y = convertY(p.Y)
                }).ToList();

                var areaObject = new
                {
                    floorPlanId = floorPlanId,
                    name = $"Exclusion Area - Stair {stairIndex++}",
                    area = imagePoints,
                    id = Guid.NewGuid().ToString(),
                    status = "CREATED"
                };

                string areaJson = JsonConvert.SerializeObject(areaObject, Formatting.Indented);
                areasList.Add(areaJson);
            }
        }

    }
}
