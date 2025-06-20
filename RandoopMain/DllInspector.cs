using System.Reflection;

namespace RandoopMain
{
    public class DllInspector
    {
        // This is the logic of getting the information out of the assemblies that we will be using to generate tests
        // Using: https://learn.microsoft.com/en-us/dotnet/api/system.reflection?view=net-8.0

        public static void Inspect(string dllPath)
        {

            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"DLL not found at: {dllPath}");
                return;
            }

            try
            {
                var assembly = Assembly.LoadFrom(dllPath);

                // Iterate all public classes and non-public classes (can adjust filters as needed to only generate some basic tests first
                // assembly.GetTypes() - This gets all the types(classes, structs, interfaces, enums etc) in the assembly and returns: Type[]
                // .Where(t => t.IsClass) - Filters the types to include only classes
                // IsClass is a boolean property on Type that returns true if the type is a class (includes abstract and static classes)
                var types = assembly.GetTypes().Where(t => t.IsClass /*&& t.IsPublic*/); // can also filter for only public classes if we need

                foreach (Type type in types)
                {
                    // (public/private/internal/etc)
                    Console.WriteLine($"Class: {GetTypeVisibility(type)} {type.FullName}");

                    PrintAttributes(type.GetCustomAttributes(), "  ");

                    // Print generic type parameters and constraints (if has any)
                    PrintGenericParameters(type);

                    // --- Constructors ---
                    //  instance, static, public, nonpublic, inherited (not using BindingFlags.DeclaredOnly)
                    var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var constructor in constructors)
                    {
                        Console.WriteLine($"  Constructor: {GetMethodVisibility(constructor)} ({GetParameterList(constructor.GetParameters())})");

                        // Get any custom attributes (like [Obsolete], [CompilerGenerated], etc.)
                        PrintAttributes(constructor.GetCustomAttributes(), "    ");
                        
                        PrintGenericParameters(constructor);
                    }

                    // --- Methods ---
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        // Detect if method is explicit interface implementation (private methods with special names containing interface name)
                        bool explicitImpl = method.IsPrivate && method.Name.Contains(".");

                        string explicitMarker;
                        if (explicitImpl)
                        {
                            explicitMarker = "[Explicit Interface Implementation] ";
                        }
                        else
                        {
                            explicitMarker = "";
                        }

                        // Parameter modifiers detection
                        var paramStrings = method.GetParameters()
                            .Select(p => $"{GetParameterModifier(p)}{p.ParameterType.Name} {p.Name}")
                            .ToArray();

                        // Join parameters into a single string like: "int x, string name"
                        string paramList = string.Join(", ", paramStrings);

                        // Get return type of the method (e.g., "void", "int", etc.)
                        string returnType = method.ReturnType.Name;

                        // Print full method signature with:
                        // - Visibility 
                        // - Method name
                        // - Parameters
                        // - Return type
                        Console.WriteLine($"  Method: {explicitMarker}{GetMethodVisibility(method)} {method.Name}({paramList}) : {returnType}");

                        PrintAttributes(method.GetCustomAttributes(), "    ");

                        PrintGenericParameters(method);
                    }

                    // --- Properties ---
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var prop in properties)
                    {

                        if (prop.GetMethod != null)
                            Console.WriteLine($"  Property Getter: {GetMethodVisibility(prop.GetMethod)} {prop.PropertyType.Name} {prop.Name}");

                        if (prop.SetMethod != null)
                            Console.WriteLine($"  Property Setter: {GetMethodVisibility(prop.SetMethod)} {prop.PropertyType.Name} {prop.Name}");

                        PrintAttributes(prop.GetCustomAttributes(), "    ");
                    }


                    // --- Fields ---
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var field in fields)
                    {
                        Console.WriteLine($"  Field: {GetFieldVisibility(field)} {field.FieldType.Name} {field.Name}");

                        PrintAttributes(field.GetCustomAttributes(), "    ");
                    }

                    // --- Events ---
                    var events = type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var ev in events)
                    {
                        string eventType;

                        if (ev.EventHandlerType != null && ev.EventHandlerType.Name != null)
                        {
                            eventType = ev.EventHandlerType.Name;
                        }
                        else
                        {
                            eventType = "<unknown>";
                        }

                        // Check and report add accessor if it exists
                        if (ev.AddMethod != null)
                            Console.WriteLine($"  Event Add: {GetMethodVisibility(ev.AddMethod)} {eventType} {ev.Name}");

                        // Check and report remove accessor if it exists
                        if (ev.RemoveMethod != null)
                            Console.WriteLine($"  Event Remove: {GetMethodVisibility(ev.RemoveMethod)} {eventType} {ev.Name}");

                        PrintAttributes(ev.GetCustomAttributes(), "    ");
                    }

                    // --- Nested Types ---
                    var nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var nested in nestedTypes)
                    {
                        Console.WriteLine($"  Nested Type: {GetTypeVisibility(nested)} {nested.Name}");

                        PrintAttributes(nested.GetCustomAttributes(), "    ");
                    }

                    // --- Interfaces ---
                    var interfaces = type.GetInterfaces();
                    foreach (var iface in interfaces)
                    {
                        Console.WriteLine($"  Implements Interface: {iface.FullName}");
                    }

                    Console.WriteLine();
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine("Error loading types:");

                if (ex.LoaderExceptions != null)
                {
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
                else
                {
                    Console.WriteLine("(No loader exceptions available)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load assembly: {ex.Message}");
            }
        }

        // -- Helpers --

        // readable visibility for types
        static string GetTypeVisibility(Type t)
        {
            if (t.IsPublic || t.IsNestedPublic) return "public";
            if (t.IsNestedFamily) return "protected";
            if (t.IsNestedFamORAssem) return "protected internal";
            if (t.IsNestedAssembly) return "internal";
            if (t.IsNestedPrivate) return "private";
            return "non-public";
        }

        // readable visibility for methods/constructors
        static string GetMethodVisibility(MethodBase m)
        {
            if (m.IsPublic) return "public";
            if (m.IsFamily) return "protected";
            if (m.IsFamilyOrAssembly) return "protected internal";
            if (m.IsAssembly) return "internal";
            if (m.IsPrivate) return "private";
            return "non-public";
        }

        // readable visibility for fields
        static string GetFieldVisibility(FieldInfo f)
        {
            if (f.IsPublic) return "public";
            if (f.IsFamily) return "protected";
            if (f.IsFamilyOrAssembly) return "protected internal";
            if (f.IsAssembly) return "internal";
            if (f.IsPrivate) return "private";
            return "non-public";
        }

        // Get parameter modifier as string (ref, out, in, or empty)
        static string GetParameterModifier(ParameterInfo p)
        {
            if (p.IsOut) return "out ";
            if (p.ParameterType.IsByRef && !p.IsOut) return "ref ";

            var inAttr = p.GetCustomAttributes(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false).FirstOrDefault();
            
            if (inAttr != null) return "in ";
            return "";
        }

        // Print generic parameters and constraints for type or method
        static void PrintGenericParameters(MemberInfo member)
        {
            Type[] genericArgs = Array.Empty<Type>();

            if (member is Type t && t.IsGenericType)
            {
                genericArgs = t.GetGenericArguments();
            }
            else if (member is MethodInfo m && m.IsGenericMethod)
            {
                genericArgs = m.GetGenericArguments();
            }

            if (genericArgs != null && genericArgs.Length > 0)
            {
                Console.WriteLine($"    Generic Parameters: {string.Join(", ", genericArgs.Select(ga => ga.Name))}");
                foreach (var ga in genericArgs)
                {
                    var constraints = ga.GetGenericParameterConstraints();
                    if (constraints.Length > 0)
                    {
                        string cs = string.Join(", ", constraints.Select(c => c.Name));
                        Console.WriteLine($"      Constraint on {ga.Name}: {cs}");
                    }

                    var gpAttributes = ga.GenericParameterAttributes;

                    if (gpAttributes != GenericParameterAttributes.None)
                    {
                        Console.WriteLine($"      Attributes on {ga.Name}: {gpAttributes}");
                    }
                }
            }
        }

        // Print custom attributes in readable form with indentation
        static void PrintAttributes(System.Collections.Generic.IEnumerable<object> attributes, string indent = "")
        {
            foreach (var attr in attributes)
            {
                var attrType = attr.GetType();
                string attrName = attrType.Name;

                // relfection returns something like: SerializableAttribute, want to output just Serializable so stripping it
                if (attrName.EndsWith("Attribute"))
                {
                    attrName = attrName.Substring(0, attrName.Length - "Attribute".Length);
                }

                Console.WriteLine($"{indent}Attribute: [{attrName}]");
            }
        }

        // Format parameters list as string
        static string GetParameterList(ParameterInfo[] parameters)
        {
            return string.Join(", ", parameters.Select(p => $"{GetParameterModifier(p)}{p.ParameterType.Name} {p.Name}"));
        }
    }
}
