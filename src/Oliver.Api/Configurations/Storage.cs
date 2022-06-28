namespace Oliver.Api.Configurations;

public record Storage
{
    public string QueuesFolder { get; set; }
    public string BlobFolder { get; set; }
}
