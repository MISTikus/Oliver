namespace Oliver.Common.Models;

public struct Instance : IEquatable<Instance>
{
    public Instance(string tenant, string environment) : this()
    {
        Tenant = tenant;
        Environment = environment;
    }

    public string Tenant { get; set; }
    public string Environment { get; set; }

    public bool Equals(Instance other)
    {
        return Tenant == other.Tenant
            && Environment == other.Environment;
    }

    public override bool Equals(object obj)
    {
        var other = obj as Instance?;
        return other.HasValue && Equals(other.Value);
    }

    public override int GetHashCode() => $"{Tenant}{Environment}".GetHashCode();

    public static bool operator ==(Instance left, Instance right) => left.Equals(right);

    public static bool operator !=(Instance left, Instance right) => !left.Equals(right);
}
