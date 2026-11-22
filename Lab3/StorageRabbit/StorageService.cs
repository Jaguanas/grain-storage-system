namespace Servers;

using System.Text;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using NLog;

using Services;
using StorageCore;
using System.Text.Json.Nodes;


/// <summary>
/// RabbitMQ adapter over IStorageLogic: consumes requests, calls the logic, publishes replies.
/// </summary>
class StorageService
{
	/// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger log = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Connection to RabbitMQ message broker.
	/// </summary>
	private IConnection mConn;

	/// <summary>
	/// Communications channel to RabbitMQ message broker.
	/// </summary>
	private IChannel mChann;

	/// <summary>
	/// Thread-safe storage rules and season thread (shared with Lab 1 and Lab 2).
	/// </summary>
	private readonly IStorageLogic mLogic;


	/// <summary>
	/// Constructor. Connects, declares the request queue and starts consuming.
	/// </summary>
	/// <param name="logic">Storage logic to forward calls to.</param>
	public StorageService(IStorageLogic logic)
	{
		mLogic = logic;

		var asyncScope = async () => {
			//connect to the RabbitMQ message broker
			var rmqConnFact = new ConnectionFactory();
			rmqConnFact.Uri = new Uri(RabbitNames.BrokerUri);
			mConn = await rmqConnFact.CreateConnectionAsync();

			//get channel, configure exchanges and request queue
			mChann = mConn.CreateChannelAsync().GetAwaiter().GetResult();

			await mChann.ExchangeDeclareAsync(exchange: RabbitNames.ExchangeName, type: ExchangeType.Direct);
			await mChann.QueueDeclareAsync(queue: RabbitNames.ServerQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
			await mChann.QueueBindAsync(queue: RabbitNames.ServerQueueName, exchange: RabbitNames.ExchangeName, routingKey: RabbitNames.ServerQueueName, arguments: null);

			//connect to the queue as consumer
			var rmqConsumer = new AsyncEventingBasicConsumer(mChann);
			rmqConsumer.ReceivedAsync += async (consumer, msg) =>
			{
				//we need to catch exceptions manually here, otherwise they get swallowed silently
				try
				{
					await OnMessageReceived(((AsyncEventingBasicConsumer)consumer).Channel, msg);
				}
				catch (Exception e)
				{
					log.Error(e,"Unhandled exception caught in event handler.");
				}
			};
			await mChann.BasicConsumeAsync(queue: RabbitNames.ServerQueueName, autoAck: true, consumer : rmqConsumer);
		};
		asyncScope().GetAwaiter().GetResult();
	}

	/// <summary>
	/// Handles one request message. Don't assume it always runs on the same thread.
	/// </summary>
	/// <param name="channel">Channel the message came from (use it to reply).</param>
	/// <param name="msgIn">Delivered message.</param>
	private async Task OnMessageReceived(IChannel channel, BasicDeliverEventArgs msgIn)
	{
		try
		{
			//get call request
			var request =
				JsonConvert.DeserializeObject<RPCMessage>(
					Encoding.UTF8.GetString(
						msgIn.Body.ToArray()
					)
				);

			//set response as undefined by default
			RPCMessage response = null;

			log.Debug($"Received {request.Action}, call {msgIn.BasicProperties.CorrelationId}, client {msgIn.BasicProperties.ReplyTo}");

			//process the call
			switch(request.Action)
			{
				case $"Call_{nameof(mLogic.GetSeasonState)}":
				{
					//make the call
					var result = mLogic.GetSeasonState();

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(mLogic.GetSeasonState)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				case $"Call_{nameof(mLogic.RegisterBaker)}":
				{
					//deserialize arguments
					var bakerDesc = JsonConvert.DeserializeObject<BakerDesc>(request.Data);

					//make the call
					var result = mLogic.RegisterBaker(bakerDesc);

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(mLogic.RegisterBaker)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				case $"Call_{nameof(mLogic.RegisterFarmer)}":
				{
					//deserialize arguments
					var farmerDesc = JsonConvert.DeserializeObject<FarmerDesc>(request.Data);

					//make the call
					var result = mLogic.RegisterFarmer(farmerDesc);

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(mLogic.RegisterFarmer)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				case $"Call_{nameof(mLogic.ConsumeGrain)}":
				{
					//deserialize arguments
					var args = JsonConvert.DeserializeAnonymousType(request.Data, new {baker = new BakerDesc(), amount = 0});

					//make the call
					var result = mLogic.ConsumeGrain(args.baker, args.amount);

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(mLogic.ConsumeGrain)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				case $"Call_{nameof(mLogic.TryDepositGrain)}":
				{
					//deserialize arguments
					var args = JsonConvert.DeserializeAnonymousType(request.Data, new {farmer = new FarmerDesc(), amount = 0});

					//make the call
					var result = mLogic.TryDepositGrain(args.farmer, args.amount);

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(mLogic.TryDepositGrain)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				default:
					
					log.Error($"Error in the request: {request.Action}; given data: {request.Data}");
					break;

			}

			//response is defined? send reply message
			if( response != null )
			{
				//prepare metadata for outgoing message
				var msgOutProps = new BasicProperties();
				msgOutProps.CorrelationId = msgIn.BasicProperties.CorrelationId;

				//send reply message to the client queue
				await channel.BasicPublishAsync(
					exchange : RabbitNames.ExchangeName,
					routingKey : msgIn.BasicProperties.ReplyTo,
					mandatory: true,
					basicProperties : msgOutProps,
					body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response))
				);
			}
		}

		catch( Exception e )
		{
			log.Error(e, "Unhandled exception caught when processing a message. The message is now lost.");
		}
	}
}
