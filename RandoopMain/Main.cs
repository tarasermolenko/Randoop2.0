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

            DllInspector.Inspect(dllPath);

            // Define output path you want the test to be generated
            string outputPath = "C:\\Users\\birfa\\Source\\Repos\\Randoop2.0\\TestLibrary.Tests\\Tests";

            // Call the test generator
            TestGenerator.GenerateTests(dllPath, outputPath);

            Console.WriteLine($"Tests generated and saved to {outputPath}");
        }
    }
}