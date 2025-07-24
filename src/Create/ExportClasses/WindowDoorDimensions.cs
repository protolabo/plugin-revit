using Autodesk.Revit.DB;
using System;
using System.IO;

namespace Create.ExportClasses
{
    internal class WindowDoorDimensions
    {
        public static OpeningData GetWindowDoorDimensions(FamilyInstance inst,
            double wallX1, double wallY1, double wallZ1,
            double wallX2, double wallY2, double wallZ2)
        {
            double width = 3.2;  // default width in feet
            double height = 6.3; // default height in feet

            bool foundWidth = false;
            bool foundHeight = false;

            // 1. Search in instance parameters.
            Parameter widthParam = inst.LookupParameter("Width");
            Parameter heightParam = inst.LookupParameter("Height");

            // 2. If not found, search in the FamilySymbol using BuiltInParameter.
            if (widthParam == null || !widthParam.HasValue)
                widthParam = inst.Symbol?.get_Parameter(BuiltInParameter.DOOR_WIDTH);

            if (heightParam == null || !heightParam.HasValue)
                heightParam = inst.Symbol?.get_Parameter(BuiltInParameter.DOOR_HEIGHT);

            if (widthParam != null && widthParam.HasValue)
            {
                width = widthParam.AsDouble();
                foundWidth = true;
            }

            if (heightParam != null && heightParam.HasValue)
            {
                height = heightParam.AsDouble();
                foundHeight = true;
            }

            // 3. If not found, retrieve from geometry.
            if (!foundWidth || !foundHeight)
            {
                Options geomOptions = new Options
                {
                    ComputeReferences = false,
                    IncludeNonVisibleObjects = false,
                    DetailLevel = ViewDetailLevel.Fine
                };

                GeometryElement geomElement = inst.get_Geometry(geomOptions);

                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;
                double minZ = double.MaxValue, maxZ = double.MinValue;

                foreach (GeometryObject obj in geomElement)
                {
                    if (obj is GeometryInstance geomInst)
                    {
                        GeometryElement symbolGeometry = geomInst.GetSymbolGeometry();
                        foreach (GeometryObject geomObj in symbolGeometry)
                        {
                            if (geomObj is Solid solid && solid.Faces.Size > 0)
                            {
                                foreach (Edge edge in solid.Edges)
                                {
                                    foreach (XYZ point in edge.Tessellate())
                                    {
                                        minX = Math.Min(minX, point.X);
                                        maxX = Math.Max(maxX, point.X);
                                        minY = Math.Min(minY, point.Y);
                                        maxY = Math.Max(maxY, point.Y);
                                        minZ = Math.Min(minZ, point.Z);
                                        maxZ = Math.Max(maxZ, point.Z);
                                    }
                                }
                            }
                        }
                    }
                }

                double geomWidth = maxX - minX;
                double geomHeight = maxZ - minZ;

                if (!foundWidth && geomWidth > 0)
                    width = geomWidth;

                if (!foundHeight && geomHeight > 0)
                    height = geomHeight;
            }

            // 4. Calculate central position.
            XYZ center = null;
            object position = null;

            if (inst.Location is LocationPoint lp)
                center = lp.Point;
            else if (inst.Location is LocationCurve lc)
                center = lc.Curve.Evaluate(0.5, true);
            else
            {
                BoundingBoxXYZ bbox = inst.get_BoundingBox(null);
                if (bbox != null)
                    center = (bbox.Min + bbox.Max) / 2;
            }

            if (center != null)
            {
                double x = center.X;
                double y = center.Y;
                double z = center.Z;

                if (Math.Abs(wallX1 - wallX2) < 0.01)
                    x = wallX1;
                if (Math.Abs(wallY1 - wallY2) < 0.01)
                    y = wallY1;

                center = new XYZ(x, y, z);
                position = new { x, y, z };
            }

            // 5. Calculate start_point y end_point
            object start_point = null;
            object end_point = null;

            if (center != null)
            {
                XYZ direction = inst.FacingOrientation?.Normalize();
                if (direction != null)
                {
                    XYZ ortho = new XYZ(-direction.Y, direction.X, direction.Z);
                    double halfWidth = width / 2.0;
                    XYZ offset = ortho.Multiply(halfWidth);

                    XYZ start = center - offset;
                    XYZ end = center + offset;

                    if (Math.Abs(wallX1 - wallX2) < 0.01)
                    {
                        start = new XYZ(wallX1, start.Y, start.Z);
                        end = new XYZ(wallX1, end.Y, end.Z);
                    }
                    if (Math.Abs(wallY1 - wallY2) < 0.01)
                    {
                        start = new XYZ(start.X, wallY1, start.Z);
                        end = new XYZ(end.X, wallY1, end.Z);
                    }

                    start_point = new { x = start.X, y = start.Y, z = start.Z };
                    end_point = new { x = end.X, y = end.Y, z = end.Z };
                }
            }

            // 6. Return Object
            return new OpeningData
            {
                type = inst.Category?.Name ?? "Unknown",
                id = inst.Id.IntegerValue,
                name = inst.Name,
                position = position != null ? new Point
                {
                    x = ((dynamic)position).x,
                    y = ((dynamic)position).y,
                    z = ((dynamic)position).z
                } : null,
                width_ft = width,
                height_ft = height,
                start_point = start_point != null ? new Point
                {
                    x = ((dynamic)start_point).x,
                    y = ((dynamic)start_point).y,
                    z = ((dynamic)start_point).z
                } : null,
                end_point = end_point != null ? new Point
                {
                    x = ((dynamic)end_point).x,
                    y = ((dynamic)end_point).y,
                    z = ((dynamic)end_point).z
                } : null
            };

        }
    }
}



