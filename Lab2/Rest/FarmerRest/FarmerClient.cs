namespace FarmerRest;

using GeneratedRest;
using NLog;

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

    Logger mLog = LogManager.GetCurrentClassLogger();

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

    public void Run()
    {
        ConfigureLogging();

        while (true)
        {
            try
            {   
                using var httpClient = new HttpClient();

                var storageUrl =
                    Environment.GetEnvironmentVariable("STORAGE_URL")
                    ?? "http://127.0.0.1:5001";

                var storage = new StorageRestClient(
                    storageUrl,
                    httpClient);
			
                var random = new Random();

                var farmer = new FarmerDesc();
                farmer.NameSurname = NAMES[random.Next(NAMES.Count)] + 
                    " " +
                    SURNAMES[random.Next(SURNAMES.Count)];

                farmer.Id = storage.Farmers(farmer);
                mLog.Info($"REST farmer {farmer.NameSurname} registered with ID {farmer.Id}.");

                while (true)
                {
                    Season season = storage.Season();

                    while (season == Season.Winter || season == Season.Spring)
					{
						mLog.Info($"Current season is {season}. Sleeping...");
						Thread.Sleep(2000);
						season = storage.Season();
					}

                    int amount = random.Next(0, 101);

                    GrainOperationResult result =
						storage.Produce(amount, farmer);

					// Retry the same amount until storage has enough free capacity.
					while ( result.Status == GrainOperationStatus.StorageFull )
					{
						mLog.Info($"Currently the storage is full! Retrying in 5 sec...");
						Thread.Sleep(5000);

						result = storage.Produce(amount, farmer);
					}

					// Explain a winter transition that invalidated this deposit.
					if (result.Status == GrainOperationStatus.SeasonChanged)
					{
						mLog.Info($"The season changed to winter. This amount of grain lost: {amount}");
					}

					// Explain why spring deposits are rejected.
					if (result.Status == GrainOperationStatus.InactiveSeason)
					{
						mLog.Info($"Currently it is spring and no grain can be generated.");
					}

					mLog.Info(
						$"Generation result: {result.Status}. {result.Reason}");

					Thread.Sleep(3000 + random.Next(3000));
                }
            }
            catch (Exception exception)
            {
                mLog.Warn(
                    exception,
                    "REST farmer connection failed. Reconnecting.");

                Thread.Sleep(2000);
            }
        }
    }

    public static void Main(string[] args)
    {
        var client = new FarmerClient();
        client.Run();
    }
}