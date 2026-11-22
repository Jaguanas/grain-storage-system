namespace BakerGrpc;

using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using NLog;
using StorageGrpc.Services;

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

    private readonly Logger mlog =
        LogManager.GetCurrentClassLogger();

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

    public async Task RunAsync()
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

                var baker = new BakerDesc();
                baker.NameSurname = NAMES[random.Next(NAMES.Count)] + 
                    " " +
                    SURNAMES[random.Next(SURNAMES.Count)];

                var registrationResponse = await storage.RegisterBakerAsync(baker);

                baker.Id = registrationResponse.Id;

                mlog.Info($"gRPC baker {baker.NameSurname} registered with ID {baker.Id}.");

                while (true)
                {
                    var seasonResponse = await storage.GetSeasonAsync(new Empty());
                    Season season = seasonResponse.Season;

                    int period =
                        season == Season.Winter || season == Season.Spring
                            ? 10000
                            : 5000;

                    await Task.Delay(period);

                    var amount = random.Next(0, 101);

                    mlog.Info(
                        $"gRPC baker {baker.Id} ({baker.NameSurname}) generated {amount} grains.");

                    var result = await storage.ConsumeAsync(
                            new ConsumeRequest
                            {
                                Baker = baker,
                                Amount = amount
                            });

                    mlog.Info($"gRPC consumption result: {result.Status}. {result.Reason}");
                }
            }
            catch (Exception exception)
            {
                mlog.Warn(
                    exception,
                    "gRPC baker connection failed. Reconnecting.");

                await Task.Delay(2000);
            }
        }
    }

    public static async Task Main(string[] args)
    {
        var client = new BakerClient();

        await client.RunAsync();
    }
}