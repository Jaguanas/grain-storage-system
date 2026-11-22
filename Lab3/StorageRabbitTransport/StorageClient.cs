namespace Clients;

using System.Text;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using NLog;

using Services;

/// <summary>
/// Implements the Storage contract by turning every call into
/// a request/reply exchange over RabbitMQ
/// </summary>
public class StorageClient : IStorageService
{
	/// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Unique ID of this client. Used to build the reply queue name
	/// </summary>
	public string ClientId { get; }

	/// <summary>
	/// Name of this client's private reply queue
	/// </summary>
	private string ClientQueueName { get; }

	/// <summary>
	/// Connection to RabbitMQ message broker
	/// </summary>
	private IConnection mConn;

	/// <summary>
	/// Communications channel to RabbitMQ message broker
	/// </summary>
	private IChannel mChann;


	/// <summary>
	/// Constructor. Connects to the broker and sets up the queues this client needs
	/// </summary>
	public StorageClient()
	{
		//initialize properties
		ClientId = Guid.NewGuid().ToString();
		ClientQueueName = RabbitNames.ClientQueueNamePrefix + ClientId;

		//log client ID for easier traceability
		mLog.Info($"Client ID is '{ClientId}'.");

		//
		var asyncScope = async () => {
			//connect to the RabbitMQ message broker
			var rmqConnFact = new ConnectionFactory();
			rmqConnFact.Uri = new Uri(RabbitNames.BrokerUri);
			mConn = await rmqConnFact.CreateConnectionAsync();

			//get channel, configure exchanges and request queue
			mChann = mConn.CreateChannelAsync().GetAwaiter().GetResult();

			await mChann.ExchangeDeclareAsync(exchange: RabbitNames.ExchangeName, type: ExchangeType.Direct);
			await mChann.QueueDeclareAsync(queue: ClientQueueName, durable: false, exclusive: true, autoDelete: false, arguments: null);
			await mChann.QueueDeclareAsync(queue: RabbitNames.ServerQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
			await mChann.QueueBindAsync(queue: ClientQueueName, exchange: RabbitNames.ExchangeName, routingKey: ClientQueueName, arguments: null);
			await mChann.QueueBindAsync(queue: RabbitNames.ServerQueueName, exchange: RabbitNames.ExchangeName, routingKey: RabbitNames.ServerQueueName, arguments: null);
		};
		asyncScope().GetAwaiter().GetResult();
	}

	/// <summary>
	/// Sends one request and blocks until the matching reply arrives
	/// </summary>
	/// <typeparam name="RESULT">Type of the result</typeparam>
	/// <param name="methodName">Contract method name</param>
	/// <param name="requestData">Arguments to send or null if there are none</param>
	/// <returns>Deserialized result from the reply</returns>
	private RESULT Call<RESULT>(string methodName, object requestData)
	{
		//
		var asyncScope = async () => {
			
			//declare result storage
			RESULT result = default;

			//declare stuff used to avoid result owerwriting and to signal when result is ready
			var isResultReady = false;
			var resultReadySignal = new AutoResetEvent(false);

			//create request
			var request =
				new RPCMessage()
				{
					Action = $"Call_{methodName}",
                    Data = requestData != null ? JsonConvert.SerializeObject(requestData) : null
                };

			var requestProps = new BasicProperties();
			requestProps.CorrelationId = Guid.NewGuid().ToString();
			requestProps.ReplyTo = ClientQueueName;

			string consumerTag = null;

			//ensure contents of variables set in main thread, are loadable by receiver thread
			Thread.MemoryBarrier();

			//create response message consumer
			var consumer = new AsyncEventingBasicConsumer(mChann);
			consumer.ReceivedAsync +=
				async (channel, delivery) => {
					
					//we need to catch exceptions manually here, otherwise they get swallowed silently
					try
					{
						//ensure contents of variables set in main thread are loaded into this thread
						Thread.MemoryBarrier();

						//prevent owerwriting of result, check if the expected message is received
						if (!isResultReady &&
							(delivery.BasicProperties.CorrelationId == requestProps.CorrelationId))
						{
							var response =
								JsonConvert.DeserializeObject<RPCMessage>(
									Encoding.UTF8.GetString(delivery.Body.ToArray()));
							if (response.Action == $"Result_{methodName}")
							{
								//extract the result
								result = JsonConvert.DeserializeAnonymousType(response.Data, new { Value = default(RESULT) }).Value;

								//indicate result has been received, ensure it is loadable by main thread
								isResultReady = true;
								Thread.MemoryBarrier();

								//signal main thread that result has been received
								resultReadySignal.Set();
							}
							else
							{
								mLog.Info(
									$"Unsupported type of RPC action '{request.Action}'. Ignoring the message.");
							}
						}
					}
					catch (Exception e)
					{
						mLog.Error(e, "Unhandled exception caught in event handler.");
					}
				};

			//attach message consumer to the response queue
			consumerTag = await mChann.BasicConsumeAsync(ClientQueueName, true, consumer);

			//send request
			await mChann.BasicPublishAsync(
				exchange : RabbitNames.ExchangeName,
				routingKey : RabbitNames.ServerQueueName,
				mandatory: true,
				basicProperties : requestProps,
				body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
			);

			//wait for the result to be ready
			resultReadySignal.WaitOne();

			//ensure contents of variables set by the receiver are loaded into this thread
			Thread.MemoryBarrier();

			//detach message consumer from the response queue
			await mChann.BasicCancelAsync(consumerTag);

			return result;	
		};

		var res = asyncScope().GetAwaiter().GetResult();

		return res;
	}

	// Each contract method is a one-liner around Call<T>
	public int RegisterBaker(BakerDesc baker)
	{
		return Call<int>(nameof(RegisterBaker), baker);
	}

	public int RegisterFarmer(FarmerDesc farmer)
	{
		return Call<int>(nameof(RegisterFarmer), farmer);
	}

	public Season GetSeasonState()
	{
		return Call<Season>(nameof(GetSeasonState), null);
	}

	public GrainOperationResult ConsumeGrain(BakerDesc baker, int amount)
	{
		return Call<GrainOperationResult>(nameof(ConsumeGrain), new {baker, amount});
	}

	public GrainOperationResult TryDepositGrain(FarmerDesc farmer, int amount)
	{
		return Call<GrainOperationResult>(nameof(TryDepositGrain), new {farmer, amount});
	}
}
