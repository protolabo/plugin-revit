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
                    // Checks if the wall is valid
                    if (wall.start == null || wall.end == null)
                    {
                        updatedWalls.Add(wall); 
                        continue;
                    }

                    var baseWall = new WallData
                    {
                        id = wall.id,
                        name = wall.name,
                        type = wall.type,
                        start = wall.start,
                        end = wall.end,
                        openings = wall.openings != null ? new List<OpeningData>(wall.openings) : new List<OpeningData>()
                    };

                    List<OpeningData> originalOpenings = wall.openings ?? new List<OpeningData>();
                    List<WallData> segments = new List<WallData>();

                    // Split wall in segments according to openings list
                    RecursiveWallSplit(baseWall, originalOpenings, segments);

                    // Create an object for every segment
                    foreach (var segment in segments)
                    {
                        baseWall.openings.Add(new OpeningData
                        {
                            type = "Walls",
                            name = segment.name,
                            start_point = segment.start,
                            end_point = segment.end
                        });
                    }

                    updatedWalls.Add(baseWall);

                }

                // Remove "voids" from the final openings list.
                foreach (var wall in updatedWalls)
                {
                    wall.openings = wall.openings
                        ?.Where(o => o.type != "Opening")
                        .ToList();
                }

                // At this point, the final list of openings contains only doors, windows, and the wall segments that make up the main wall.
                // To ensure their proper interconnection, it is necessary to make sure that the wall segments are ordered,
                // and that the start and end points of each segment are also in the correct order.
                foreach (var wall in updatedWalls)
                {
                    if (wall.start == null || wall.end == null || wall.openings == null)
                        continue;

                    // Check whether the wall is vertical or horizontal.
                    string axis = Math.Abs(wall.start.x - wall.end.x) < 1e-6 ? "y" : "x";

                    // Reorder each segment in ascending order, including its start and end points
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
                        .OrderBy(o =>
                        {
                            return axis == "x" ? o.start_point.x : o.start_point.y;
                        })
                        .ToList();
                }



                // Replace the original walls with the updated ones containing embedded segments
                modelDataSegments[viewName] = new ModelData
                {
                    walls = updatedWalls
                };
            }


            //// save file
            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //string outputFilePath = Path.Combine(desktopPath, "model_data_segments.json");

            //var options = new System.Text.Json.JsonSerializerOptions
            //{
            //    WriteIndented = true,
            //    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            //};

            //try
            //{
            //    string json = Newtonsoft.Json.JsonConvert.SerializeObject(modelDataSegments, Newtonsoft.Json.Formatting.Indented);
            //    File.WriteAllText(outputFilePath, json);

            //}
            //catch (Exception ex)
            //{
            //    //TaskDialog.Show("Error", $"No JSON:\n{ex.Message}");
            //}
        }


        static double LengthBetweenPoints(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        static Point CenterOfElement(OpeningData element) => element.position;

        static bool IsCenterInsideWall(WallData wall, Point center)
        {
            double xMin = Math.Min(wall.start.x, wall.end.x);
            double xMax = Math.Max(wall.start.x, wall.end.x);
            double yMin = Math.Min(wall.start.y, wall.end.y);
            double yMax = Math.Max(wall.start.y, wall.end.y);
            double margin = 1e-9;

            return (xMin - margin <= center.x && center.x <= xMax + margin) &&
                   (yMin - margin <= center.y && center.y <= yMax + margin);
        }

        // The function RecursiveWallSplit is a recursive function that divides a wall into segments depending on the openings
        // it contains and removes the segments that overlap with the openings to place the corresponding opening in their place.
        // This function receives a wall, a list of openings, and a list of results.
        //  - Base case: If the wall does not contain any openings, the wall is added to the results list.
        //  - Recursive case: The first opening is taken (the first in the list, not necessarily the first in position),
        //      and the wall is split into two: one segment from one end of the wall to one end of the opening,
        //      and another segment from the other end of the opening to the other end of the wall.
        //      Then, the openings are assigned to the appropriate wall segments, the opening used to divide the wall is removed,
        //      and the function is called recursively with the new wall segments and their corresponding remaining openings.
        static void RecursiveWallSplit(WallData wall, List<OpeningData> openings, List<WallData> results)
        {
            if (openings.Count == 0)
            {
                // if the wall is less than 1 inch in length, ignore it
                if (LengthBetweenPoints(wall.start, wall.end) >= 0.08)
                    results.Add(wall);
                return;
            }

            var opening = openings[0];
            var remainingOpenings = openings.Skip(1).ToList();

            string axis;
            if (Math.Abs(wall.start.x - wall.end.x) < 1e-6) axis = "y";
            else if (Math.Abs(wall.start.y - wall.end.y) < 1e-6) axis = "x";
            else
            {
                double wallLength = LengthBetweenPoints(wall.start, wall.end);
                if (wallLength < 0.08)
                    return;

                double distA = LengthBetweenPoints(wall.start, opening.start_point);
                double distB = LengthBetweenPoints(wall.start, opening.end_point);

                // Split wall in segments according to the opening
                Point cut = distA < distB ? opening.start_point : opening.end_point;
                Point other = distA < distB ? opening.end_point : opening.start_point;

                var wall_d_1 = new WallData
                {
                    type = wall.type,
                    name = wall.name,
                    start = new Point(wall.start),
                    end = new Point(cut),
                    openings = new List<OpeningData>()
                };

                var wall_d_2 = new WallData
                {
                    type = wall.type,
                    name = wall.name,
                    start = new Point(other),
                    end = new Point(wall.end),
                    openings = new List<OpeningData>()
                };

                if (LengthBetweenPoints(wall_d_1.start, wall_d_1.end) < 0.08 &&
                    LengthBetweenPoints(wall_d_2.start, wall_d_2.end) < 0.08)
                    return;

                // Filter the remaining openings to assign them to the newly created wall segments.
                // For each new wall segment, only include openings whose centers lie within its boundaries.
                // This ensures that each recursive call processes only the openings relevant to its segment.
                var openings_d_1 = remainingOpenings.Where(op => IsCenterInsideWall(wall_d_1, CenterOfElement(op))).ToList();
                var openings_d_2 = remainingOpenings.Where(op => IsCenterInsideWall(wall_d_2, CenterOfElement(op))).ToList();

                RecursiveWallSplit(wall_d_1, openings_d_1, results);
                RecursiveWallSplit(wall_d_2, openings_d_2, results);

                return;
            }

            double openStart = axis == "x" ? opening.start_point.x : opening.start_point.y;
            double openEnd = axis == "x" ? opening.end_point.x : opening.end_point.y;
            double startVal = axis == "x" ? wall.start.x : wall.start.y;
            double endVal = axis == "x" ? wall.end.x : wall.end.y;

            double distStart = Math.Min(Math.Abs(startVal - openStart), Math.Abs(startVal - openEnd));
            double distEnd = Math.Min(Math.Abs(endVal - openStart), Math.Abs(endVal - openEnd));

            WallData wall1, wall2;

            // Determines whether the opening is closer to the start or the end of the wall.
            if (distStart < distEnd)
            {
                // Split wall in segments according to the opening
                double cut = Math.Abs(startVal - openStart) < Math.Abs(startVal - openEnd) ? openStart : openEnd;

                wall1 = new WallData
                {
                    type = wall.type,
                    name = wall.name,
                    start = new Point(wall.start),
                    end = new Point(wall.start),
                    openings = new List<OpeningData>()
                };
                if (axis == "x") wall1.end.x = cut; else wall1.end.y = cut;

                wall2 = new WallData
                {
                    type = wall.type,
                    name = wall.name,
                    start = new Point(wall.end),
                    end = new Point(wall.end),
                    openings = new List<OpeningData>()
                };
                if (axis == "x") wall2.start.x = cut == openStart ? openEnd : openStart;
                else wall2.start.y = cut == openStart ? openEnd : openStart;
            }
            else
            {
                // Split wall in segments according to the opening
                double cut = Math.Abs(endVal - openStart) < Math.Abs(endVal - openEnd) ? openStart : openEnd;

                wall1 = new WallData
                {
                    type = wall.type,
                    name = wall.name,
                    start = new Point(wall.end),
                    end = new Point(wall.end),
                    openings = new List<OpeningData>()
                };
                if (axis == "x") wall1.end.x = cut; else wall1.end.y = cut;

                wall2 = new WallData
                {
                    type = wall.type,
                    name = wall.name,
                    start = new Point(wall.start),
                    end = new Point(wall.start),
                    openings = new List<OpeningData>()
                };
                if (axis == "x") wall2.end.x = cut == openStart ? openEnd : openStart;
                else wall2.end.y = cut == openStart ? openEnd : openStart;
            }

            // Filter the remaining openings to assign them to the newly created wall segments.
            // For each new wall segment, only include openings whose centers lie within its boundaries.
            // This ensures that each recursive call processes only the openings relevant to its segment.
            var openings1 = remainingOpenings.Where(op => IsCenterInsideWall(wall1, CenterOfElement(op))).ToList();
            var openings2 = remainingOpenings.Where(op => IsCenterInsideWall(wall2, CenterOfElement(op))).ToList();

            RecursiveWallSplit(wall1, openings1, results);
            RecursiveWallSplit(wall2, openings2, results);

            return;
        }
    }

}



