using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    public class ModelData
    {
        public List<WallData> walls { get; set; }
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
        public string type { get; set; }           
        public long id { get; set; }                
        public string name { get; set; }           
        public Point position { get; set; }        
        public Point start_point { get; set; }     
        public Point end_point { get; set; }       
        public double? width_ft { get; set; }      
        public double? height_ft { get; set; }     
    }


    public class WallData
    {
        public string type;
        public long id;
        public string name;
        public Point start;
        public Point end;
        public List<OpeningData> openings;
    }

    public class ViewData
    {
        public string viewName { get; set; }
        public Point min { get; set; }
        public Point max { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class WallPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Id { get; set; }
    }
}
