using CityGameBot.Controllers;
using CityGameBot.Extensions;
using CityGameBot.Models;
using CityGameBot.Services;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

var botConfigurationSection = builder.Configuration.GetSection("BotConfiguration");
builder.Services.Configure<BotConfiguration>(botConfigurationSection);
var botConfiguration = botConfigurationSection.Get<BotConfiguration>();

builder.Services.AddHostedService<ConfigureWebhook>();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<HandleUpdateService>();

builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        TelegramBotClientOptions options = new(botConfiguration.BotToken);
        return new TelegramBotClient(options, httpClient);
    });

builder.Services.AddControllers()
    .AddNewtonsoftJson();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.MapBotWebhookRoute<BotController>(route: botConfiguration.Route);
app.MapControllers();
app.Run();