using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace CityGameBot.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly List<string> _allCities;

    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger, IMemoryCache memoryCache)
    {
        _botClient = botClient;
        _logger = logger;
        _memoryCache = memoryCache;
        _allCities = GetAllCities();
    }

    private List<string> GetAllCities()
    {
        var assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var json = File.ReadAllText(assemblyPath + "/Cities.json");
        var cities = JsonConvert.DeserializeObject<List<string>>(json);
        if (cities != null)
            return cities;
        return new List<string>();
    }

    public async Task EchoAsync(Update update)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(update.Message!),
            _ => UnkownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        switch (message.Type)
        {
            case MessageType.Text:
                await ReplayNextCity(message);
                break;
            default:
                return;
        }
    }

    private async Task ReplayNextCity(Message message)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(3));
        
        var ignoreValues = new HashSet<char>() { 'ы', 'ь', 'ъ' };
        if (!_memoryCache.TryGetValue(message.Chat.Id, out List<string>? cities))
        {
            _memoryCache.Set(message.Chat.Id, _allCities, cacheEntryOptions);
        }

        if (cities != null)
        {
            //TODO: какой клеч кэша выбрать? уникальный для юзера
            //Добавить копку для перезапуска игры
            //Бот должен проверять что юзер его не набетывает
            var lastChar = message.Text.Last();
            if (ignoreValues.Contains(lastChar))
                lastChar = message.Text[message.Text.Length - 2];

            lastChar = char.ToUpper(lastChar);
            _logger.LogInformation($"Last char: {lastChar}");
            foreach (var city in cities)
            {
                //TODO: Исключать из сыдачи те города, которые были.
                if (city[0] == lastChar)
                {
                    cities.Remove(city);
                    _memoryCache.Set(message.Chat.Id, cities, cacheEntryOptions);
                    await _botClient.SendTextMessageAsync(message.Chat.Id, city);
                    return;
                }
            }
            await _botClient.SendTextMessageAsync(message.Chat.Id, "Черт! Я проиграл(");
        }
    }


    private Task UnkownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unkown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
    
    public Task HandleErrorAsync(Exception exception)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegrap API Error: \n[{apiRequestException.ErrorCode}]" +
                                                       $"\n{apiRequestException.Message}",
            _ => exception.ToString()
        };
        
        _logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }
}