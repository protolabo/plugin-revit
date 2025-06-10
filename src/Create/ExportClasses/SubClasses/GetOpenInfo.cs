using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Create.ExportClasses.SubClasses
{
    internal class GetOpenInfo
    {
        public static object ExtractOpeningInfo(FamilyInstance inst, double wallX1, double wallY1, double wallZ1, double wallX2, double wallY2, double wallZ2)
        {
            double? width = null;
            double? height = null;
            FamilySymbol symbol = inst.Symbol;

            // Step 1: Search for width and height parameters
            // Attempt to retrieve the 'Width' and 'Height' parameters directly from the instance.
            // If not found or not set, try to find them in the family symbol parameters by checking for names
            // that contain "width" or "height" (case-insensitive) and ensure the parameter is of type Double.
            // If found and valid, convert the values from Revit internal units to millimeters.
            Parameter widthParam = inst.LookupParameter("Width");
            Parameter heightParam = inst.LookupParameter("Height");

            if ((widthParam == null || !widthParam.HasValue) && symbol != null)
            {
                widthParam = symbol.Parameters
                    .Cast<Parameter>()
                    .FirstOrDefault(p => p.Definition.Name.ToLower().Contains("width") && p.StorageType == StorageType.Double);
            }

            if ((heightParam == null || !heightParam.HasValue) && symbol != null)
            {
                heightParam = symbol.Parameters
                    .Cast<Parameter>()
                    .FirstOrDefault(p => p.Definition.Name.ToLower().Contains("height") && p.StorageType == StorageType.Double);
            }

            if (widthParam != null && widthParam.HasValue)
                width = UnitUtils.ConvertFromInternalUnits(widthParam.AsDouble(), UnitTypeId.Millimeters);

            if (heightParam != null && heightParam.HasValue)
                height = UnitUtils.ConvertFromInternalUnits(heightParam.AsDouble(), UnitTypeId.Millimeters);

            // Step 2: Fallback — Extract width and height from the family symbol's name
            // If width or height are still not determined, try to extract them from the symbol's name
            // assuming a format like "1000 x 2100" or similar.
            // Split the name by spaces and 'x'/'X', then filter digits and periods from each token.
            // Attempt to parse numbers from the cleaned tokens to assign width and height.
            if ((width == null || height == null) && symbol != null)
            {
                string[] tokens = symbol.Name.Split(' ', 'x', 'X');
                foreach (string token in tokens)
                {
                    string clean = new string(token.Where(c => char.IsDigit(c) || c == '.').ToArray());

                    if (double.TryParse(clean, out double value))
                    {
                        if (width == null) width = value;
                        else if (height == null) height = value;
                    }

                    if (width != null && height != null) break;
                }
            }

            // Step 3: Determine the central position of the element
            // Try to calculate the center point of the instance based on its location type:
            //
            // - If the element has a LocationPoint, use the point directly as the center.
            // - If it has a LocationCurve, evaluate the curve at its midpoint (parameter 0.5) to get the center.
            // - If neither is available, use the bounding box:
            //     * Retrieve the bounding box of the element.
            //     * Calculate the center as the average of the Min and Max corners.
            //
            // The resulting 'center' represents the 3D coordinates of the element's central position.
            XYZ center = null;
            object position = null;
            if (inst.Location is LocationPoint lp)
            {
                center = lp.Point;
            }
            else if (inst.Location is LocationCurve lc)
            {
                center = lc.Curve.Evaluate(0.5, true);
            }
            else
            {
                var bbox = inst.get_BoundingBox(null);
                if (bbox != null)
                {
                    center = (bbox.Min + bbox.Max) / 2;
                }
            }

            // Align the element's center with the wall's axis if applicable
            // If the center point has been successfully calculated:
            //
            // - Extract the X, Y, Z coordinates from the center.
            // - If the wall's X coordinates (start and end) are nearly the same, it means the wall is vertical.
            //   In that case, align the element's X position with the wall's X to ensure it sits flush.
            // - Similarly, if the wall's Y coordinates are nearly the same, the wall is horizontal,
            //   so align the element's Y position with the wall's Y.
            //
            // - Reconstruct the 'center' XYZ point with the updated coordinates,
            //   and store the position as an anonymous object with x, y, z values for further use.

            // IMPORTANT!!! Add code for diagonal Walls.
            if (center != null)
            {
                double x = center.X;
                double y = center.Y;
                double z = center.Z;

                // Align X if it's constant in the wall
                if (Math.Abs(wallX1 - wallX2) < 0.01)
                    x = wallX1;

                // Align y if it's constant in the wall
                if (Math.Abs(wallY1 - wallY2) < 0.01)
                    y = wallY1;

                center = new XYZ(x, y, z);
                position = new { x, y, z };
            }

            // Step 4: Calculate the start and end points of the opening (e.g., door or window)
            // in a direction orthogonal to the element's facing orientation.
            //
            // - Ensure that both 'center' and 'width' are available.
            // - Retrieve the normalized facing orientation vector of the instance.
            // - Compute an orthogonal vector (perpendicular to the facing direction) by rotating 90° in the XY plane.
            // - Convert half of the element's width from millimeters to feet (Revit's internal units).
            // - Multiply the orthogonal vector by the half width to get an offset from the center.
            // - Subtract and add the offset to the center point to calculate the start and end points.
            //
            // - Align the start and end points with the wall if it is strictly vertical or horizontal
            //   (based on whether X or Y is constant across the wall's endpoints).
            //
            // - Store the aligned start and end points as anonymous objects with x, y, z properties.
            object start_point = null;
            object end_point = null;

            if (center != null && width != null)
            {
                XYZ direction = inst.FacingOrientation?.Normalize();
                if (direction != null)
                {
                    XYZ ortho = new XYZ(-direction.Y, direction.X, direction.Z);
                    double halfWidth = (double)width / (2.0 * 304.8); // mm to ft
                    XYZ offset = ortho.Multiply(halfWidth);

                    XYZ start = center - offset;
                    XYZ end = center + offset;

                    // Align endpoints as well. MAY NOT BE NECESSARY. Run tests to verify.
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

            // Step 5. Result
            return new
            {
                type = inst.Category?.Name ?? "Unknown",
                id = inst.Id.IntegerValue,
                name = inst.Name,
                position,
                width_mm = width,
                height_mm = height,
                start_point,
                end_point
            };
        }
    }
}
