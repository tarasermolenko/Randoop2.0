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

                    var rClass = new ReflectedClass
                    {
                        Type = type,
                        CanInstantiate = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Any()
                    };

                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                    foreach (var method in methods)
                    {
                        if (method.IsSpecialName || method.DeclaringType == typeof(object))
                            continue;

                        var rMethod = new ReflectedMethod
                        {
                            Method = method
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
        public Type Type { get; set; } = null!;
        public bool CanInstantiate { get; set; }
        public List<ReflectedMethod> Methods { get; set; } = new();

        public string FullName => Type.FullName!;
        public string SimpleName => Type.Name;
        public bool IsStatic => Type.IsAbstract && Type.IsSealed;
    }

    public class ReflectedMethod
    {
        public MethodInfo Method { get; set; } = null!;

        public string Name => Method.Name;
        public bool IsStatic => Method.IsStatic;
        public List<Type> ParameterTypes => Method.GetParameters().Select(p => p.ParameterType).ToList();
    }
}
