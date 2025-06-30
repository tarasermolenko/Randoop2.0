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
            //string outputPath = "C:\\Users\\Taras\\source\\repos\\Randoop\\Tests\\GeneratedTests.cs";

            // Call the test generator
            // TestGenerator.GenerateTests(dllPath, outputPath);

            //Console.WriteLine($"Tests generated and saved to {outputPath}");
            List<TestResult> results = TestRunner.RunTests(dllPath);

            foreach (var result in results)
            {
                string paramList;
                if (result.ParameterValues.Count > 0)
                {
                    paramList = "(Parameters: " + string.Join(", ", result.ParameterValues) + ")";
                }
                else
                {
                    paramList = "(No parameters)";
                }

                string returnInfo = "";
                if (result.ReturnType != null && result.ReturnType != "")
                {
                    returnInfo = " (Returns: " + result.ReturnType;
                    if (result.ReturnValue != null && result.ReturnValue != "")
                    {
                        returnInfo += " = " + result.ReturnValue;
                    }
                    returnInfo += ")";
                }

                string failureInfo = "";
                if (result.Outcome == "FAIL" || result.Outcome == "SKIP")
                {
                    if (result.FailureReason != null && result.FailureReason != "")
                    {
                        failureInfo = "- " + result.FailureReason;
                    }
                }

                Console.WriteLine("[" + result.Outcome + "] " + result.ClassName + "." + result.MethodName + " " + paramList + returnInfo + " " + failureInfo);
            }

        }
    }
}