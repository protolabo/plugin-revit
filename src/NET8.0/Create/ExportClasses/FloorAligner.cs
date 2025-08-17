using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace Create.ExportClasses
{
    internal class FloorAligner
    {
        public static void AlignFloorsAndGenerateJson(
            Dictionary<string, List<WallPoint>> wallPointsByFloor,
            List<ViewData> viewInfo,
            string tempPath)
        {
            if (wallPointsByFloor.Count < 2)
            {
                TaskDialog.Show("Info", "At least two floors are required to compare points.");
                return;
            }

            string referenceFloorId = wallPointsByFloor.Keys.First();
            List<WallPoint> basePoints = wallPointsByFloor[referenceFloorId];
            double tolerance = 20;

            TaskDialog.Show("Reference Floor", $"Using floor '{referenceFloorId}' as reference.");

            // List to store aligned groups of points (one per floor)
            List<List<AlignedPoint>> alignedGroups = new List<List<AlignedPoint>>();

            // Step 1: Find groups of aligned points across all floors
            foreach (var basePoint in basePoints)
            {
                var alignedGroup = new List<AlignedPoint>();

                // Add the base point from the reference floor
                alignedGroup.Add(new AlignedPoint
                {
                    FloorId = referenceFloorId,
                    X = basePoint.X,
                    Y = basePoint.Y,
                });

                bool allFloorsHaveMatch = true;

                // Look for matching points on all other floors
                foreach (var kvp in wallPointsByFloor)
                {
                    string otherFloorId = kvp.Key;
                    if (otherFloorId == referenceFloorId)
                        continue;

                    List<WallPoint> otherPoints = kvp.Value;

                    // Find a close enough point within the tolerance
                    var matched = otherPoints.FirstOrDefault(otherPoint =>
                    {
                        double dx = basePoint.X - otherPoint.X;
                        double dy = basePoint.Y - otherPoint.Y;
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        return distance <= tolerance;
                    });

                    if (matched != null)
                    {
                        alignedGroup.Add(new AlignedPoint
                        {
                            FloorId = otherFloorId,
                            X = matched.X,
                            Y = matched.Y,
                        });
                    }
                    else
                    {
                        allFloorsHaveMatch = false;
                        break;
                    }
                }

                if (allFloorsHaveMatch)
                {
                    alignedGroups.Add(alignedGroup);
                }
            }

            if (alignedGroups.Count == 0)
            {
                TaskDialog.Show("Info", "No fully aligned points found across all floors.");
                return;
            }

            // Step 2: Calculate approximate center axis from view info
            (double centerX, double centerY) = GetApproximateCenterAxis(viewInfo);

            // Step 3: Group aligned points by quadrant using the reference floor
            var quadrantGroups = GroupByQuadrant(alignedGroups, referenceFloorId, centerX, centerY);

            // Step 4: Select one group per quadrant (up to 4 groups)
            var selectedGroups = SelectOneGroupPerQuadrant(quadrantGroups);

            // Step 5: Generate JSON if we have at least 3 groups, else show error
            GenerateReferencePointsJson(selectedGroups, tempPath);
        }

        // Class representing a point aligned across floors
        public class AlignedPoint
        {
            public string FloorId { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        // Calculate approximate center axis (half the average width and height)
        public static (double centerX, double centerY) GetApproximateCenterAxis(List<ViewData> views)
        {
            if (views == null || views.Count == 0)
                throw new ArgumentException("The view list cannot be null or empty.");

            double averageWidth = views.Average(v => v.width);
            double averageHeight = views.Average(v => v.height);

            double centerX = averageWidth / 2.0;
            double centerY = averageHeight / 2.0;

            return (centerX, centerY);
        }

        // Group aligned groups by quadrant relative to the origin
        public static Dictionary<string, List<List<AlignedPoint>>> GroupByQuadrant(
            List<List<AlignedPoint>> alignedGroups,
            string referenceFloorId,
            double originX,
            double originY)
        {
            var quadrantGroups = new Dictionary<string, List<List<AlignedPoint>>>
            {
                { "I", new List<List<AlignedPoint>>() },
                { "II", new List<List<AlignedPoint>>() },
                { "III", new List<List<AlignedPoint>>() },
                { "IV", new List<List<AlignedPoint>>() }
            };

            foreach (var group in alignedGroups)
            {
                var refPoint = group.FirstOrDefault(p => p.FloorId == referenceFloorId);
                if (refPoint == null)
                    continue;

                if (refPoint.X > originX && refPoint.Y > originY)
                    quadrantGroups["I"].Add(group);
                else if (refPoint.X < originX && refPoint.Y > originY)
                    quadrantGroups["II"].Add(group);
                else if (refPoint.X < originX && refPoint.Y < originY)
                    quadrantGroups["III"].Add(group);
                else if (refPoint.X > originX && refPoint.Y < originY)
                    quadrantGroups["IV"].Add(group);
            }

            return quadrantGroups;
        }

        // Select one group from each quadrant, filling if needed, to get up to 4 groups
        public static List<List<AlignedPoint>> SelectOneGroupPerQuadrant(
            Dictionary<string, List<List<AlignedPoint>>> quadrantGroups)
        {
            var result = new List<List<AlignedPoint>>();
            string[] quadrantOrder = { "I", "II", "III", "IV" };

            // Take first group from each quadrant if available
            foreach (var quadrant in quadrantOrder)
            {
                if (quadrantGroups[quadrant].Count > 0)
                    result.Add(quadrantGroups[quadrant][0]);
            }

            // Fill remaining slots if less than 4 groups selected
            if (result.Count < 4)
            {
                foreach (var quadrant in quadrantOrder)
                {
                    foreach (var group in quadrantGroups[quadrant])
                    {
                        if (!result.Contains(group))
                        {
                            result.Add(group);
                            if (result.Count == 4)
                                break;
                        }
                    }
                    if (result.Count == 4)
                        break;
                }
            }

            return result;
        }

        // Generate JSON output or show error if less than 3 groups
        public static void GenerateReferencePointsJson(List<List<AlignedPoint>> selectedGroups, string tempPath)
        {
            if (selectedGroups == null || selectedGroups.Count < 3)
            {
                TaskDialog.Show("Error", "Could not align floors properly: fewer than 3 alignment groups found.");
                return;
            }

            var referencePoints = new List<object>();

            for (int i = 0; i < selectedGroups.Count; i++)
            {
                var group = selectedGroups[i];

                var projections = new List<object>();
                foreach (var point in group)
                {
                    projections.Add(new
                    {
                        floorPlanId = point.FloorId,
                        coord = new
                        {
                            x = point.X,
                            y = point.Y
                        }
                    });
                }

                referencePoints.Add(new
                {
                    name = $"Alignment Point {i + 1}",
                    projections = projections,
                    id = Guid.NewGuid().ToString(),
                    status = "CREATED"
                });
            }

            var finalObject = new
            {
                referencePoints = referencePoints
            };

            string json = JsonConvert.SerializeObject(finalObject, Formatting.Indented);

            File.WriteAllText(Path.Combine(tempPath, "referencePoints.json"), json);
            
        }
    }
}


