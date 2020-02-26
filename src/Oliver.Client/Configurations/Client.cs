namespace Oliver.Client.Configurations
{
    internal class Client
    {
        public string DefaultFolder { get; set; }
        public Instance[] Instances { get; set; }

        public class Instance
        {
            public string Tenant { get; set; }
            public string Environment { get; set; }
        }
    }
}
