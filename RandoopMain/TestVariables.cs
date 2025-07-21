public class TestVariables
{
    public string name { get; set; } = "";
    public Type type;
    public object instance;
    public string construction;
    public bool isOut = false;

    public TestVariables(Type type, object instance, string construction)
    {
        this.type = type;
        this.instance = instance;
        this.construction = construction;
    }
}
