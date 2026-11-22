namespace FarmerGRPC;

using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using NLog;
using StorageGrpc.Services;

public class FarmerClient
{
    private readonly List<string> NAMES = 
		new List<string> { 
			"John", "Peter", "Jack", "Steve"
		};
	private readonly List<string> SURNAMES = 
		new List<String> { 
			"Johnson", "Peterson", "Jackson", "Steveson" 
		};

    Logger mlog = LogManager.GetCurrentClassLogger();

    private void ConfigureLogging()
    {
        var config = new NLog.Config.LoggingConfiguration();

        var console = new NLog.Targets.ConsoleTarget("console")
        {
            Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
        };

        config.AddTarget(console);
        config.AddRuleForAllLevels(console);

        LogManager.Configuration = config;
    }

    public async Task Run()
    {
        ConfigureLogging();

        while (true)
        {
            try
            {   
                var grpcAddress =
                    Environment.GetEnvironmentVariable("STORAGE_GRPC_URL")
                    ?? "http://127.0.0.1:5002";

                using var channel = GrpcChannel.ForAddress(grpcAddress);

                var storage = new Storage.StorageClient(channel);
			
                var random = new Random();

                var farmer = new FarmerDesc();
                farmer.NameSurname = NAMES[random.Next(NAMES.Count)] + 
                    " " +
                    SURNAMES[random.Next(SURNAMES.Count)];

                var registrationResponse = await storage.RegisterFarmerAsync(farmer);

                farmer.Id = registrationResponse.Id;

                mlog.Info($"gRPC farmer {farmer.NameSurname} registered with ID {farmer.Id}.");

                while (true)
                {
                    var seasonResponse = await storage.GetSeasonAsync(new Empty());
                    Season season = seasonResponse.Season;

                    while (season == Season.Winter || season == Season.Spring)
					{
						mlog.Info($"Current season is {season}. Sleeping...");
						await Task.Delay(2000);

						seasonResponse = await storage.GetSeasonAsync(new Empty());
                        season = seasonResponse.Season;
					}

                    int amount = random.Next(0, 101);

                    var result = await storage.ProduceAsync(
                            new ProduceRequest
                            {
                                Farmer = farmer,
                                Amount = amount
                            });

					// Retry the same amount until storage has enough free capacity.
					while ( result.Status == GrainOperationStatus.StorageFull )
					{
						mlog.Info($"Currently the storage is full! Retrying in 5 sec...");
						await Task.Delay(5000);

						result = await storage.ProduceAsync(
                            new ProduceRequest
                            {
                                Farmer = farmer,
                                Amount = amount
                            });
					}

					// Explain a winter transition that invalidated this deposit.
					if (result.Status == GrainOperationStatus.SeasonChanged)
					{
						mlog.Info($"The season changed to winter. This amount of grain lost: {amount}");
					}

					// Explain why spring deposits are rejected.
					if (result.Status == GrainOperationStatus.InactiveSeason)
					{
						mlog.Info($"Currently it is spring and no grain can be generated.");
					}

					mlog.Info(
						$"Generation result: {result.Status}. {result.Reason}");

					await Task.Delay(3000 + random.Next(3000));
                }
            }
            catch (Exception exception)
            {
                mlog.Warn(
                    exception,
                    "gRPC farmer connection failed. Reconnecting.");

                await Task.Delay(2000);
            }
        }
    }

    public static async Task Main(string[] args)
    {
        var client = new FarmerClient();
        await client.Run();
    }
}