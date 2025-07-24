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
        public int id { get; set; }                
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
        public int id;
        public string name;
        public Point start;
        public Point end;
        public List<OpeningData> openings;
    }
}
