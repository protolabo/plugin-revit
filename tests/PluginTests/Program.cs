using Create.ExportClasses;
using System.Reflection;

class Program
{
    static void Main(string[] args)
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        string modelsPath = "./TestFiles/Models";
        string[] modelFiles = Directory.GetFiles(modelsPath, "*.json");

        foreach (var modelFile in modelFiles)
        {
            string fileName = Path.GetFileName(modelFile);

            Console.WriteLine($"\n🔁 Running tests for model: {fileName}");

            TemplateCreator.CreateTemplate(tempPath);
            Exporter.SimulateExport(fileName);

            // Fill id's values for Empty Template
            IDGenerator.GenerateIDInJsonFiles(tempPath);

            // ImageJsonFileCreator TEST
            Console.WriteLine("🧪 Running ImageJsonFileCreator test...");
            ImageJsonFileCreatorTester.RunTest(fileName);

            // WallSplitter TEST
            Console.WriteLine("🧪 Running WallSplitter test...");
            WallSplitterTester.RunTest(fileName);

            // Getters TEST
            Console.WriteLine("🧪 Running Getters test...");
            GettersTester.RunTest();

            // SegmentsListCreator TEST
            Console.WriteLine("🧪 Running SegmentsListCreator test...");
            SegmentsListCreatorTester.RunTest(fileName);

            // WallsInserter TEST
            Console.WriteLine("🧪 Running WallsInserter test...");
            WallsInserterTester.RunTest(fileName);

            // AttenuationUpdater TEST
            Console.WriteLine("🧪 Running AttenuationUpdater test...");
            AttenuationUpdaterTester.RunTest();

        }

    }
}

