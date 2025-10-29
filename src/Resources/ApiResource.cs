namespace PayOS.Resources;


/// <summary>
/// Base class for API resource services
/// </summary>
public abstract class ApiResource(PayOSClient client)
{
    protected readonly PayOSClient _client = client;
}