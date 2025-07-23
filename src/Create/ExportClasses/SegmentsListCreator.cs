using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Create.ExportClasses
{
    internal class SegmentsListCreator
    {
        public static void FillSegmentsList(
            JToken elementsJson,
            string floorPlanId,
            Func<double, double> convertX,
            Func<double, double> convertY,
            string path,
            List<string> wallPointsList,
            List<string> wallSegmentsList)
        {
            // This is an auxiliary list where the start and end points of all processed walls will be stored.
            // Newly processed walls will search this list to find ends of other walls that are close enough to connect with.
            var wallPointObjects = new List<WallPoint>();

            var wallTypesJson = File.ReadAllText(Path.Combine(path, "wallTypes.json"));

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

                // If a wall has only one segment, it means it has no openings, in which case we can look directly
                // inside the points list for two points that are close enough to connect the start and end of the wall.
                if (openings.Count == 1)
                {
                    var opening = openings[0];
                    var sp = opening["start_point"];
                    var ep = opening["end_point"];
                    if (sp == null || ep == null) continue;

                    double x1 = convertX((double)sp["x"]);
                    double y1 = convertY((double)sp["y"]);
                    double x2 = convertX((double)ep["x"]);
                    double y2 = convertY((double)ep["y"]);

                    string type = opening["type"]?.ToString();
                    string segmentName = opening["name"]?.ToString();
                    string wallTypeToUse = null;

                    // Get corresponding ID for every wall
                    if (string.Equals(type, "Doors", StringComparison.OrdinalIgnoreCase))
                        wallTypeToUse = Getters.GetDoorId(segmentName, wallTypesJson);
                    else if (string.Equals(type, "Windows", StringComparison.OrdinalIgnoreCase))
                        wallTypeToUse = Getters.GetWindowId(segmentName, wallTypesJson);
                    else if (string.Equals(type, "Walls", StringComparison.OrdinalIgnoreCase))
                        wallTypeToUse = Getters.GetWallId(segmentName, wallTypesJson);
                    else
                        continue;

                    const double minDistance = 0.08;

                    // Search the list for a point that is close enough (less than 1 inch)
                    var nearbyStart = wallPointObjects.FirstOrDefault(p => CalculateDistance(p.X, p.Y, x1, y1) < minDistance);
                    string idStart;
                    if (nearbyStart != null)
                    {
                        idStart = nearbyStart.Id;
                    }
                    else
                    {
                        // If no nearby point is found, add the wall’s point to the Ekahau points list and to the auxiliary list.
                        idStart = Guid.NewGuid().ToString();
                        wallPointsList.Add(PointAndSegment.MakeWallPoint(idStart, floorPlanId, x1, y1));
                        wallPointObjects.Add(new WallPoint { Id = idStart, X = x1, Y = y1 });
                    }

                    // Search the list for a point that is close enough (less than 1 inch) to the end point.
                    var nearbyEnd = wallPointObjects.FirstOrDefault(p => CalculateDistance(p.X, p.Y, x2, y2) < minDistance);
                    string idEnd;
                    if (nearbyEnd != null)
                    {
                        idEnd = nearbyEnd.Id;
                    }
                    else
                    {
                        idEnd = Guid.NewGuid().ToString();
                        wallPointsList.Add(PointAndSegment.MakeWallPoint(idEnd, floorPlanId, x2, y2));
                        wallPointObjects.Add(new WallPoint { Id = idEnd, X = x2, Y = y2 });
                    }

                    // Create the segment using either the found nearby points or the wall’s extreme points.
                    wallSegmentsList.Add(PointAndSegment.MakeWallSegment(idStart, idEnd, wallTypeToUse));

                    continue;
                }

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
                    string segmentName = opening["name"]?.ToString();
                    string wallTypeToUse = null;

                    // Get the corresponding Ekahau wall for every choosen Revit wall.
                    if (string.Equals(type, "Doors", StringComparison.OrdinalIgnoreCase))
                        wallTypeToUse = Getters.GetDoorId(segmentName, wallTypesJson);
                    else if (string.Equals(type, "Windows", StringComparison.OrdinalIgnoreCase))
                        wallTypeToUse = Getters.GetWindowId(segmentName, wallTypesJson);
                    else if (string.Equals(type, "Walls", StringComparison.OrdinalIgnoreCase))
                        wallTypeToUse = Getters.GetWallId(segmentName, wallTypesJson);
                    else
                        continue;

                    // If the wall contains more than one segment, use the first segment as the wall’s endpoint.
                    if (prevEndX == null || prevEndY == null)
                    {
                        const double minDistance = 0.08;
                        string idStart;

                        // In this case, we only need the starting point of the first segment, which corresponds to the wall’s initial endpoint.
                        var nearbyPoint = wallPointObjects.FirstOrDefault(p => CalculateDistance(p.X, p.Y, x1, y1) < minDistance);

                        if (nearbyPoint != null)
                        {
                            idStart = nearbyPoint.Id;
                        }
                        else
                        {
                            idStart = Guid.NewGuid().ToString();
                            wallPointsList.Add(PointAndSegment.MakeWallPoint(idStart, floorPlanId, x1, y1));
                            wallPointObjects.Add(new WallPoint
                            {
                                Id = idStart,
                                X = x1,
                                Y = y1
                            });
                        }

                        // Always create the Final Point of first segment
                        string idEnd = Guid.NewGuid().ToString();
                        wallPointsList.Add(PointAndSegment.MakeWallPoint(idEnd, floorPlanId, x2, y2));
                        wallSegmentsList.Add(PointAndSegment.MakeWallSegment(idStart, idEnd, wallTypeToUse));
                        wallPointObjects.Add(new WallPoint
                        {
                            Id = idEnd,
                            X = x2,
                            Y = y2
                        });

                        prevEndX = x2;
                        prevEndY = y2;
                        prevEndId = idEnd;
                    }
                    // the rest of the openings
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

                // Process the wall’s final segment since the endpoint of that segment corresponds to the wall’s final endpoint.
                if (prevEndX != null && prevEndY != null && prevEndId != null)
                {
                    const double minDistance = 0.08;

                    // Search the auxiliary list for a point close to the endpoint of the last segment.
                    var nearbyPoint = wallPointObjects
                        .FirstOrDefault(p => CalculateDistance(p.X, p.Y, prevEndX.Value, prevEndY.Value) < minDistance);

                    // If a sufficiently close point is found, remove the last segment and create a new one instead,
                    // going from the starting point of the segment to be removed to the found point.
                    if (nearbyPoint != null && wallSegmentsList.Count > 0)
                    {
                        // get last segment
                        string lastSegmentJson = wallSegmentsList.Last();

                        try
                        {
                            // get values from last segment
                            JObject lastSegment = JObject.Parse(lastSegmentJson);
                            var wallPoints = lastSegment["wallPoints"] as JArray;
                            var startIdToken = wallPoints?[0];
                            var wallTypeIdToken = lastSegment["wallTypeId"];

                            if (startIdToken != null && wallTypeIdToken != null)
                            {
                                string startId = startIdToken.ToString();
                                string wallTypeId = wallTypeIdToken.ToString();

                                // Delete old last segment
                                wallSegmentsList.RemoveAt(wallSegmentsList.Count - 1);

                                // Create the segment that will replace it, connected to the found wall endpoint.
                                string newSegment = PointAndSegment.MakeWallSegment(startId, nearbyPoint.Id, wallTypeId);
                                wallSegmentsList.Add(newSegment);
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error processing the last segment: " + ex.Message);
                        }
                    }
                    else if (nearbyPoint == null)
                    {
                        // No nearby point found; add as a new point.
                        wallPointObjects.Add(new WallPoint
                        {
                            Id = prevEndId,
                            X = prevEndX.Value,
                            Y = prevEndY.Value
                        });
                    }
                }


            }
        }
        private static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private class WallPoint
        {
            public double X { get; set; }
            public double Y { get; set; }
            public string Id { get; set; }
        }
    }

}


