using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.Helpers.SubFunctions
{
    internal class WallOpen
    {
        public static void ProcessWallOpen(string inputFileName, string outputFileName)
        {
            string buildToolsPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "build_files", "build_tools");
            string inputPath = Path.Combine(buildToolsPath, $"{inputFileName}.json");

            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string outputPath = Path.Combine(buildToolsPath, $"{outputFileName}.json");

            string json = File.ReadAllText(inputPath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, List<WallData>>>(json);

            List<WallData> resultadoMuro = new List<WallData>();
            int idBase = 1000000;

            foreach (var muro in data["walls"])
            {
                var aperturas = muro.openings ?? new List<OpeningData>();

                if (aperturas.Count == 0)
                {
                    if (LengthBetweenPoints(muro.start, muro.end) >= 0.08)
                        resultadoMuro.Add(muro);
                    continue;
                }

                idBase = DividirMuroRecursivo(muro, aperturas, resultadoMuro, idBase);
            }

            var resultJson = JsonConvert.SerializeObject(new { walls = resultadoMuro }, Formatting.Indented);
            File.WriteAllText(outputPath, resultJson);

            //Console.WriteLine($"✅ Archivo 'muros_divididos.json' creado en el escritorio.");
        }

        static double LengthBetweenPoints(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        static Point CentroElemento(OpeningData elem) => elem.position;

        static bool CentroDentroDeMuro(WallData muro, Point centro)
        {
            double xMin = Math.Min(muro.start.x, muro.end.x);
            double xMax = Math.Max(muro.start.x, muro.end.x);
            double yMin = Math.Min(muro.start.y, muro.end.y);
            double yMax = Math.Max(muro.start.y, muro.end.y);
            double margen = 1e-9;

            return (xMin - margen <= centro.x && centro.x <= xMax + margen) &&
                   (yMin - margen <= centro.y && centro.y <= yMax + margen);
        }

        static int DividirMuroRecursivo(WallData muro, List<OpeningData> aperturas, List<WallData> resultado, int nuevoIdBase)
        {
            if (aperturas.Count == 0)
            {
                if (LengthBetweenPoints(muro.start, muro.end) >= 0.08)
                    resultado.Add(muro);
                return nuevoIdBase;
            }

            var apertura = aperturas[0];
            var restantes = aperturas.Skip(1).ToList();

            string eje;
            if (Math.Abs(muro.start.x - muro.end.x) < 1e-6) eje = "y";
            else if (Math.Abs(muro.start.y - muro.end.y) < 1e-6) eje = "x";
            else
            {
                if (LengthBetweenPoints(muro.start, muro.end) >= 0.08)
                    resultado.Add(muro);
                return nuevoIdBase;
            }

            double apStart = eje == "x" ? apertura.start_point.x : apertura.start_point.y;
            double apEnd = eje == "x" ? apertura.end_point.x : apertura.end_point.y;
            double startVal = eje == "x" ? muro.start.x : muro.start.y;
            double endVal = eje == "x" ? muro.end.x : muro.end.y;

            double dInicio = Math.Min(Math.Abs(startVal - apStart), Math.Abs(startVal - apEnd));
            double dFin = Math.Min(Math.Abs(endVal - apStart), Math.Abs(endVal - apEnd));

            WallData muro1, muro2;

            if (dInicio < dFin)
            {
                double corte = Math.Abs(startVal - apStart) < Math.Abs(startVal - apEnd) ? apStart : apEnd;

                muro1 = new WallData
                {
                    type = muro.type,
                    id = nuevoIdBase,
                    name = muro.name,
                    start = new Point(muro.start),
                    end = new Point(muro.start),
                    openings = new List<OpeningData>()
                };
                if (eje == "x") muro1.end.x = corte; else muro1.end.y = corte;

                muro2 = new WallData
                {
                    type = muro.type,
                    id = nuevoIdBase + 1,
                    name = muro.name,
                    start = new Point(muro.end),
                    end = new Point(muro.end),
                    openings = new List<OpeningData>()
                };
                if (eje == "x") muro2.start.x = corte == apStart ? apEnd : apStart;
                else muro2.start.y = corte == apStart ? apEnd : apStart;
            }
            else
            {
                double corte = Math.Abs(endVal - apStart) < Math.Abs(endVal - apEnd) ? apStart : apEnd;

                muro1 = new WallData
                {
                    type = muro.type,
                    id = nuevoIdBase,
                    name = muro.name,
                    start = new Point(muro.end),
                    end = new Point(muro.end),
                    openings = new List<OpeningData>()
                };
                if (eje == "x") muro1.end.x = corte; else muro1.end.y = corte;

                muro2 = new WallData
                {
                    type = muro.type,
                    id = nuevoIdBase + 1,
                    name = muro.name,
                    start = new Point(muro.start),
                    end = new Point(muro.start),
                    openings = new List<OpeningData>()
                };
                if (eje == "x") muro2.end.x = corte == apStart ? apEnd : apStart;
                else muro2.end.y = corte == apStart ? apEnd : apStart;
            }

            nuevoIdBase += 2;

            var aperturas1 = restantes.Where(op => CentroDentroDeMuro(muro1, CentroElemento(op))).ToList();
            var aperturas2 = restantes.Where(op => CentroDentroDeMuro(muro2, CentroElemento(op))).ToList();

            nuevoIdBase = DividirMuroRecursivo(muro1, aperturas1, resultado, nuevoIdBase);
            nuevoIdBase = DividirMuroRecursivo(muro2, aperturas2, resultado, nuevoIdBase);

            return nuevoIdBase;
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
