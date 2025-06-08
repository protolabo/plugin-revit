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
            int baseId = 1000000;

            foreach (var wall in data["walls"])
            {
                var openings = wall.openings ?? new List<OpeningData>();

                if (openings.Count == 0)
                {
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
