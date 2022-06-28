namespace Oliver.Common.Models;

public class VariableSet
{
    public long Id { get; set; }
    public Instance Instance { get; set; }
    public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
}
