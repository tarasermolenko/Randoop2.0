namespace RandoopMain
{
    public class TestResult
    {
        public string ClassName { get; set; } = "";
        public string MethodName { get; set; } = "";
        public string Outcome { get; set; } = ""; // "PASS", "FAIL", "SKIP"
        public string? FailureReason { get; set; }
        public List<string> ParameterValues { get; set; } = new();
        public string constructedTest { get; set; } = "";
    }
}