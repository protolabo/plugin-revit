﻿using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.Helpers.SubFunctions
{
    internal class WallElements
    {
        public static void ProcessWallElements(
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

                    double x1 = convertX((double)sp["x"]);
                    double y1 = convertY((double)sp["y"]);
                    double x2 = convertX((double)ep["x"]);
                    double y2 = convertY((double)ep["y"]);

                    // Identificar si es puerta o ventana según el campo "type"
                    string type = opening["type"]?.ToString().ToLower();
                    string wallTypeToUse = type == "doors" ? doorTypeId : windowTypeId;

                    string idStart = Guid.NewGuid().ToString();
                    string idEnd = Guid.NewGuid().ToString();

                    wallPointsList.Add(SubFunctions.PointAndSegment.MakeWallPoint(idStart, floorPlanId, x1, y1));
                    wallPointsList.Add(SubFunctions.PointAndSegment.MakeWallPoint(idEnd, floorPlanId, x2, y2));
                    wallSegmentsList.Add(SubFunctions.PointAndSegment.MakeWallSegment(idStart, idEnd, wallTypeToUse));
                }

            }
        }

    }
}

