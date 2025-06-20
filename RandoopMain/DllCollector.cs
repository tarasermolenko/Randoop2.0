using System.Reflection;

namespace RandoopMain
{
    public class DllCollector
    {
        public static List<ReflectedClass> Collect(string dllPath)
        {
            var reflectedClasses = new List<ReflectedClass>();

            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"DLL not found at: {dllPath}");
                return reflectedClasses;
            }

            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                var types = assembly.GetTypes().Where(t => t.IsClass);

                foreach (Type type in types)
                {
                    string fullName;
                    if (type.FullName != null)
                    {
                        fullName = type.FullName;
                    }
                    else
                    {
                        fullName = type.Name;
                    }


                    var rClass = new ReflectedClass
                    {

                        FullName = fullName,
                        SimpleName = type.Name,
                        IsStatic = type.IsAbstract && type.IsSealed,
                        CanInstantiate = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Any()
                    };

                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                    foreach (var method in methods)
                    {
                        // TODO: Improve filter to exclude compiler-generated types and methods
                        // Only include user-defined classes and methods that are not marked [CompilerGenerated] and do not contain '<' in names ?
                        if (method.IsSpecialName || method.DeclaringType == typeof(object))
                        {
                            continue;
                        }

                        var rMethod = new ReflectedMethod
                        {
                            Name = method.Name,
                            IsStatic = method.IsStatic,
                            ParameterTypes = method.GetParameters().Select(p => p.ParameterType).ToList()
                        };

                        rClass.Methods.Add(rMethod);
                    }

                    reflectedClasses.Add(rClass);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine("Error loading types:");
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    if (loaderException != null && loaderException.Message != null)
                    {
                        Console.WriteLine(loaderException.Message);
                    }
                    else
                    {
                        Console.WriteLine("(Unknown loader error)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load assembly: {ex.Message}");
            }

            return reflectedClasses;
        }
    }

    public class ReflectedClass
    {
        public string FullName { get; set; } = ""; // for path
        public string SimpleName { get; set; } = ""; // for naming
        public bool IsStatic { get; set; }
        public bool CanInstantiate { get; set; }
        public List<ReflectedMethod> Methods { get; set; } = new();
    }

    public class ReflectedMethod
    {
        public string Name { get; set; } = "";
        public bool IsStatic { get; set; }
        public List<Type> ParameterTypes { get; set; } = new();
    }
}
