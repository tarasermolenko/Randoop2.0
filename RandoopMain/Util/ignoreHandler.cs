namespace RandoopMain.Util
{
    internal class ignoreHandler
    {
        private Dictionary<string, HashSet<string>> ignoreMap = new();
        public ignoreHandler()
        {
            string filepath = Path.GetFullPath(Path.Combine("RandoopConfigs", "ignoreClasses.txt"));
            if (File.Exists(filepath))
            {
                LoadIgnoreList(File.ReadAllLines(filepath));
            }
            else
            {
                Console.WriteLine("File not found \n");
            }
        }

        private void LoadIgnoreList(IEnumerable<string> lines)
        {
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 2)
                    continue;

                string className = tokens[0];
                string methodName = tokens[1];

                if (!ignoreMap.ContainsKey(className))
                    ignoreMap[className] = new HashSet<string>();

                ignoreMap[className].Add(methodName);
            }
        }

        public bool ShouldIgnore(string className, string methodName)
        {
            if (ignoreMap.TryGetValue(className, out var methods))
            {
                return methods.Contains("*") || methods.Contains(methodName);
            }
            return false;
        }
    }
}
