using System.Collections.Generic;

namespace Create.ExportClasses
{
    public class ImageData
    {
        public string viewName { get; set; }
        public Point min { get; set; }
        public Point max { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class CropRegion
    {
        public string viewName { get; set; }
        public Point min { get; set; }
        public Point max { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public double scale { get; set; }
    }

    public class ModelInfo
    {
        public List<CropRegion> cropRegions { get; set; }
        public string walls { get; set; }
        public string walls_expected { get; set; }
        public List<string> images_expected { get; set; }
        public string wallPoints_expected { get; set; }
        public string wallSegments_expected { get; set; }
    }

    public class WallSegment
    {
        public string Id { get; set; }
        public string WallTypeId { get; set; }
        public string OriginType { get; set; }
        public string Status { get; set; }
        public List<WallPoint> WallPoints { get; set; }
    }

    public class WallPoint
    {
        public string Id { get; set; }
        public Location Location { get; set; }
        public string Status { get; set; }
    }

    public class Location
    {
        public string FloorPlanId { get; set; }
        public Coord Coord { get; set; }
    }

    public class Coord
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

}
