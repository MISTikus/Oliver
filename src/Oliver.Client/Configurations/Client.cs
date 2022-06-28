namespace Oliver.Client.Configurations;

internal record Client
{
    public string DefaultFolder { get; set; }
    public Instance[] Instances { get; set; }

    public record Instance
    {
        public string Tenant { get; set; }
        public string Environment { get; set; }
    }
}
