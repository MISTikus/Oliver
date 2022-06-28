namespace Oliver.Client.Configurations;

internal record Server
{
    public string BaseUrl { get; set; }
    public string ApiVersion { get; set; } = "1";
}
