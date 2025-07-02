namespace RandoopMain
{
    public class Program
    {
        // Entry for our console app that will take in a .Dll to analyze
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: RandoopReimp <path-to-dll>");
                return;
            }

            string dllPath = args[0];

            //DllInspector.Inspect(dllPath);

            // Define output path you want the test to be generated
            string outputPath = "C:\\Users\\birfa\\Source\\Repos\\Randoop2.0\\TestLibrary.Tests\\Tests";

            // Call the test generator
            // TestGenerator.GenerateTests(dllPath, outputPath);

            //Console.WriteLine($"Tests generated and saved to {outputPath}");
            TestRunner runner = new TestRunner();

            List<TestResult> results = runner.RunTests(dllPath);
            int testCount = 0;
            foreach (var result in results)
            {
                string paramList = result.ParameterValues.Count > 0
                    ? $"(Parameters: {string.Join(", ", result.ParameterValues)})"
                    : "(No parameters)";

                Console.WriteLine($"[{result.Outcome}] {result.ClassName}.{result.MethodName} {paramList} " +
                                  $"{(result.Outcome == "FAIL" || result.Outcome == "SKIP" ? "- " + result.FailureReason : "")}");
                if(result.Outcome == "PASS")
                {
                    string outputFilePath = Path.Combine(outputPath, $"TestV{testCount}.cs"); // Safe join
                    Directory.CreateDirectory(outputPath); // Ensure folder exists
                    File.WriteAllText(outputFilePath, result.constructedTest);
                    testCount++;
                }
                
            }

        }
    }
}