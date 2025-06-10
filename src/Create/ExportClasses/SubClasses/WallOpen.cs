using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses.SubClasses
{
    internal class WallOpen
    {
        public static void ProcessWallOpen(string inputFileName, string outputFileName)
        {
            string buildToolsPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "build_files", "build_tools");
            string inputPath = Path.Combine(buildToolsPath, $"{inputFileName}.json");
            string outputPath = Path.Combine(buildToolsPath, $"{outputFileName}.json");

            string json = File.ReadAllText(inputPath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, List<WallData>>>(json);

            List<WallData> wallResults = new List<WallData>();
            // This ID doesn't have any real significance.
            int baseId = 1000000;

            foreach (var wall in data["walls"])
            {
                var openings = wall.openings ?? new List<OpeningData>();

                if (openings.Count == 0)
                {
                    // Ignore walls shorter than one inch in length.
                    // These are considered too small to be relevant for processing or export.
                    // This code may change if we get all walls connected!!!!!!!!
                    if (LengthBetweenPoints(wall.start, wall.end) >= 0.08)
                        wallResults.Add(wall);
                    continue;
                }

                baseId = RecursiveWallSplit(wall, openings, wallResults, baseId);
            }

            var resultJson = JsonConvert.SerializeObject(new { walls = wallResults }, Formatting.Indented);
            File.WriteAllText(outputPath, resultJson);
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

        // Recursively splits a wall into segments based on the openings (e.g., doors/windows) it contains.
        //
        // Parameters:
        // - wall: The current wall segment to process.
        // - openings: A list of openings that intersect with this wall segment.
        // - results: A list where the resulting wall segments without openings will be added.
        // - newBaseId: The current ID to assign to newly created wall segments.
        //
        // Process:
        // 1. If there are no openings left to process, check if the wall segment's length is at least 0.08 units (approx. 1 inch).
        //    If so, add it to the results list and return the current newBaseId.
        // 2. Determine the axis of the wall segment (either "x" or "y") based on the start and end points.
        //    If the wall is not aligned along X or Y (i.e., diagonal), check length and add it directly to results.
        // 3. Extract the start and end coordinates of the first opening relative to the wall's axis.
        // 4. Calculate distances from the wall's start and end to the opening's start and end to find which end to split from.
        // 5. Depending on which distance is shorter, split the wall segment at the appropriate opening edge:
        //    - Create two new wall segments (wall1 and wall2).
        //    - Assign new IDs to each segment.
        //    - Adjust start and end points accordingly to exclude the opening area.
        // 6. Increment newBaseId by 2 to prepare IDs for further recursive splits.
        // 7. Recursively call `RecursiveWallSplit` on the two new wall segments, passing only the openings that lie inside each segment.
        // 8. Return the updated newBaseId for continued ID assignment.
        //
        // This function effectively divides a wall into smaller segments around openings, 
        // ensuring each wall segment in the results does not intersect with any opening.
        static int RecursiveWallSplit(WallData wall, List<OpeningData> openings, List<WallData> results, int newBaseId)
        {
            if (openings.Count == 0)
            {
                if (LengthBetweenPoints(wall.start, wall.end) >= 0.08)
                    results.Add(wall);
                return newBaseId;
            }

            var opening = openings[0];
            var remainingOpenings = openings.Skip(1).ToList();

            string axis;
            if (Math.Abs(wall.start.x - wall.end.x) < 1e-6) axis = "y";
            else if (Math.Abs(wall.start.y - wall.end.y) < 1e-6) axis = "x";
            else
            {
                if (LengthBetweenPoints(wall.start, wall.end) >= 0.08)
                    results.Add(wall);
                return newBaseId;
            }

            double openStart = axis == "x" ? opening.start_point.x : opening.start_point.y;
            double openEnd = axis == "x" ? opening.end_point.x : opening.end_point.y;
            double startVal = axis == "x" ? wall.start.x : wall.start.y;
            double endVal = axis == "x" ? wall.end.x : wall.end.y;

            double distStart = Math.Min(Math.Abs(startVal - openStart), Math.Abs(startVal - openEnd));
            double distEnd = Math.Min(Math.Abs(endVal - openStart), Math.Abs(endVal - openEnd));

            WallData wall1, wall2;

            if (distStart < distEnd)
            {
                double cut = Math.Abs(startVal - openStart) < Math.Abs(startVal - openEnd) ? openStart : openEnd;

                wall1 = new WallData
                {
                    type = wall.type,
                    id = newBaseId,
                    name = wall.name,
                    start = new Point(wall.start),
                    end = new Point(wall.start),
                    openings = new List<OpeningData>()
                };
                if (axis == "x") wall1.end.x = cut; else wall1.end.y = cut;

                wall2 = new WallData
                {
                    type = wall.type,
                    id = newBaseId + 1,
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
                double cut = Math.Abs(endVal - openStart) < Math.Abs(endVal - openEnd) ? openStart : openEnd;

                wall1 = new WallData
                {
                    type = wall.type,
                    id = newBaseId,
                    name = wall.name,
                    start = new Point(wall.end),
                    end = new Point(wall.end),
                    openings = new List<OpeningData>()
                };
                if (axis == "x") wall1.end.x = cut; else wall1.end.y = cut;

                wall2 = new WallData
                {
                    type = wall.type,
                    id = newBaseId + 1,
                    name = wall.name,
                    start = new Point(wall.start),
                    end = new Point(wall.start),
                    openings = new List<OpeningData>()
                };
                if (axis == "x") wall2.end.x = cut == openStart ? openEnd : openStart;
                else wall2.end.y = cut == openStart ? openEnd : openStart;
            }

            newBaseId += 2;

            var openings1 = remainingOpenings.Where(op => IsCenterInsideWall(wall1, CenterOfElement(op))).ToList();
            var openings2 = remainingOpenings.Where(op => IsCenterInsideWall(wall2, CenterOfElement(op))).ToList();

            newBaseId = RecursiveWallSplit(wall1, openings1, results, newBaseId);
            newBaseId = RecursiveWallSplit(wall2, openings2, results, newBaseId);

            return newBaseId;
        }
    }

    public class Point
    {
        public double x;
        public double y;
        public double z;

        public Point() { }

        public Point(Point other)
        {
            x = other.x;
            y = other.y;
            z = other.z;
        }
    }

    public class OpeningData
    {
        public Point start_point;
        public Point end_point;
        public Point position;
    }

    public class WallData
    {
        public string type;
        public int id;
        public string name;
        public Point start;
        public Point end;
        public List<OpeningData> openings;
    }
}
