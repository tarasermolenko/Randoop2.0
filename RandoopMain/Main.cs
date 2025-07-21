namespace RandoopMain
{
    public class Program
    {
        // Entry for our console app that will take in a DLL to analyze
        public static void Main(string[] args)
        {
            // Check if at least the DLL path is provided
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Usage: RandoopReimp <path-to-dll> [output-directory]");
                return;
            }

            string dllPath = args[0];

            // Use second argument if provided; otherwise use default output path
            string outputPath;

            if (args.Length >= 2)
            {
                outputPath = args[1];
            }
            else
            {
                outputPath = Path.Combine("TestLibrary.Tests", "Tests");
            }

            TestRunner runner = new TestRunner();
            List<TestResult> results = runner.RunTests(dllPath);

            int testCount = 0;

            foreach (var result in results)
            {
                string paramList;

                if (result.ParameterValues.Count > 0)
                {
                    paramList = $"(Parameters: {string.Join(", ", result.ParameterValues)})";
                }
                else
                {
                    paramList = "(No parameters)";
                }

                Console.WriteLine($"[{result.Outcome}] {result.ClassName}.{result.MethodName} {paramList} " +
                                  $"{(result.Outcome == "FAIL" || result.Outcome == "SKIP" ? "- " + result.FailureReason : "")}");

                if (result.Outcome == "PASS")
                {
                    string outputFilePath = Path.Combine(outputPath, $"TestV{testCount}.cs");
                    Directory.CreateDirectory(outputPath); // Ensure the folder exists
                    File.WriteAllText(outputFilePath, result.constructedTest);
                    testCount++;
                }
            }

            Console.WriteLine($"\n Generated {testCount} passing tests in: {Path.GetFullPath(outputPath)}");
        }
    }
}