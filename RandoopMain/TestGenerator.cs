using RandoopMain.Util;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RandoopMain
{
    public class TestGenerator
    {
        /*
        public static void GenerateTests(string dllPath, string outputPath)
        {
            var reflectedClasses = DllCollector.Collect(dllPath);
            var sb = new StringBuilder();
            var ignoreHandler = new ignoreHandler();
            var testCount = 0;

            // Loop through each collected class
            foreach (var rClass in reflectedClasses)
            {
                int classTestnumber = 1;
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

                    string test = GenerateTestFileContent(rClass, method, testName, classTestnumber);
                    classTestnumber++;
                    string outputFilePath = Path.Combine(outputPath, $"Test{testCount}.cs"); // Safe join
                    Directory.CreateDirectory(outputPath); // Ensure folder exists
                    File.WriteAllText(outputFilePath, test); // Write actual test content to file
                    testCount++;



                    Console.WriteLine("Test files generated at: " + outputPath);
                }
            }
        }


        private static string GenerateTestFileContent(ReflectedClass rClass, ReflectedMethod method, string testName,int number)
        {
            var typeName = rClass.FullName;
            var sb = new StringBuilder();

            sb.AppendLine("using Xunit;");
            sb.AppendLine();
            sb.AppendLine("namespace GeneratedTests");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {rClass.SimpleName}_Test{number}");
            sb.AppendLine("    {");
            sb.AppendLine("        [Fact]");
            sb.AppendLine($"        public void {testName}()");
            sb.AppendLine("        {");

            // Create instance if needed
            object instance = null;
            if (!method.IsStatic && rClass.CanInstantiate)
            {
                ConstructorInfo? ctor = rClass.Type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(c => c.GetParameters().Length)
                .FirstOrDefault();

                if (ctor == null)
                {
                    return $"// Skipped {testName}: no usable constructor for {rClass.SimpleName}";
                }

                // Generate dummy values
                var ctorParams = ctor.GetParameters();
                var ctorArgs = ctorParams.Select(p => GetDummyArgValue(p.ParameterType)).ToArray();
                var ctorArgLiterals = ctorParams.Select(p => GetDummyArg(p.ParameterType)).ToArray();
                instance = ctor.Invoke(ctorArgs);
                // instance = Activator.CreateInstance(rClass.Type);
                sb.AppendLine($"            var instance = new {typeName}({string.Join(", ", ctorArgLiterals)});");
            }

            // Create dummy arguments
            object[] dummyValues = method.ParameterTypes.Select(GetDummyArgValue).ToArray();
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
                Console.WriteLine($"[Skipped Test] {testName}: {e.GetType().Name} - {e.Message}");
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
            if(method.MethodInfo.ReturnType == typeof(void))
            {
                sb.AppendLine($"            {callExpr};");
            }
            else
            {
                sb.AppendLine($"            var result = {callExpr};");
                sb.AppendLine($"            Assert.Equal({expected}, result);");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
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

        private static object GetDummyArgValue(Type type)
        {
            if (type == typeof(int)) return 0;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(string)) return "test";
            if (type == typeof(bool)) return false;
            if (type == typeof(char)) return 'a';
            if (type == typeof(float)) return 0f;
            if (type == typeof(long)) return 0L;
            if (type == typeof(byte)) return (byte)0;
            if (type == typeof(short)) return (short)0;
            if (type == typeof(decimal)) return 0m;
            if (type == typeof(DateTime)) return DateTime.Now;
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0)!;
            if (type.IsValueType) return Activator.CreateInstance(type)!;
            return null!;
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
            return $"/* unsupported result type: {value.GetType().Name} ";*/
    }
    
}


