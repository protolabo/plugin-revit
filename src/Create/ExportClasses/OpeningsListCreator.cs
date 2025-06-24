using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    internal class OpeningsListCreator
    {
        public static void FillOpeningsList(
            JToken elementsJson,
            string floorPlanId,
            Func<double, double> convertX,
            Func<double, double> convertY,
            string windowTypeId,
            string doorTypeId,
            List<string> wallPointsList,
            List<string> wallSegmentsList)
        {
            var wallsWithOpenings = elementsJson["walls"]
                .Where(w => w["openings"] is JArray arr && arr.Count > 0)
                .ToList();

            foreach (var wall in wallsWithOpenings)
            {
                var wallStart = wall["start"];
                var wallEnd = wall["end"];
                if (wallStart == null || wallEnd == null) continue;

                double wallX1 = convertX((double)wallStart["x"]);
                double wallY1 = convertY((double)wallStart["y"]);
                double wallX2 = convertX((double)wallEnd["x"]);
                double wallY2 = convertY((double)wallEnd["y"]);

                foreach (var opening in wall["openings"])
                {
                    var sp = opening["start_point"];
                    var ep = opening["end_point"];
                    if (sp == null || ep == null) continue;

                    // Units transformation.
                    double x1 = convertX((double)sp["x"]);
                    double y1 = convertY((double)sp["y"]);
                    double x2 = convertX((double)ep["x"]);
                    double y2 = convertY((double)ep["y"]);

                    // Identify if it is a door or window according to the 'type' field
                    string type = opening["type"]?.ToString();
                    string wallTypeToUse = null;

                    if (string.Equals(type, "Doors", StringComparison.OrdinalIgnoreCase))
                    {
                        wallTypeToUse = doorTypeId;
                    }
                    else if (string.Equals(type, "Windows", StringComparison.OrdinalIgnoreCase))
                    {
                        wallTypeToUse = windowTypeId;
                    }
                    else
                    {
                        // Ignore if any other type
                        continue;
                    }


                    // Creates an ID for each start and end point of a segment according to Ekahau's requirements.
                    string idStart = Guid.NewGuid().ToString();
                    string idEnd = Guid.NewGuid().ToString();

                    // PointAndSegment.MakeWallPoint creates a 'Point' or 'Segment' object 
                    // using the appropriate format for Ekahau JSON files.
                    wallPointsList.Add(PointAndSegment.MakeWallPoint(idStart, floorPlanId, x1, y1));
                    wallPointsList.Add(PointAndSegment.MakeWallPoint(idEnd, floorPlanId, x2, y2));
                    wallSegmentsList.Add(PointAndSegment.MakeWallSegment(idStart, idEnd, wallTypeToUse));
                }

            }
        }

    }
}

