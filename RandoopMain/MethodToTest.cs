namespace RandoopMain
{
    public class MethodToTest
    {
        public ReflectedClass Class { get; set; }
        public ReflectedMethod method { get; set; }

        public MethodToTest(ReflectedClass c, ReflectedMethod m)
        {
            Class = c;
            method = m;
        }
    }
}
