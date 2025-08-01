﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    internal class WallListCreator
    {
        public static void FillWallList(
            Newtonsoft.Json.Linq.JToken elementsJson,
            string floorPlanId,
            Func<double, double> convertX,
            Func<double, double> convertY,
            string path,
            List<string> wallPointsList,
            List<string> wallSegmentsList)
        {
            var walls = elementsJson["walls"]
                .Where(w => w["openings"] is Newtonsoft.Json.Linq.JArray arr && arr.Count == 0)
                .ToList();

            var wallTypesJson = File.ReadAllText(Path.Combine(path, "wallTypes.json"));

            foreach (var wall in walls)
            {
                var start = wall["start"];
                var end = wall["end"];
                if (start == null || end == null) continue;

                double x1 = convertX((double)start["x"]);
                double y1 = convertY((double)start["y"]);
                double x2 = convertX((double)end["x"]);
                double y2 = convertY((double)end["y"]);

                string idStart = Guid.NewGuid().ToString();
                string idEnd = Guid.NewGuid().ToString();

                string wallName = (string)wall["name"];
                string wallConcreteId = Getters.GetWallId(wallName, wallTypesJson);

                wallPointsList.Add(PointAndSegment.MakeWallPoint(idStart, floorPlanId, x1, y1));
                wallPointsList.Add(PointAndSegment.MakeWallPoint(idEnd, floorPlanId, x2, y2));
                wallSegmentsList.Add(PointAndSegment.MakeWallSegment(idStart, idEnd, wallConcreteId));
            }
        }
    }
}
