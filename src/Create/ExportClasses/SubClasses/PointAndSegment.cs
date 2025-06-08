using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses.SubClasses
{
    internal class PointAndSegment
    {
        public static string MakeWallPoint(string id, string floorPlanId, double x, double y)
        {
            return $@"{{
              ""location"": {{
                ""floorPlanId"": ""{floorPlanId}"",
                ""coord"": {{ ""x"": {x}, ""y"": {y} }}
              }},
              ""id"": ""{id}"",
              ""status"": ""CREATED""
            }}";
        }

        public static string MakeWallSegment(string idStart, string idEnd, string wallTypeId)
        {
            return $@"{{
              ""wallPoints"": [""{idStart}"", ""{idEnd}""],
              ""wallTypeId"": ""{wallTypeId}"",
              ""originType"": ""WALL_TOOL"",
              ""id"": ""{Guid.NewGuid()}"",
              ""status"": ""CREATED""
            }}";
        }
    }
}
