using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Create.ExportClasses.SubClasses
{
    internal class Areas
    {
        public static void ProcessAreas(Document doc, string viewName, string floorPlanId, string requirementId, Func<double, double> convertX, Func<double, double> convertY, List<string> areasList)
        {
            View view = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .FirstOrDefault(v => v.Name == viewName.Replace("_", " "));

            if (view == null) return;

            var rooms = new FilteredElementCollector(doc, view.Id)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<SpatialElement>();

            int roomIndex = 1;
            SpatialElementBoundaryOptions boundaryOptions = new SpatialElementBoundaryOptions();

            foreach (SpatialElement room in rooms)
            {
                var boundaryLoops = room.GetBoundarySegments(boundaryOptions);
                if (boundaryLoops == null || boundaryLoops.Count == 0) continue;

                List<List<XYZ>> loops = boundaryLoops
                    .Select(segmentList => segmentList.Select(seg => seg.GetCurve().GetEndPoint(0)).ToList())
                    .ToList();

                // Compute the area of each loop
                List<(List<XYZ> points, double area)> loopsWithArea = loops
                    .Select(points => (points, ComputePolygonArea(points)))
                    .ToList();

                // Select the loop with the largest area
                var largestLoop = loopsWithArea.OrderByDescending(pair => pair.area).FirstOrDefault();
                if (largestLoop.points == null || largestLoop.points.Count < 3) continue;

                var imagePoints = largestLoop.points.Select(p => new
                {
                    x = convertX(p.X),
                    y = convertY(p.Y)
                }).ToList();

                var areaObject = new
                {
                    floorPlanId = floorPlanId,
                    name = $"Area-{roomIndex++}",
                    noteIds = new List<string>(),
                    requirementId = requirementId,
                    capacityItems = new List<string>(),
                    color = "#2c3e50",
                    area = imagePoints,
                    id = Guid.NewGuid().ToString(),
                    status = "CREATED"
                };

                string areaJson = JsonConvert.SerializeObject(areaObject, Formatting.Indented);
                areasList.Add(areaJson);
            }
        }

        // Shoelace formula to compute polygon area
        private static double ComputePolygonArea(List<XYZ> points)
        {
            if (points == null || points.Count < 3) return 0;
            double area = 0;
            int n = points.Count;
            for (int i = 0; i < n; i++)
            {
                XYZ p1 = points[i];
                XYZ p2 = points[(i + 1) % n];
                area += (p1.X * p2.Y) - (p2.X * p1.Y);
            }
            return Math.Abs(area) * 0.5;
        }
    }
}





