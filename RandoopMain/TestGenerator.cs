using RandoopMain.Util;
using System.Text;

namespace RandoopMain
{
    public class TestGenerator
    {
        public static void GenerateTests(string dllPath, string outputPath)
        {
            var reflectedClasses = DllCollector.Collect(dllPath);
            var sb = new StringBuilder();
            var ignoreHandler = new ignoreHandler();
            var testCount = 0;

            // Loop through each collected class
            foreach (var rClass in reflectedClasses)
            {
                // Skip compiler-generated
                if (rClass.SimpleName.StartsWith("<"))
                {
                    continue;
                }

                // Skip generic
                if (rClass.FullName.Contains("`"))
                {
                    continue;
                }

                foreach (var method in rClass.Methods)
                {

                    // Skip compiler-generated
                    if (method.Name.StartsWith("<"))
                    {
                        continue;
                    }

                    // Skips generic (template)
                    if (method.Name.Contains("`"))
                    {
                        continue;
                    }

                    // Skip Abstract Methods
                    if (!rClass.CanInstantiate && !method.IsStatic)
                    {
                        continue;
                    }

                    // Skip methods with ref/out/params for now
                    if (method.ParameterTypes.Any(t => t.IsByRef || t.IsArray))
                    {
                        continue;
                    }

                    // Skip ignored methods
                    if (ignoreHandler.ShouldIgnore(rClass.SimpleName, method.Name))
                    {
                        continue;
                    }


                    // Generate unique method name using parameter types to prevent name collision from overloads
                    string paramSuffix = string.Join("_", method.ParameterTypes.Select(t => t.Name));
                    string testName;
                    if (string.IsNullOrEmpty(paramSuffix))
                    {
                        testName = $"Test_{rClass.SimpleName}_{method.Name}";
                    }
                    else
                    {
                        testName = $"Test_{rClass.SimpleName}_{method.Name}_{paramSuffix}";
                    }

                    string test = GenerateTestFileContent(rClass, method, testName);
                    string outputdir = outputPath + $"Test{testCount}.cs";
                    File.WriteAllText(outputPath, test.ToString());
                    testCount++;



                    Console.WriteLine("Test files generated at: " + outputPath);
                }
            }
        }

        private static string GetDummyArg(Type type)
        {
            if (type == typeof(int)) return "0";
            if (type == typeof(double)) return "0.0";
            if (type == typeof(string)) return "\"test\"";
            if (type == typeof(bool)) return "false";
            if (type == typeof(object)) return "new object()"; // If the parameter is an object, return a new object instance
            if (type.IsValueType) return $"default({type.Name})"; // If it's any other value type (like a struct), return its default value
            return "null"; //  when the parameter type is a reference type
        }

        private static string GenerateTestFileContent(ReflectedClass rClass, ReflectedMethod method, string testName)
        {
            var typeName = rClass.FullName;
            var sb = new StringBuilder();

            sb.AppendLine("using Xunit;");
            sb.AppendLine();
            sb.AppendLine("namespace GeneratedTests");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {rClass.SimpleName}_Tests");
            sb.AppendLine("    {");
            sb.AppendLine("        [Fact]");
            sb.AppendLine($"        public void {testName}()");
            sb.AppendLine("        {");

            // Create instance if needed
            object instance = null;
            if (!method.IsStatic && rClass.CanInstantiate)
            {
                instance = Activator.CreateInstance(rClass.Type);
                sb.AppendLine($"            var instance = new {typeName}();");
            }

            // Create dummy arguments
            object[] dummyValues = method.ParameterTypes.Select(GetDummyArg).ToArray();
            string callArgs = string.Join(", ", method.ParameterTypes.Select(GetDummyArg));

            // Invoke the method and get result
            object result = null;
            try
            {
                if (method.IsStatic)
                {
                    result = method.MethodInfo.Invoke(null, dummyValues);
                }
                else
                {
                    result = method.MethodInfo.Invoke(instance, dummyValues);
                }
            }
            catch (Exception e)
            {
                sb.AppendLine($"            // Skipped due to exception: {e.GetType().Name}: {e.Message}");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");
                return "";
            }

            // Format the result
            string expected = GetLiteralFromValue(result);
            string callExpr = "";
            if (method.IsStatic)
            {
                callExpr = $"{typeName}.{method.Name}({callArgs})";
            }
            else
            {
                callExpr = $"instance.{method.Name}({callArgs})";
            }

            sb.AppendLine($"            var result = {callExpr};");
            sb.AppendLine($"            Assert.Equal({expected}, result);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GetLiteralFromValue(object value)
        {
            if (value == null) return "null";
            if (value is string s) return $"\"{s}\"";
            if (value is char c) return $"'{c}'";
            if (value is bool b) return b ? "true" : "false";
            if (value is double d) return d.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            if (value is float f) return f.ToString("G", System.Globalization.CultureInfo.InvariantCulture) + "f";
            if (value is int or long or short or byte) return value.ToString();
            return $"/* unsupported result type: {value.GetType().Name} */";
        }
    }
}
