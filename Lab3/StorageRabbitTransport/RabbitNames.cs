namespace Services;

/// <summary>
/// Broker address and exchange/queue names shared by the server and the clients
/// Kept in one place so both sides can never disagree on a name
/// </summary>
public static class RabbitNames
{
	/// <summary>
	/// Broker URI. Overridable with the RABBITMQ_URI environment variable
	/// </summary>
	public static readonly string BrokerUri =
		Environment.GetEnvironmentVariable("RABBITMQ_URI") ?? "amqp://guest:guest@localhost:5672/";

	/// <summary>
	/// Direct exchange that all requests and replies go through
	/// </summary>
	public const string ExchangeName = "Storage.Exchange";

	/// <summary>
	/// Storage's request queue. Also used as the routing key for requests
	/// </summary>
	public const string ServerQueueName = "Storage.StorageService";

	/// <summary>
	/// Each client's reply queue is this prefix + a unique client ID
	/// </summary>
	public const string ClientQueueNamePrefix = "Storage.StorageClient_";
}
