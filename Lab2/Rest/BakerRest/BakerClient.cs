namespace BakerRest;

using GeneratedRest;
using NLog;

public class BakerClient
{
    private readonly List<string> NAMES = 
		new List<string> { 
			"Jonas", "Petras", "Dzekas", "Stivas"
		};
	private readonly List<string> SURNAMES = 
		new List<String> { 
			"Jonauskas", "Petrauskas", "Dzekauskas", "Stivensonas" 
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

                var baker = new BakerDesc();
                baker.NameSurname = NAMES[random.Next(NAMES.Count)] + 
                    " " +
                    SURNAMES[random.Next(SURNAMES.Count)];

                baker.Id = storage.Bakers(baker);
                mLog.Info($"REST baker {baker.NameSurname} registered with ID {baker.Id}.");

                while (true)
                {
                    Season season = storage.Season();

                    int period =
                        season == Season.Winter || season == Season.Spring
                            ? 10000
                            : 5000;

                    Thread.Sleep(period);

                    int amount = random.Next(0, 101);

                    mLog.Info(
                        $"REST baker {baker.Id} ({baker.NameSurname}) generated {amount} grains.");

                    GrainOperationResult result =
                        storage.Consume(amount, baker);

                    mLog.Info(
                        $"REST consumption result: {result.Status}. " +
                        $"{result.Reason}");
                }
            }
            catch (Exception exception)
            {
                mLog.Warn(
                    exception,
                    "REST baker connection failed. Reconnecting.");

                Thread.Sleep(2000);
            }
        }
    }

    public static void Main(string[] args)
    {
        var client = new BakerClient();
        client.Run();
    }
}