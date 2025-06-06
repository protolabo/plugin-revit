using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Create.Helpers.SubFunctions
{
    internal class GetOpenInfo
    {
        public static object ExtractOpeningInfo(FamilyInstance inst, double wallX1, double wallY1, double wallZ1, double wallX2, double wallY2, double wallZ2)
        {
            double? width = null;
            double? height = null;
            FamilySymbol symbol = inst.Symbol;

            // 1. Buscar parámetros
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

            // 2. Extraer desde el nombre si falta
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

            // 3. Posición central
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

            // Alinear con el muro si aplica
            if (center != null)
            {
                double x = center.X;
                double y = center.Y;
                double z = center.Z;

                // Alinear X si es constante en el muro
                if (Math.Abs(wallX1 - wallX2) < 0.01)
                    x = wallX1;

                // Alinear Y si es constante en el muro
                if (Math.Abs(wallY1 - wallY2) < 0.01)
                    y = wallY1;

                center = new XYZ(x, y, z);
                position = new { x, y, z };
            }

            // 4. Calcular puntos de inicio y fin en dirección ortogonal a la orientación
            object start_point = null;
            object end_point = null;

            if (center != null && width != null)
            {
                XYZ direction = inst.FacingOrientation?.Normalize();
                if (direction != null)
                {
                    XYZ ortho = new XYZ(-direction.Y, direction.X, direction.Z);
                    double halfWidth = (double)width / (2.0 * 304.8); // mm a pies
                    XYZ offset = ortho.Multiply(halfWidth);

                    XYZ start = center - offset;
                    XYZ end = center + offset;

                    // Alinear extremos también
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

            // 5. Resultado
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
