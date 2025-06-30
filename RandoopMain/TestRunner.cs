using System.Reflection;

namespace RandoopMain
{
    public class TestResult
    {
        public string ClassName { get; set; } = "";
        public string MethodName { get; set; } = "";
        public string Outcome { get; set; } = ""; // "PASS", "FAIL", "SKIP"
        public string? FailureReason { get; set; }
        public List<string> ParameterValues { get; set; } = new();
        public string? ReturnValue { get; set; }
        public string? ReturnType { get; set; }
    }

    public class TestRunner
    {
        public static List<TestResult> RunTests(string dllPath)
        {
            var classes = DllCollector.Collect(dllPath);

            int total = 0, passed = 0, failed = 0, skipped = 0;
            var results = new List<TestResult>();

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

                    var parameters = method.GetParameters();

                    total++;

                    try
                    {
                        object? instance = null;

                        if (!rMethod.IsStatic)
                        {
                            if (!rClass.CanInstantiate)
                            {
                                results.Add(new TestResult
                                {
                                    ClassName = rClass.SimpleName,
                                    MethodName = method.Name,
                                    Outcome = "SKIP",
                                    FailureReason = "no public constructor",
                                    ReturnType = method.ReturnType.Name,
                                    ReturnValue = null
                                });
                                skipped++;
                                continue;
                            }

                            instance = CreateInstanceWithDummyArgs(type);
                            if (instance == null)
                            {
                                results.Add(new TestResult
                                {
                                    ClassName = rClass.SimpleName,
                                    MethodName = method.Name,
                                    Outcome = "SKIP",
                                    FailureReason = "could not instantiate with dummy arguments",
                                    ReturnType = method.ReturnType.Name,
                                    ReturnValue = null
                                });
                                skipped++;
                                continue;
                            }
                        }

                        // Prepare arguments, including ref/out/params support
                        object[] args = PrepareArguments(parameters);

                        // Capture parameter values as strings
                        var paramStrings = new List<string>();
                        foreach (var arg in args)
                        {
                            if (arg == null)
                            {
                                paramStrings.Add("null");
                            }
                            else
                            {
                                string? str = arg.ToString();
                                if (str == null)
                                {
                                    paramStrings.Add("null");
                                }
                                else
                                {
                                    paramStrings.Add(str);
                                }
                            }
                        }

                        object? returnValue = method.Invoke(instance, args);

                        string? returnValueString = (returnValue == null) ? "null" : returnValue.ToString();

                        var returnType = method.ReturnType;

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
                                    ParameterValues = paramStrings,
                                    ReturnType = returnType.Name,
                                    ReturnValue = returnValueString
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
                                    ParameterValues = paramStrings,
                                    ReturnType = returnType.Name,
                                    ReturnValue = returnValueString
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
                                        ParameterValues = paramStrings,
                                        ReturnType = returnType.Name,
                                        ReturnValue = returnValueString
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
                                        ParameterValues = paramStrings,
                                        ReturnType = returnType.Name,
                                        ReturnValue = returnValueString
                                    });
                                    failed++;
                                    continue;
                                }
                            }
                        }

                        // Passed all checks
                        results.Add(new TestResult
                        {
                            ClassName = rClass.SimpleName,
                            MethodName = method.Name,
                            Outcome = "PASS",
                            ParameterValues = paramStrings,
                            ReturnType = returnType.Name,
                            ReturnValue = returnValueString
                        });
                        passed++;
                    }
                    catch (TargetInvocationException ex)
                    {
                        string failureReason = ex.InnerException != null ? ex.InnerException.GetType().Name : "Exception";

                        results.Add(new TestResult
                        {
                            ClassName = rClass.SimpleName,
                            MethodName = method.Name,
                            Outcome = "FAIL",
                            FailureReason = failureReason,
                            ReturnType = rMethod.Method.ReturnType.Name,
                            ReturnValue = null
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
                            FailureReason = ex.Message,
                            ReturnType = rMethod.Method.ReturnType.Name,
                            ReturnValue = null
                        });
                        failed++;
                    }
                }
            }

            // summary
            System.Console.WriteLine($"\nSummary: {passed}/{total} passed, {failed} failed, {skipped} skipped.");

            return results;
        }

        private static object[] PrepareArguments(ParameterInfo[] parameters)
        {
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];

                if (p.IsOut)
                {
                    args[i] = GetDefault(p.ParameterType.GetElementType() ?? p.ParameterType);
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

        private static object GetDummyArg(Type type)
        {
            if (type == typeof(int)) return 0;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(float)) return 0.0f;
            if (type == typeof(string)) return "test";
            if (type == typeof(bool)) return false;
            if (type == typeof(object)) return new object();

            if (type.IsValueType)
            {
                var instance = Activator.CreateInstance(type);
                if (instance == null)
                    throw new InvalidOperationException($"Could not create instance of value type {type.FullName}.");
                return instance;
            }

            // For reference types
            return null!;
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

        private static object? CreateInstanceWithDummyArgs(Type type)
        {
            var constructors = type.GetConstructors()
                                   .OrderBy(c => c.GetParameters().Length);

            foreach (var ctor in constructors)
            {
                var paramInfos = ctor.GetParameters();
                try
                {
                    var args = paramInfos.Select(p => GetDummyArg(p.ParameterType)).ToArray();
                    return ctor.Invoke(args);
                }
                catch
                {
                    // try next ctor
                }
            }
            return null;
        }
    }
}