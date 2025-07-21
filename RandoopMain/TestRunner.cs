using System.Reflection;
using System.Text;


namespace RandoopMain
{

    public class TestRunner
    {

        private static Random random = new();
        public List<TestVariables> testVariables = [];
        public List<string> instructions = [];

        public List<TestResult> RunTests(string dllPath)
        {
            int maxTests = 50;
            int maxAttempts = 100; // infinite loops safeguard

            var classes = DllCollector.Collect(dllPath);

            var results = new List<TestResult>();

            List<MethodToTest> methodToTest = new List<MethodToTest>();
            foreach (var rClass in classes)
            {
                foreach (var rMethod in rClass.Methods)
                {
                    methodToTest.Add(new MethodToTest(rClass, rMethod));
                }
            }

            if(methodToTest.Count == 0) return results;

            int methodCount = methodToTest.Count;
            int testCount = 0;
            int attemptCount = 0;

            for (int i = 0; i < maxTests && attemptCount < maxAttempts;)
            {
                int testMethodIndex = random.Next(0, methodCount);
                TestResult TResult = makeTest(methodToTest[testMethodIndex]);
                if(TResult.Outcome == "PASS")
                {
                    TResult = writeTests(TResult, methodToTest[testMethodIndex], testCount);
                    i++;
                    testCount++;
                }
                results.Add(TResult);
                attemptCount++;

            }

            return results;
        }

        private TestResult writeTests(TestResult test, MethodToTest methodToTest, int testNumber)
        {
            var rClass = methodToTest.Class;
            var rMethod = methodToTest.method;
            var method = rMethod.Method;

            var sb = new StringBuilder();

            string testName = $"Test_{rClass.SimpleName}_{method.Name}_{testNumber.ToString()}";

            sb.AppendLine("using Xunit;");
            sb.AppendLine();
            sb.AppendLine("namespace GeneratedTests");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {rClass.SimpleName}_Test{testNumber.ToString()}");
            sb.AppendLine("    {");
            sb.AppendLine("        [Fact]");
            sb.AppendLine($"        public void {testName}()");
            sb.AppendLine("        {");

            sb.AppendLine(test.constructedTest);


            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            test.constructedTest = sb.ToString();

            return test;
        }
        
        private TestResult makeTest(MethodToTest methodToTest)
        {
            var result = new TestResult();

            var rClass = methodToTest.Class;
            var rMethod = methodToTest.method;
            var type = rClass.Type;
            var method = rMethod.Method;
            if (type.Name.StartsWith("<") || type.ContainsGenericParameters)
            {
                result.ClassName = rClass.SimpleName;
                result.MethodName = method.Name;
                result.Outcome = "SKIP";
                result.FailureReason = "Generic Types not supported";
                return result;
            }

            if (method.Name.StartsWith("<") || method.ContainsGenericParameters || !method.IsPublic)
            {
                result.ClassName = rClass.SimpleName;
                result.MethodName = method.Name;
                result.Outcome = "SKIP";
                result.FailureReason = "Generic Types not supported";
                return result;
            }

            var parameters = method.GetParameters();
            string paramSuffix = string.Join("_", parameters.Select(t => t.Name));
            string testName;
            if (string.IsNullOrEmpty(paramSuffix)){
                testName = $"Test_{rClass.SimpleName}_{method.Name}";
            }
            else{
                testName = $"Test_{rClass.SimpleName}_{method.Name}_{paramSuffix}";
            }

            var typeName = rClass.FullName;
            var sb = new StringBuilder();

                    //=====================================================================
            try{
                object? instance = null;
                string instanceArgs = "";

                if (!rMethod.IsStatic)
                {
                    if (!rClass.CanInstantiate)
                    {
                        result.ClassName = rClass.SimpleName;
                        result.MethodName = method.Name;
                        result.Outcome = "SKIP";
                        result.FailureReason = "no public constructor";
                        return result;
                    }
                    (instance, instanceArgs) = CreateInstanceWithDummyArgs(type);
                    //========================================================================
                    instructions.Add($"var instance = new {type.FullName}({instanceArgs})");
                    //========================================================================
                }


                if (instance == null)
                {
                    result.ClassName = rClass.SimpleName;
                    result.MethodName = method.Name;
                    result.Outcome = "SKIP";
                    result.FailureReason = "could not instantiate with dummy arguments";
                    return result;
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

                var paramStringsformated = args.Select((arg, i) => FormatArgForCode(arg, parameters[i].ParameterType, parameters[i])).ToList();

                        
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
                        
                if (method.IsStatic){        
                    callExpr = $"{typeName}.{method.Name}({string.Join(",", paramStringsformated)})";
                }
                else{        
                    callExpr = $"instance.{method.Name}({string.Join(",", paramStringsformated)})";
                }



                        // Check return value matches return type
                var returnType = method.ReturnType;
                        //===================================================================================
                foreach (var vari in testVariables)
                {
                    sb.AppendLine($"            {vari.construction};");        
                }



                if (method.ReturnType == typeof(void))
                {
                    instructions.Add($"{callExpr}");
                }
                else{     
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
                        result.ClassName = rClass.SimpleName;
                        result.MethodName = method.Name;
                        result.Outcome = "FAIL";
                        result.FailureReason = $"Method returned null but return type is non-nullable {returnType.Name}";
                        result.ParameterValues = paramStrings;
                        return result;

                    }
                    // Return value type mismatch
                    if (returnValue != null && !returnType.IsAssignableFrom(returnValue.GetType()))
                    {
                        result.ClassName = rClass.SimpleName;
                        result.MethodName = method.Name;
                        result.Outcome = "FAIL";
                        result.FailureReason = $"Return value type {returnValue.GetType().Name} does not match method return type {returnType.Name}";
                        result.ParameterValues = paramStrings;
                        return result;
                    }

                            // Check for NaN or Infinity if return type is float or double
                            
                    if (returnType == typeof(float))
                    {
                        float floatValue = (float)returnValue!;
                        if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                        {
                            result.ClassName = rClass.SimpleName;
                            result.MethodName = method.Name;
                            result.Outcome = "FAIL";
                            result.FailureReason = "Returned float.NaN or Infinity";
                            result.ParameterValues = paramStrings;
                            return result;
                        }
                    }
                    else if (returnType == typeof(double))
                    {
                        double doubleValue = (double)returnValue!;
                        if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                        {
                            result.ClassName = rClass.SimpleName;
                            result.MethodName = method.Name;
                            result.Outcome = "FAIL";
                            result.FailureReason = "Returned double.NaN or Infinity";
                            result.ParameterValues = paramStrings;
                            return result;
                        }
                    }
                }


                        // Passed all checks
                        //=======================================================================
                        
                testVariables = [];
                instructions = [];
                //========================================================================

                result.ClassName = rClass.SimpleName;
                result.MethodName = method.Name;
                result.Outcome = "PASS";
                result.ParameterValues = paramStrings;
                result.constructedTest = sb.ToString();
                return result;

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

                result.ClassName = rClass.SimpleName;
                result.MethodName = method.Name;
                result.Outcome = "FAIL";
                result.FailureReason = failureReason;
                return result;
            }
            catch (Exception ex)
            {
                result.ClassName = rClass.SimpleName;
                result.MethodName = method.Name;
                result.Outcome = "FAIL";
                result.FailureReason = ex.Message;
                return result;
            }
        }
        private object[] PrepareArguments(ParameterInfo[] parameters)
        {
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];

                if (p.IsOut)
                {
                    var argType = p.ParameterType.GetElementType()!;
                    args[i] = GetDefault(argType);

                    var variableName = $"v{testVariables.Count}";
                    var constructionCode = $"{argType} {variableName}";

                    var tempvariable = new TestVariables(argType, args[i], constructionCode)
                    {
                        name = variableName,
                        isOut = true
                    };

                    testVariables.Add(tempvariable);
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
            string name = $"v{testVariables.Count}";

            if (type == typeof(int))
            {
                int value = random.Next(-1000, 1000);
                string construction = $"int {name} = {value}";
                testVariables.Add(new TestVariables(type, value, construction) { name = name });
                return value;
            }

            if (type == typeof(double))
            {
                double value = random.NextDouble() * (random.Next(0, 2) == 0 ? 1 : -1) * 1000;
                string construction = $"double {name} = {value}";
                testVariables.Add(new TestVariables(type, value, construction) { name = name });
                return value;
            }

            if (type == typeof(float))
            {
                return (float)(random.NextDouble() * (random.Next(0, 2) == 0 ? 1 : -1) * 1000);
            }

            if (type == typeof(string))
            {
                string value = GenerateRandomString();
                string formatted = $"\"{value.Replace("\"", "\\\"")}\"";
                string construction = $"string {name} = {formatted}";
                testVariables.Add(new TestVariables(type, value, construction) { name = name });
                return value;
            }

            if (type == typeof(bool))
            {
                bool value = random.Next(2) == 0;
                string construction = $"bool {name} = {(value ? "true" : "false")}";
                testVariables.Add(new TestVariables(type, value, construction) { name = name });
                return value;
            }

            if (type == typeof(object))
            {
                object value = new object();
                string construction = $"object {name} = new object();";
                testVariables.Add(new TestVariables(type, value, construction) { name = name });
                return value;
            }

            if (type.IsValueType)
            {
                object? instance = Activator.CreateInstance(type);
                if (instance == null)
                    throw new InvalidOperationException($"Could not create instance of value type {type.FullName}.");

                string construction = $"{type.FullName} {name} = new {type.FullName}();";
                testVariables.Add(new TestVariables(type, instance, construction) { name = name });
                return instance;
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var arrayInstance = Array.CreateInstance(elementType, 1);
                arrayInstance.SetValue(GetDummyArg(elementType), 0);

                var elementStrings = new List<string>();
                for (int i = 0; i < arrayInstance.Length; i++)
                {
                    elementStrings.Add(FormatArgForCode(arrayInstance.GetValue(i), elementType));
                }

                string construction = $"{elementType.FullName}[] {name} = new {elementType.FullName}[] {{ {string.Join(", ", elementStrings)} }};";
                testVariables.Add(new TestVariables(type, arrayInstance, construction) { name = name });
                return arrayInstance;
            }

            // For unsupported reference types, return null
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