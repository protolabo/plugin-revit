using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Create.ExportClasses
{
    internal class SegmentsListCreator
    {
        public static void FillOpeningsList(
            JToken elementsJson,
            string floorPlanId,
            Func<double, double> convertX,
            Func<double, double> convertY,
            string path,
            List<string> wallPointsList,
            List<string> wallSegmentsList)
        {
            // Obtains the list of all segments that make up the wall based on the list of openings.
            // The original list of openings includes the doors and windows embedded in the wall,
            // as well as the "voids" present in it. The wall containing the doors, windows,
            // and voids was previously divided into segments connecting those openings,
            // and those segments were added to the openings list. For these reasons,
            // the openings must be treated as wall segments.
            // The voids will be filtered out later to avoid drawing them in the Ekahau file.
            var wallsWithOpenings = elementsJson["walls"]
                .Where(w => w["openings"] is JArray arr && arr.Count > 0)
                .ToList();

            foreach (var wall in wallsWithOpenings)
            {
                var openings = wall["openings"].ToList();

                // State variable used to store the end point of the previous opening,
                // since the code determines whether two segments are close enough
                // (less than one inch apart) to be interconnected.
                double? prevEndX = null;
                double? prevEndY = null;
                string prevEndId = null;

                foreach (var opening in openings)
                {
                    var sp = opening["start_point"];
                    var ep = opening["end_point"];
                    if (sp == null || ep == null) continue;

                    // Transformation of units from Revit to Ekahau.
                    double x1 = convertX((double)sp["x"]);
                    double y1 = convertY((double)sp["y"]);
                    double x2 = convertX((double)ep["x"]);
                    double y2 = convertY((double)ep["y"]);

                    string type = opening["type"]?.ToString();
                    string wallTypeToUse = null;
                    var wallTypesJson = File.ReadAllText(Path.Combine(path, "wallTypes.json"));

                    // Get the corresponding Ekahau wall for every choosen Revit wall.
                    if (string.Equals(type, "Doors", StringComparison.OrdinalIgnoreCase))
                        wallTypeToUse = Getters.GetDoorId(wallTypesJson);
                    else if (string.Equals(type, "Windows", StringComparison.OrdinalIgnoreCase))
                        wallTypeToUse = Getters.GetWindowId(wallTypesJson);
                    else if (string.Equals(type, "Walls", StringComparison.OrdinalIgnoreCase)) { 
                        string wallName = (string)wall["name"];
                        wallTypeToUse = Getters.GetWallId(wallName, wallTypesJson); }
                    else
                        continue;

                    // If there is no previous point, this is the first segment
                    if (prevEndX == null || prevEndY == null)
                    {
                        string idStart = Guid.NewGuid().ToString();
                        string idEnd = Guid.NewGuid().ToString();

                        wallPointsList.Add(PointAndSegment.MakeWallPoint(idStart, floorPlanId, x1, y1));
                        wallPointsList.Add(PointAndSegment.MakeWallPoint(idEnd, floorPlanId, x2, y2));
                        wallSegmentsList.Add(PointAndSegment.MakeWallSegment(idStart, idEnd, wallTypeToUse));

                        prevEndX = x2;
                        prevEndY = y2;
                        prevEndId = idEnd;
                    }
                    else
                    {
                        // Calculate the distance between the previous end point and the current start point.
                        double dx = x1 - prevEndX.Value;
                        double dy = y1 - prevEndY.Value;
                        double dist = Math.Sqrt(dx * dx + dy * dy);

                        if (dist < 0.08)
                        {
                            // Connect directly from the previous end point to the new end point
                            string idEnd = Guid.NewGuid().ToString();
                            wallPointsList.Add(PointAndSegment.MakeWallPoint(idEnd, floorPlanId, x2, y2));
                            wallSegmentsList.Add(PointAndSegment.MakeWallSegment(prevEndId, idEnd, wallTypeToUse));

                            // Update final point
                            prevEndX = x2;
                            prevEndY = y2;
                            prevEndId = idEnd;
                        }
                        else
                        {
                            // If the segment is too far from the previous one (more than one inch),
                            // it will not be connected and will be treated as the starting point of another
                            // series of interconnected segments.
                            // This likely means it corresponds to a "void" in the wall.
                            string idStart = Guid.NewGuid().ToString();
                            string idEnd = Guid.NewGuid().ToString();

                            wallPointsList.Add(PointAndSegment.MakeWallPoint(idStart, floorPlanId, x1, y1));
                            wallPointsList.Add(PointAndSegment.MakeWallPoint(idEnd, floorPlanId, x2, y2));
                            wallSegmentsList.Add(PointAndSegment.MakeWallSegment(idStart, idEnd, wallTypeToUse));

                            prevEndX = x2;
                            prevEndY = y2;
                            prevEndId = idEnd;
                        }
                    }
                }
            }
        }
    }
}


