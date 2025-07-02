using System;
using System.Reflection;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

    public class TestVariables
    {
        public string name { get; set; } = "";
        public Type type;
        public object instance;
        public string construction;
        public bool isOut = false;
    }



    public class TestRunner
    {

        private static Random random = new();
        public List<TestVariables> testVariables = [];
        public List<string> instructions = [];

        public List<TestResult> RunTests(string dllPath)
        {
            var classes = DllCollector.Collect(dllPath);

            int total = 0, passed = 0, failed = 0, skipped = 0;
            var results = new List<TestResult>();

            int testCount = 0;

            foreach (var rClass in classes)
            {
                var type = rClass.Type;

                if (type.Name.StartsWith("<") || type.ContainsGenericParameters)
                    continue;

                foreach (var rMethod in rClass.Methods)
                {
                    var method = rMethod.Method;

                    if (method.Name.StartsWith("<") || method.ContainsGenericParameters)
                        continue;
                    //==================================================================================
                    var parameters = method.GetParameters();

                    string paramSuffix = string.Join("_", parameters.Select(t => t.Name));
                    string testName;
                    if (string.IsNullOrEmpty(paramSuffix))
                    {
                        testName = $"Test_{rClass.SimpleName}_{method.Name}";
                    }
                    else
                    {
                        testName = $"Test_{rClass.SimpleName}_{method.Name}_{paramSuffix}";
                    }

                    var typeName = rClass.FullName;
                    var sb = new StringBuilder();

                    sb.AppendLine("using Xunit;");
                    sb.AppendLine();
                    sb.AppendLine("namespace GeneratedTests");
                    sb.AppendLine("{");
                    sb.AppendLine($"    public class {rClass.SimpleName}_Test{testCount}");
                    sb.AppendLine("    {");
                    sb.AppendLine("        [Fact]");
                    sb.AppendLine($"        public void {testName}()");
                    sb.AppendLine("        {");


                    //=====================================================================
                    total++;

                    try
                    {
                        object? instance = null;
                        string instanceArgs = "";

                        if (!rMethod.IsStatic)
                        {
                            if (!rClass.CanInstantiate)
                            {
                                results.Add(new TestResult
                                {
                                    ClassName = rClass.SimpleName,
                                    MethodName = method.Name,
                                    Outcome = "SKIP",
                                    FailureReason = "no public constructor"
                                });
                                skipped++;
                                continue;
                            }

                            (instance, instanceArgs) = CreateInstanceWithDummyArgs(type);
                            //========================================================================
                            instructions.Add($"var instance = new {type.FullName}({instanceArgs})");
                            //========================================================================
                            if (instance == null)
                            {
                                results.Add(new TestResult
                                {
                                    ClassName = rClass.SimpleName,
                                    MethodName = method.Name,
                                    Outcome = "SKIP",
                                    FailureReason = "could not instantiate with dummy arguments"
                                });
                                skipped++;
                                continue;
                            }
                        }

                        // Prepare arguments, including ref/out/params support
                        object[] args = PrepareArguments(parameters);

                        // Capture parameter values as strings
                        var paramStrings = args.Select(arg =>
                        {
                            if (arg == null)
                                return "null";
                            else
                            {
                                var str = arg.ToString();
                                return str ?? "null";
                            }
                        }).ToList();

                        // Invoke method and get return value
                        object? returnValue = method.Invoke(instance, args);

                        var paramStringsformated = args
                        .Select((arg, i) => FormatArgForCode(arg, parameters[i].ParameterType, parameters[i]))
                        .ToList();

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var p = parameters[i];

                            if (p.IsOut)
                            {
                                paramStringsformated[i] = "out " + paramStringsformated[i];
                                continue;
                            }

                            if (p.ParameterType.IsByRef)
                            {
                                paramStringsformated[i] = "ref " + paramStringsformated[i];
                                continue;
                            }
                        }


                            string callExpr = "";
                        if (method.IsStatic)
                        {
                            callExpr = $"{typeName}.{method.Name}({string.Join(",", paramStringsformated)})";
                        }
                        else
                        {
                            callExpr = $"instance.{method.Name}({string.Join(",", paramStringsformated)})";
                        }
                        


                        // Check return value matches return type
                        var returnType = method.ReturnType;
                        //===================================================================================
                        foreach(var vari in testVariables)
                        {
                            sb.AppendLine($"            {vari.construction};");
                            
                                }



                        if (method.ReturnType == typeof(void))
                        {
                            instructions.Add($"{callExpr}");
                        }
                        else
                        {
                            instructions.Add($"var result = {callExpr}");
                            string formattedReturnValue = FormatArgForCode(returnValue, returnType);
                            instructions.Add($"Assert.Equal({formattedReturnValue}, result)");
                        }

                        foreach (var instruction in instructions)
                        {
                            sb.AppendLine($"            {instruction};");
                        }

                        //===================================================================================
                        if (returnType != typeof(void))
                        {
                            // Null returned for non-nullable value type
                            if (returnValue == null && returnType.IsValueType && Nullable.GetUnderlyingType(returnType) == null)
                            {
                                results.Add(new TestResult
                                {
                                    ClassName = rClass.SimpleName,
                                    MethodName = method.Name,
                                    Outcome = "FAIL",
                                    FailureReason = $"Method returned null but return type is non-nullable {returnType.Name}",
                                    ParameterValues = paramStrings
                                });
                                failed++;
                                continue;
                            }

                            // Return value type mismatch
                            if (returnValue != null && !returnType.IsAssignableFrom(returnValue.GetType()))
                            {
                                results.Add(new TestResult
                                {
                                    ClassName = rClass.SimpleName,
                                    MethodName = method.Name,
                                    Outcome = "FAIL",
                                    FailureReason = $"Return value type {returnValue.GetType().Name} does not match method return type {returnType.Name}",
                                    ParameterValues = paramStrings
                                });
                                failed++;
                                continue;
                            }

                            // Check for NaN or Infinity if return type is float or double
                            if (returnType == typeof(float))
                            {
                                float floatValue = (float)returnValue!;
                                if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                                {
                                    results.Add(new TestResult
                                    {
                                        ClassName = rClass.SimpleName,
                                        MethodName = method.Name,
                                        Outcome = "FAIL",
                                        FailureReason = "Returned float.NaN or Infinity",
                                        ParameterValues = paramStrings
                                    });
                                    failed++;
                                    continue;
                                }
                            }
                            else if (returnType == typeof(double))
                            {
                                double doubleValue = (double)returnValue!;
                                if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                                {
                                    results.Add(new TestResult
                                    {
                                        ClassName = rClass.SimpleName,
                                        MethodName = method.Name,
                                        Outcome = "FAIL",
                                        FailureReason = "Returned double.NaN or Infinity",
                                        ParameterValues = paramStrings
                                    });
                                    failed++;
                                    continue;
                                }
                            }
                        }
                        

                        // Passed all checks
                        //=======================================================================
                        sb.AppendLine("        }");
                        sb.AppendLine("    }");
                        sb.AppendLine("}");
                        testVariables = [];
                        instructions = [];
                        //========================================================================

                        results.Add(new TestResult
                        {
                            ClassName = rClass.SimpleName,
                            MethodName = method.Name,
                            Outcome = "PASS",
                            ParameterValues = paramStrings,
                            constructedTest = sb.ToString()
                        });
                        passed++;
                        testCount++;
                    }
                    catch (TargetInvocationException ex)
                    {
                        string failureReason;
                        if (ex.InnerException != null)
                        {
                            failureReason = ex.InnerException.GetType().Name;
                        }
                        else
                        {
                            failureReason = "Exception";
                        }

                        results.Add(new TestResult
                        {
                            ClassName = rClass.SimpleName,
                            MethodName = method.Name,
                            Outcome = "FAIL",
                            FailureReason = failureReason
                        });
                        failed++;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new TestResult
                        {
                            ClassName = rClass.SimpleName,
                            MethodName = method.Name,
                            Outcome = "FAIL",
                            FailureReason = ex.Message
                        });
                        failed++;
                    }
                }
            }

            // summary
            System.Console.WriteLine($"\nSummary: {passed}/{total} passed, {failed} failed, {skipped} skipped.");

            return results;
        }

        private object[] PrepareArguments(ParameterInfo[] parameters)
        {
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];

                if (p.IsOut)
                {
                    args[i] = GetDefault(p.ParameterType.GetElementType() ?? p.ParameterType);
                    TestVariables tempvariable = new TestVariables();
                    tempvariable.type = p.ParameterType.GetElementType()!;
                    tempvariable.name = $"v{testVariables.Count}";
                    tempvariable.instance = args[i];
                    tempvariable.construction = $"{p.ParameterType.GetElementType()} {tempvariable.name}";
                    tempvariable.isOut = true ;
                    testVariables.Add( tempvariable );
                    continue;
                }

                if (p.ParameterType.IsByRef)
                {
                    var elementType = p.ParameterType.GetElementType()!;
                    args[i] = GetDummyArg(elementType);
                    continue;
                }

                var isParams = p.GetCustomAttributes(typeof(ParamArrayAttribute), false).Any();
                if (isParams)
                {
                    var elementType = p.ParameterType.GetElementType()!;
                    var arrayInstance = System.Array.CreateInstance(elementType, 1);
                    arrayInstance.SetValue(GetDummyArg(elementType), 0);
                    args[i] = arrayInstance;
                    continue;
                }

                args[i] = GetDummyArg(p.ParameterType);
            }

            return args;
        }

        private object GetDummyArg(Type type)
        {
            TestVariables tempvariable = new TestVariables();
            tempvariable.type = type;
            tempvariable.name = $"v{testVariables.Count}";
            if (type == typeof(int))
            {
                int vtemp = random.Next(-1000, 1000);
                tempvariable.instance = vtemp;
                tempvariable.construction = $"int {tempvariable.name} = {vtemp}";
                testVariables.Add(tempvariable);
                return vtemp;
            }

            if (type == typeof(double))
            {
                double vtemp = random.NextDouble() * (random.Next(0, 2) == 0 ? 1 : -1) * 1000;
                tempvariable.instance = vtemp;
                tempvariable.construction = $"double {tempvariable.name} = {vtemp}";
                testVariables.Add(tempvariable);
                return vtemp;
            }

            if (type == typeof(float))
                return (float)(random.NextDouble() * (random.Next(0, 2) == 0 ? 1 : -1) * 1000);

            if (type == typeof(string))
            {
                string vtemp = GenerateRandomString();
                string formated = $"\"{vtemp.Replace("\"", "\\\"")}\"";
                tempvariable.instance = vtemp;
                tempvariable.construction = $"string {tempvariable.name} = {formated}";
                testVariables.Add(tempvariable);
                return vtemp;
            }

            if (type == typeof(bool))
            {
                bool vtemp = random.Next(2) == 0;
                tempvariable.instance = vtemp;
                string formated = vtemp ? "true" : "false";
                tempvariable.construction = $"bool {tempvariable.name} = {formated}";
                testVariables.Add(tempvariable);
                return vtemp;
            }

            if (type == typeof(object)) 
            {
                object vtemp = new object();
                tempvariable.instance = vtemp;
                tempvariable.construction = $"object {tempvariable.name} = new object();";
                testVariables.Add(tempvariable);
                return vtemp;
            }

            if (type.IsValueType)
            {
                var instance = Activator.CreateInstance(type);
                if (instance == null)
                    throw new InvalidOperationException($"Could not create instance of value type {type.FullName}.");
                tempvariable.construction = $"{type.FullName} {tempvariable.name} = {type.FullName}();";
                tempvariable.instance = instance;
                testVariables.Add(tempvariable);
                return instance;
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var arrayInstance = Array.CreateInstance(elementType, 1);
                arrayInstance.SetValue(GetDummyArg(elementType), 0);

                tempvariable.instance = arrayInstance;
                string[] Args = [];
                for(int i = 0; i < arrayInstance.Length; i++)
                {
                    Args.Append(FormatArgForCode(arrayInstance.GetValue(i), elementType));
                }

                tempvariable.construction = $"{elementType.FullName}[] {tempvariable.name} = new {elementType.FullName}[] {{ {string.Join(",",Args)} }};";
                testVariables.Add(tempvariable);
                return arrayInstance;
            }

            // For reference types
            return null!;
        }

        private static string GenerateRandomString()
        {
            int length = random.Next(0, 15);
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(0,chars.Length)];
            }
            return new string(result);
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                var instance = Activator.CreateInstance(type);
                if (instance == null)
                    throw new InvalidOperationException($"Could not create instance of value type {type.FullName}.");
                return instance;
            }

            throw new NotSupportedException($"No default value for reference type {type.FullName}");
        }

        private (object? instance, string args) CreateInstanceWithDummyArgs(Type type)
        {
            var constructors = type.GetConstructors()
                                   .OrderBy(c => c.GetParameters().Length);

            foreach (var ctor in constructors)
            {
                var paramInfos = ctor.GetParameters();
                try
                {
                    var args = paramInfos.Select(p => GetDummyArg(p.ParameterType)).ToArray();
                    var instance = ctor.Invoke(args);
                    var argStrings = args.Select((arg, i) => FormatArgForCode(arg, paramInfos[i].ParameterType)).ToArray();
                    var stringArgs = string.Join(", ", argStrings);
                    return (instance, stringArgs);
                }
                catch
                {
                    // try next ctor
                }
            }
            return (null, "");
        }

        private string FormatArgForCode(object? arg, Type type, ParameterInfo? param = null)
        {

            if (arg == null)
                return "null";

            if (type.IsByRef)
            {
                var elementType = type.GetElementType()!;
                bool isOut = param?.IsOut == true;

                foreach (var vari in testVariables)
                {
                    if (vari.type != elementType)
                        continue;
                    if (isOut && vari.isOut)
                        return vari.name;

                    if (!isOut && !vari.isOut)
                        return vari.name;
                }

                return "null";
            }

            foreach (var vari in testVariables)
            {
                if(vari.type == type)
                {
                    if(vari.instance.Equals(arg))
                    {
                        return vari.name;
                    }
                }
            }

            if (type == typeof(string))
                return $"\"{arg.ToString()?.Replace("\"", "\\\"")}\"";

            if (type == typeof(char))
                return $"'{arg}'";

            if (type == typeof(bool))
                return ((bool)arg) ? "true" : "false";

            if (type == typeof(float))
                return $"{arg}f";

            if (type == typeof(double))
                return $"{arg}d";

            if (type == typeof(decimal))
                return $"{arg}m";

            if (type.IsEnum)
                return $"{type.FullName}.{arg}";

            if (type.IsPrimitive || type == typeof(decimal))
                return arg.ToString()!;

            if (type.IsValueType)
            {
                return $"new {type.FullName}()";
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var array = (Array)arg!;
                var elements = new List<string>();
                foreach (var item in array)
                {
                    elements.Add(FormatArgForCode(item, elementType));
                }
                return $"new {elementType.FullName}[] {{ {string.Join(", ", elements)} }}";
            }

            

            return "null2";
        }
    }
}