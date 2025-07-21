using Xunit;

namespace GeneratedTests
{
    public class Test1_Test25
    {
        [Fact]
        public void Test_Test1_DivideFloat_25()
        {
            double v0 = -403.466147659848;
            double v1 = -912.8951433023543;
            var instance = new TestLibrary.Test1();
            var result = instance.DivideFloat(v0,v1);
            Assert.Equal(0.4419632973402932d, result);

        }
    }
}
