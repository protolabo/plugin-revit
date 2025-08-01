using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Create.ExportClasses; 

public static class ShorthandParser
{
    private static HashSet<int> usedIds = new HashSet<int>();
    private static Random rng = new Random();

    public static List<(Dictionary<string, ModelData> input, Dictionary<string, ModelData> expected)> LoadShorthandTests(string[] lines)
    {
        var allTests = new List<(Dictionary<string, ModelData>, Dictionary<string, ModelData>)>();

        for (int i = 0; i < lines.Length - 1; i += 2)
        {
            string inputLine = lines[i];
            string expectedLine = lines[i + 1];

            var inputData = FromShorthandToModelData(inputLine);
            var expectedData = FromShorthandToExpectedModelData(expectedLine);

            allTests.Add((inputData, expectedData));
        }

        return allTests;
    }

    public static Dictionary<string, ModelData> FromShorthandToModelData(string shorthand)
    {
        return ParseInternal(shorthand, assignIds: true);
    }

    public static Dictionary<string, ModelData> FromShorthandToExpectedModelData(string shorthand)
    {
        return ParseInternal(shorthand, assignIds: false);
    }

    public static string ToShorthand(Dictionary<string, ModelData> modelData)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var model in modelData.Values)
        {
            if (model.walls.Count > 0)
            {
                var firstWall = model.walls[0];
                bool isHorizontal = firstWall.start.y == firstWall.end.y;
                bool isVertical = firstWall.start.x == firstWall.end.x;
                bool isInclined = firstWall.start.x != firstWall.end.x && firstWall.start.y != firstWall.end.y;

                if (isHorizontal)
                {
                    sb.Append($"h[{firstWall.start.y:0.##}]");
                }
                else if (isVertical)
                {
                    sb.Append($"v[{firstWall.start.x:0.##}]");
                }
                else if (isInclined)
                {
                    sb.Append("i[]");
                }

            }
        }

        bool firstLevel = true;
        foreach (var levelKvp in modelData.OrderBy(kvp => kvp.Key))
        {
            if (!firstLevel)
            {
                sb.Append("L");
            }

            var model = levelKvp.Value;
            firstLevel = false;

            foreach (var wall in model.walls)
            {
                bool isHorizontal = wall.start.y == wall.end.y;
                string aStart = isHorizontal ? wall.start.x.ToString("0.##") : wall.start.y.ToString("0.##");
                string aEnd = isHorizontal ? wall.end.x.ToString("0.##") : wall.end.y.ToString("0.##");

                sb.Append($"w[{aStart},{aEnd}]");

                if (wall.openings != null && wall.openings.Count > 0)
                {
                    sb.Append("{");

                    foreach (var opening in wall.openings)
                    {
                        string oStart = isHorizontal ? opening.start_point.x.ToString("0.##") : opening.start_point.y.ToString("0.##");
                        string oEnd = isHorizontal ? opening.end_point.x.ToString("0.##") : opening.end_point.y.ToString("0.##");

                        string code = opening.type switch
                        {
                            "Walls" => "w",
                            "Opening" => "o",
                            "Doors" => "d",
                            "Windows" => "g",
                            _ => "u"
                        };

                        sb.Append($"{code}[{oStart},{oEnd}]");
                    }

                    sb.Append("}");
                }
            }
        }

        return sb.ToString();
    }

    private static Dictionary<string, ModelData> ParseInternal(string shorthand, bool assignIds)
{
    var result = new Dictionary<string, ModelData>();
    int levelCount = 1;
    double fixedCoord = 0;
    bool isHorizontal = true;
    bool isInclined = false;

    var levels = shorthand.Split('L');
    foreach (var level in levels)
    {
        if (string.IsNullOrWhiteSpace(level)) continue;

        var orientationMatch = Regex.Match(level, @"^(h|v|i)\[([-+]?[0-9]*\.?[0-9]*)\]");
        string rest = level;
        if (orientationMatch.Success)
        {
            string orientation = orientationMatch.Groups[1].Value;
            string value = orientationMatch.Groups[2].Value;

            isHorizontal = orientation == "h";
            isInclined = orientation == "i";

            if (!isInclined)
            {
                fixedCoord = double.Parse(value);
            }

            rest = level.Substring(orientationMatch.Length);
        }

        var wallMatches = Regex.Matches(rest, @"w\[([-+]?[0-9]*\.?[0-9]+),([-+]?[0-9]*\.?[0-9]+)\](\{[^}]*\})?");
        var wallList = new List<WallData>();

        foreach (Match wallMatch in wallMatches)
        {
            double startVal = double.Parse(wallMatch.Groups[1].Value);
            double endVal = double.Parse(wallMatch.Groups[2].Value);
            string openingsRaw = wallMatch.Groups[3].Value;

            Point startPoint, endPoint;

            if (isInclined)
            {
                startPoint = new Point { x = startVal, y = startVal, z = 0 };
                endPoint = new Point { x = endVal, y = endVal, z = 0 };
            }
            else if (isHorizontal)
            {
                startPoint = new Point { x = startVal, y = fixedCoord, z = 0 };
                endPoint = new Point { x = endVal, y = fixedCoord, z = 0 };
            }
            else // vertical
            {
                startPoint = new Point { x = fixedCoord, y = startVal, z = 0 };
                endPoint = new Point { x = fixedCoord, y = endVal, z = 0 };
            }

            var wall = new WallData
            {
                type = "Walls",
                id = 0,
                start = startPoint,
                end = endPoint,
                openings = new List<OpeningData>()
            };

            if (!string.IsNullOrEmpty(openingsRaw))
            {
                var openingMatches = Regex.Matches(openingsRaw, @"([wodg])\[([-+]?[0-9]*\.?[0-9]+),([-+]?[0-9]*\.?[0-9]+)\]");
                foreach (Match openingMatch in openingMatches)
                {
                    string oCode = openingMatch.Groups[1].Value;
                    double oStartVal = double.Parse(openingMatch.Groups[2].Value);
                    double oEndVal = double.Parse(openingMatch.Groups[3].Value);

                    var typeMap = new Dictionary<string, string>
                    {
                        ["w"] = "Walls",
                        ["o"] = "Opening",
                        ["d"] = "Doors",
                        ["g"] = "Windows"
                    };

                    Point oStart, oEnd;

                    if (isInclined)
                    {
                        oStart = new Point { x = oStartVal, y = oStartVal, z = 0 };
                        oEnd = new Point { x = oEndVal, y = oEndVal, z = 0 };
                    }
                    else if (isHorizontal)
                    {
                        oStart = new Point { x = oStartVal, y = fixedCoord, z = 0 };
                        oEnd = new Point { x = oEndVal, y = fixedCoord, z = 0 };
                    }
                    else // vertical
                    {
                        oStart = new Point { x = fixedCoord, y = oStartVal, z = 0 };
                        oEnd = new Point { x = fixedCoord, y = oEndVal, z = 0 };
                    }

                    wall.openings.Add(new OpeningData
                    {
                        type = typeMap[oCode],
                        id = assignIds ? GenerateUniqueId() : 0,
                        start_point = oStart,
                        end_point = oEnd
                    });
                }
            }

            wallList.Add(wall);
        }

        result[$"Level_{levelCount}"] = new ModelData { walls = wallList };
        levelCount++;
    }

    return result;
}


    private static int GenerateUniqueId()
    {
        int id;
        do
        {
            id = rng.Next(100000, 999999);
        } while (!usedIds.Add(id));
        return id;
    }
}