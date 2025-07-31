using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Create.ExportClasses
{
    internal class WallSplitter
    {
        public static void SplitWallByOpening(Dictionary<string, ModelData> modelData, Dictionary<string, ModelData> modelDataSegments)
        {
            foreach (var kvp in modelData)
            {
                string viewName = kvp.Key;
                var model = kvp.Value;
                var originalWalls = model.walls;

                List<WallData> updatedWalls = new List<WallData>();

                foreach (var wall in originalWalls)
                {
                    if (wall.start == null || wall.end == null)
                    {
                        updatedWalls.Add(wall);
                        continue;
                    }

                    // Create new point list including wall endpoints and opening endpoints
                    List<Point> points = new List<Point> { wall.start, wall.end };
                    foreach (var opening in wall.openings ?? new List<OpeningData>())
                    {
                        if (opening.start_point != null) points.Add(opening.start_point);
                        if (opening.end_point != null) points.Add(opening.end_point);
                    }

                    // Merge points that are closer than threshold
                    double threshold = 0.08;
                    bool changed;
                    do
                    {
                        changed = false;
                        for (int i = 0; i < points.Count; i++)
                        {
                            for (int j = i + 1; j < points.Count; j++)
                            {
                                if (Distance(points[i], points[j]) < threshold)
                                {
                                    var mid = MidPoint(points[i], points[j]);
                                    points.RemoveAt(j);
                                    points.RemoveAt(i);
                                    points.Add(mid);
                                    changed = true;
                                    break;
                                }
                            }
                            if (changed) break;
                        }
                    } while (changed);

                    // Sort points according to wall orientation
                    bool isVertical = Math.Abs(wall.start.x - wall.end.x) < 1e-6;
                    points = isVertical
                        ? points.OrderBy(p => p.y).ToList()
                        : points.OrderBy(p => p.x).ToList();

                    //Console.WriteLine($"Points for wall ID {wall.id}:");
                    //foreach (var point in points)
                    //{
                    //    Console.WriteLine($"  Point(x: {point.x}, y: {point.y}, z: {point.z})");
                    //}

                    // Generate segments and classify them
                    List<OpeningData> segments = new List<OpeningData>();

                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var p1 = points[i];
                        var p2 = points[i + 1];
                        var mid = MidPoint(p1, p2);

                        var allOpenings = wall.openings ?? new List<OpeningData>();

                        // Get openings whose segment contains the midpoint
                        var matchingOpenings = allOpenings.Where(o =>
                            o.start_point != null && o.end_point != null &&
                            IsPointWithinSegment(mid, o.start_point, o.end_point, isVertical)).ToList();

                        if (matchingOpenings.Count >= 2)
                        {
                            // Check for mix of doors and windows
                            //var types = matchingOpenings.Select(o => o.type).ToHashSet();
                            int doorsQty = matchingOpenings.Count(o => o.type == "Doors");
                            int windowsQty = matchingOpenings.Count(o => o.type == "Windows");
                            bool hasMix = doorsQty + windowsQty >= 2;

                            if (hasMix)
                            {
                                // Sort the endpoints to split the segment
                                var (first, second) = IsFirstPointSmaller(p1, p2, isVertical) ? (p1, p2) : (p2, p1);
                                var cut = MidPoint(first, second);

                                // Get previous segment data
                                var previousSegment = segments.LastOrDefault();

                                if (previousSegment != null && (previousSegment.type == "Doors" || previousSegment.type == "Windows"))
                                {
                                    string previousType = previousSegment.type;
                                    int previousId = previousSegment.id;
                                    string previousName = previousSegment.name;
                                    Point previousPosition = previousSegment?.position;
                                    Point previousStart = previousSegment.start_point;
                                    Point previousEnd = previousSegment.end_point;
                                    double? previousWidth = previousSegment?.width_ft;
                                    double? previousHeight = previousSegment?.height_ft;


                                    // First subsegment — inherits type from previous segment
                                    segments.Add(new OpeningData
                                    {
                                        type = previousType,
                                        id = previousId,
                                        name = previousName,
                                        position = previousPosition,
                                        start_point = first,
                                        end_point = cut,
                                        width_ft = previousWidth,
                                        height_ft = previousHeight,

                                    });

                                    double tolerance = 0.001;
                                    bool allEndPointsClose = true;
                                    for (int j = 0; j < matchingOpenings.Count - 1; j++)
                                    {
                                        var point1 = matchingOpenings[j].end_point;
                                        var point2 = matchingOpenings[j + 1].end_point;
                                        if (Distance(point1, point2) > tolerance)
                                        {
                                            allEndPointsClose = false;
                                            break;
                                        }
                                    }

                                    var remainingOpenings = matchingOpenings;
                                    OpeningData chosen = null;
                                    if (!allEndPointsClose)
                                    {
                                        if (remainingOpenings.Count > 0)
                                        {
                                            double maxDistance = double.MinValue;
                                            foreach (var o in remainingOpenings)
                                            {
                                                double distanceToCut = Distance(o.end_point, cut);
                                                if (distanceToCut > maxDistance)
                                                {
                                                    maxDistance = distanceToCut;
                                                    chosen = o;
                                                }
                                            }
                                        }


                                    }
                                    else
                                    {
                                        if (remainingOpenings.Count > 0)
                                        {
                                            // Find the longest opening
                                            double maxLength = double.MinValue;
                                            foreach (var o in remainingOpenings)
                                            {
                                                double len = Distance(o.start_point, o.end_point);
                                                if (len > maxLength)
                                                {
                                                    maxLength = len;
                                                    chosen = o;
                                                }
                                            }
                                        }

                                    }
                                    

                                    // Second subsegment — inherits from the longest remaining opening or previous
                                    segments.Add(new OpeningData
                                    {
                                        type = chosen.type,
                                        id = chosen.id,
                                        name = chosen.name,
                                        position = chosen.position,
                                        start_point = cut,
                                        end_point = chosen.end_point,
                                        width_ft = chosen.width_ft,
                                        height_ft = chosen.height_ft,
                                    });

                                }
                                else
                                {
                                    OpeningData chosen = null;
                                    double maxLength = double.MinValue;
                                    foreach (var o in matchingOpenings)
                                    {
                                        double len = Distance(o.start_point, o.end_point);
                                        if (len > maxLength)
                                        {
                                            maxLength = len;
                                            chosen = o;
                                        }
                                    }

                                    // Second subsegment — inherits from the longest remaining opening or previous
                                    segments.Add(new OpeningData
                                    {
                                        type = chosen.type,
                                        id = chosen.id,
                                        name = chosen.name,
                                        position = chosen.position,
                                        start_point = chosen.start_point,
                                        end_point = chosen.end_point,
                                        width_ft = chosen.width_ft,
                                        height_ft = chosen.height_ft,
                                    });

                                }


                                continue;
                            }
                            else
                            {
                                OpeningData chosen = null;

                                foreach (var o in matchingOpenings)
                                {
                                    if (o.type == "Doors" || o.type == "Windows")
                                    {
                                        chosen = o;
                                        break;
                                    }
                                    else
                                    {
                                        chosen = o;
                                    }
                                }

                                segments.Add(new OpeningData
                                {
                                    type = chosen.type,
                                    id = chosen.id,
                                    name = chosen.name,
                                    position = chosen.position,
                                    start_point = chosen.start_point,
                                    end_point = chosen.end_point,
                                    width_ft = chosen.width_ft,
                                    height_ft = chosen.height_ft,
                                });
                                continue;
                            }
                        }

                        // Default behavior — either wall or a single opening match
                        var match = matchingOpenings.FirstOrDefault();
                        if (match != null)
                        {
                            // Opening segment
                            segments.Add(new OpeningData
                            {
                                type = match.type,
                                name = match.name,
                                id = match.id,
                                start_point = p1,
                                end_point = p2,
                                width_ft = match.width_ft,
                                height_ft = match.height_ft,
                                position = match.position
                            });
                        }
                        else
                        {
                            // Wall segment
                            segments.Add(new OpeningData
                            {
                                type = "Walls",
                                name = wall.name,
                                start_point = p1,
                                end_point = p2
                            });
                        }
                    }


                    // First merge: merge segments with same ID and type (except walls with id = 0)
                    List<OpeningData> mergedSegments = new List<OpeningData>();

                    if (segments.Count > 0)
                    {
                        OpeningData current = segments[0];

                        for (int i = 1; i < segments.Count; i++)
                        {
                            var next = segments[i];

                            // If consecutive segments share the same opening id and type (and id != 0)
                            if (current.id == next.id && current.type == next.type && current.id != 0)
                            {
                                // Extend current segment's end_point to next segment's end_point
                                current.end_point = next.end_point;
                            }
                            else
                            {
                                // Add current segment to merged list and move to next
                                mergedSegments.Add(current);
                                current = next;
                            }
                        }

                        // Add last segment
                        mergedSegments.Add(current);
                    }
                    else
                    {
                        mergedSegments = segments;
                    }

                    // Second pass: merge consecutive segments with type "Openings"
                    List<OpeningData> finalSegments = new List<OpeningData>();

                    if (mergedSegments.Count > 0)
                    {
                        OpeningData current = mergedSegments[0];

                        for (int i = 1; i < mergedSegments.Count; i++)
                        {
                            var next = mergedSegments[i];

                            // If both current and next are of type "Openings"
                            if (current.type == "Opening" && next.type == "Opening")
                            {
                                // Extend current to cover next segment
                                current.end_point = next.end_point;
                            }
                            else
                            {
                                // Add current to final list and move to next
                                finalSegments.Add(current);
                                current = next;
                            }
                        }

                        // Add last segment
                        finalSegments.Add(current);
                    }
                    else
                    {
                        finalSegments = mergedSegments;
                    }

                    // Create copy of original wall with updated openings list
                    var baseWall = new WallData
                    {
                        id = wall.id,
                        name = wall.name,
                        type = wall.type,
                        start = wall.start,
                        end = wall.end,
                        openings = finalSegments
                    };

                    updatedWalls.Add(baseWall);

                }

                // Sort segments within each wall
                foreach (var wall in updatedWalls)
                {
                    if (wall.start == null || wall.end == null || wall.openings == null)
                        continue;

                    string axis = Math.Abs(wall.start.x - wall.end.x) < 1e-6 ? "y" : "x";

                    foreach (var o in wall.openings)
                    {
                        if (o.start_point != null && o.end_point != null)
                        {
                            bool swap = axis == "x"
                                ? o.start_point.x > o.end_point.x
                                : o.start_point.y > o.end_point.y;

                            if (swap)
                            {
                                var temp = o.start_point;
                                o.start_point = o.end_point;
                                o.end_point = temp;
                            }
                        }
                    }

                    wall.openings = wall.openings
                        .OrderBy(o => axis == "x" ? o.start_point.x : o.start_point.y)
                        .ToList();
                }

                // Save updated walls to modelDataSegments dictionary
                modelDataSegments[viewName] = new ModelData
                {
                    walls = updatedWalls
                };
            }
        }

        // Auxiliary methods

        // Calculates Euclidean distance between two points
        static double Distance(Point a, Point b)
        {
            double dx = a.x - b.x;
            double dy = a.y - b.y;
            double dz = a.z - b.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // Calculates midpoint between two points
        static Point MidPoint(Point a, Point b)
        {
            return new Point
            {
                x = (a.x + b.x) / 2,
                y = (a.y + b.y) / 2,
                z = (a.z + b.z) / 2
            };
        }

        // Checks if point p is within segment defined by points a and b,
        // considering vertical or horizontal orientation
        static bool IsPointWithinSegment(Point p, Point a, Point b, bool vertical)
        {
            if (vertical)
            {
                double minY = Math.Min(a.y, b.y);
                double maxY = Math.Max(a.y, b.y);
                return p.y >= minY && p.y <= maxY;
            }
            else
            {
                double minX = Math.Min(a.x, b.x);
                double maxX = Math.Max(a.x, b.x);
                return p.x >= minX && p.x <= maxX;
            }
        }

        // private static bool IsFirstPointSmaller(Point p1, Point p2, bool isVertical)
        public static bool IsFirstPointSmaller(Point p1, Point p2, bool isVertical)
        {
            if (isVertical)
                return p1.y < p2.y;
            else
                return p1.x < p2.x;
        }

        // private static bool ArePointsEqual(Point p1, Point p2, double tolerance = 0.0001)
        public static bool ArePointsEqual(Point p1, Point p2, double tolerance = 0.0001)
        {
            if (p1 == null || p2 == null)
                return false;

            return Math.Abs(p1.x - p2.x) < tolerance &&
                   Math.Abs(p1.y - p2.y) < tolerance &&
                   Math.Abs(p1.z - p2.z) < tolerance;
        }


    }
}



