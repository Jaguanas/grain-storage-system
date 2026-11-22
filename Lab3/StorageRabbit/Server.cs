namespace Servers;

using NLog;

using StorageCore;

/// <summary>
/// Server host: starts the RabbitMQ service and keeps the process alive.
/// </summary>
class Server
{
	/// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger log = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Configure logging subsystem.
	/// </summary>
	private void ConfigureLogging()
	{
		var config = new NLog.Config.LoggingConfiguration();

		var console =
			new NLog.Targets.ConsoleTarget("console")
			{
				Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
			};
		config.AddTarget(console);
		config.AddRuleForAllLevels(console);

		LogManager.Configuration = config;
	}

	/// <summary>
	/// Program body.
	/// </summary>
	private void Run()
	{
		ConfigureLogging();

		// Created once, outside the restart loop: a broker reconnect must not reset the storage
		// state or start a second season thread
		IStorageLogic logic = new StorageLogic();

		while( true )
		{
			try
			{
				// Connects to the broker and starts consuming requests.
				var service = new StorageService(logic);

				log.Info("Server has been started.");

				// Requests are handled on RabbitMQ.Client's consumer threads; the main thread only keeps the process alive.
				while( true )
				{
					Thread.Sleep(1000);
				}
			}
			catch( Exception e )
			{
				log.Error(e, "Unhandled exception caught. Server will now restart.");

				//prevent console spamming
				Thread.Sleep(2000);
			}
		}
	}

	/// <summary>
	/// Program entry point.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	static void Main(string[] args)
	{
		var self = new Server();
		self.Run();
	}
}
