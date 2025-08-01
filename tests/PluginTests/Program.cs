using Create.ExportClasses;
using System.Reflection;
using System;
using System.IO;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        bool generateReport = false;

        StringBuilder consoleOutput = new StringBuilder();
        TextWriter originalConsole = Console.Out;

        if (generateReport)
        {
            Console.SetOut(new StringWriter(consoleOutput));
        }

        Console.WriteLine("==========     TESTS REPORT   ==========");
        Console.WriteLine("\n--- UNIT TESTS REPORT");
        UnitTests.RunTests();

        Console.WriteLine();
        Console.WriteLine("\n--- INTEGRATION TESTS REPORT");
        IntegrationTests.RunScript();
        
        Console.WriteLine("\n==========     END REPORT   ==========");

        if (generateReport)
        {
            Console.SetOut(originalConsole);

            string reportPath = "./test_report.txt";

            using (StreamWriter writer = new StreamWriter(reportPath, false, Encoding.UTF8))
            {
                // writer.WriteLine("TESTS RAPPORT");
                // writer.WriteLine();
                writer.Write(consoleOutput.ToString());
            }

            Console.WriteLine($"Report saved at: {reportPath}");
        }
    }
}

