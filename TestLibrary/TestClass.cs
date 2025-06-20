namespace TestLibrary
{
    public class Test1
    {
        public int Add(int a, int b) => a + b;

        public double Divide(double numerator, double denominator) => numerator / denominator;

        public string Echo(string message) => $"Echo: {message}";

        public static void PrintHello() => Console.WriteLine("Test1");

        private void Hidden() { } 
    }

    public class Test2
    {
        public string Name { get; set; }

        public Test2() { Name = "Default"; }

        public Test2(string name) { Name = name; }

        public string ReturnString() => $"Name is {Name}";

        public static int StaticValue => 42;

        public event EventHandler OnSomething = delegate { };

        public void RaiseEvent()
        {
            if (OnSomething != null)
            {
                OnSomething.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class Test3
    {
        // Fields (public and private)
        public int publicField = 10;
        private string privateField = "privateValue";

        // Properties with getter and setter
        public int Prop { get; set; }

        // Constructor with parameters
        public Test3(int initialValue) { Prop = initialValue; }

        // Overloaded methods
        public void Overload() { Console.WriteLine("No params"); }
        public void Overload(int x) { Console.WriteLine($"Int param: {x}"); }
        public void Overload(string s) { Console.WriteLine($"String param: {s}"); }

        // Generic method
        public T Identity<T>(T input) => input;
    }

    public static class Test4
    {
        // Static class - only static members
        public static void StaticMethod() => Console.WriteLine("StaticMethod called");

        public static int StaticProperty { get; set; } = 100;

        public static int StaticField = 200;
    }

    public class Test5
    {
        // Method with ref and out parameters
        public void RefOutMethod(ref int a, out int b)
        {
            a += 10;
            b = 42;
        }

        // Method with params array
        public int SumParams(params int[] numbers)
        {
            int sum = 0;
            foreach (var n in numbers) sum += n;
            return sum;
        }
    }

    public class Test6<T>
    {
        public T GenericProperty { get; set; }

        public Test6(T value)
        {
            GenericProperty = value;
        }

        public T GenericMethod(T input) => input;

        public static void StaticGenericMethod<U>(U param)
        {
            Console.WriteLine($"Generic static method with {typeof(U).Name}: {param}");
        }
    }

    public interface ITest7
    {
        void InterfaceMethod();
    }

    public class Test7Impl : ITest7
    {
        public void InterfaceMethod()
        {
            Console.WriteLine("Interface method implemented");
        }
    }

    public abstract class Test8Base
    {
        public abstract void AbstractMethod();

        public virtual string VirtualMethod() => "Base virtual method";
    }

    public class Test8Derived : Test8Base
    {
        public override void AbstractMethod()
        {
            Console.WriteLine("Implemented abstract method");
        }

        public override string VirtualMethod() => "Derived virtual method";
    }
}
