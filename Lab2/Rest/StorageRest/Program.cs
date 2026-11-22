namespace Servers;

using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;

using NLog;
using StorageCore;

public class Server
{
    Logger log = LogManager.GetCurrentClassLogger();

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

    public static void Main(string[] args)
	{
		var self = new Server();
		self.Run(args);
	}

	private void Run(string[] args) 
	{
		//configure logging
		ConfigureLogging();

		//indicate server is about to start
		log.Info("Server is about to start");

		//start the server
		StartServer(args);
	}

    private void StartServer(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //configure integrated server
		builder.WebHost.ConfigureKestrel(opts => {
			opts.Listen(IPAddress.Loopback, 5001);
		});

		//add and configure swagger documentation generator (http://127.0.0.1:5001/swagger/)
		builder.Services.AddSwaggerGen(opts => {
			//include code comments in swagger documentation
			var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
			opts.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
		});

		//turn on support for web api controllers
		builder.Services
			.AddControllers()
			.AddJsonOptions(opts => {
				//this makes enumeration values to be strings instead of integers in opeanapi doc
				opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			});

		//add CORS policies
		builder.Services.AddCors(cr => {
			//allow everything from everywhere
			cr.AddPolicy("allowAll", cp => {
				cp.AllowAnyOrigin();
				cp.AllowAnyMethod();
				cp.AllowAnyHeader();
			});
		});

		//publish the background logic as (an internal) service through dependency injection, 
		//otherwise it will not start until the first client calls into controller
		builder.Services.AddSingleton<IStorageLogic, StorageLogic>();

		//build the server
		var app = builder.Build();

		//turn CORS policy on
		app.UseCors("allowAll");

		//turn on support for swagger doc web page
		app.UseSwagger();
		app.UseSwaggerUI();

		//turn on request routing
		app.UseRouting();

        app.MapControllers();

		//run the server
		app.Run();
    }
}



