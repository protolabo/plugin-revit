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

                    // Generate segments and classify them
                    List<OpeningData> segments = new List<OpeningData>();
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var p1 = points[i];
                        var p2 = points[i + 1];
                        var mid = MidPoint(p1, p2);

                        // Find matching opening segment for midpoint
                        var match = (wall.openings ?? new List<OpeningData>()).FirstOrDefault(o =>
                            o.start_point != null && o.end_point != null &&
                            IsPointWithinSegment(mid, o.start_point, o.end_point, isVertical));

                        if (match != null)
                        {
                            // Use original opening data
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
                            // Segment belongs to main wall
                            segments.Add(new OpeningData
                            {
                                type = "Walls",
                                name = wall.name,
                                start_point = p1,
                                end_point = p2
                            });
                        }
                    }

                    // Merge consecutive segments with the same opening ID (except wall segments with id=0)
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

                    // Create copy of original wall with updated openings list
                    var baseWall = new WallData
                    {
                        id = wall.id,
                        name = wall.name,
                        type = wall.type,
                        start = wall.start,
                        end = wall.end,
                        openings = mergedSegments
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
    }
}



