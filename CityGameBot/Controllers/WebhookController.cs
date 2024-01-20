using CityGameBot.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace CityGameBot.Controllers;

public class BotController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromServices] HandleUpdateService handleUpdateService,
        [FromBody] Update update)
    {
        await handleUpdateService.EchoAsync(update);
        return Ok();
    }
}