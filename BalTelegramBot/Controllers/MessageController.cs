using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BalTelegramBot.Models;
using BalTelegramBot.Models.Commands;
using BalTelegramBot.Models.Commands.Registration_State_Machine;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BalTelegramBot.Controllers
{
    [Route("api/message/update")]
    public class MessageController : Controller
    {
        // GET api/values
        [HttpGet]
        public IActionResult Get()
        {
            return Redirect("http://127.0.0.1:4040/inspect/http");
        }

        // POST api/values
        [HttpPost]
        public async Task<OkResult> Post([FromBody]Update update)
        {
            if (update == null) return Ok();

            var commands = Bot.Commands;
            var ikCommands = Bot.InlineKeyboardCommands;

            Message message = default;
            switch(update.Type)
            {
                case Telegram.Bot.Types.Enums.UpdateType.Message:
                    message = update.Message;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.CallbackQuery:
                    message = update.CallbackQuery.Message;
                    break;
                default:
                    throw new NotImplementedException();
            }
 
            var botClient = await Bot.GetBotClientAsync();
            await botClient.SendChatActionAsync(message.Chat.Id, Telegram.Bot.Types.Enums.ChatAction.Typing);

            var userInformation = await BalDbController.GetUserInformationAsync(message.Chat.Id);
            if (userInformation == null)
            {
                await new StartCommand().Execute(message, botClient, null);
                return Ok();
            }
            // Inline keyboard commands
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
            {
                foreach(var command in ikCommands)
                {
                    if(command.Contains(message))
                    {
                        try
                        {
                            await command.Execute(update.CallbackQuery, botClient, userInformation);
                        }
                        catch(Exception ex)
                        {
                            await SendMessageToCreatorOfException(ex, userInformation, botClient);
                        }
                        return Ok();
                    }
                }
            }
            // Bot commands
            foreach (var command in commands)
            {
                if (command.Contains(message))
                {
                    try
                    {
                        var result = await command.Execute(message, botClient, userInformation);
                    }
                    catch (Exception ex)
                    {
                        await SendMessageToCreatorOfException(ex, userInformation, botClient);
                    }
                    return Ok();  
                }
            }
            // State Machine Registration
            if (userInformation.IsRegistred == false && userInformation.State != RegistrationStateMachine.None.ToString()) // If user start registred
            {
                try
                {
                    await new RegistrationState(botClient, userInformation)
                        .HandleStateRegistrationAsync(message); // Go to state machine registration
                }
                catch (Exception ex)
                {
                    await SendMessageToCreatorOfException(ex, userInformation, botClient);
                }

                return Ok();
            }
            // State Machine Send Message Other User
            if((Regex.IsMatch(userInformation.State, SendMessagesCommand.SendMessageState.FromPupilToTeacher.ToString())  || 
                Regex.IsMatch(userInformation.State, SendMessagesCommand.SendMessageState.FromTeacherToPupils.ToString()) ||
                Regex.IsMatch(userInformation.State, SendMessagesCommand.SendMessageState.FromPupilToClassmates.ToString()) 
                )
               )
            {
                await new SendMessagesCommand().Execute(message, botClient, userInformation);
            }
            return Ok();
        }

        public static async Task SendMessageToCreatorOfException(Exception ex, UserInfo userInfo, TelegramBotClient client)
        {
            long chatIdCreator = 363574232;
            string message = default;
            if (userInfo != null && ex != null)
            {
                message += $"У користувача {userInfo.ChatId} name: {userInfo.NameUser} сталася помилка.\n";
                message += $"Exception: {ex}. InnerException: {ex.InnerException}. StackTrace: {ex.StackTrace}\n";
                message += $"Message: {ex.Message}.\n";
                message += $"Час: {DateTime.Now}";
                //message += $"Останне повідомлення користувача: {}";
                await client.SendTextMessageAsync(  chatId: chatIdCreator,
                                                    text: message);
            }
        }
    }
}
/*
 1. Сделать учителей.
 2. Сделать блок настройки.
 3. Прогнать учеников и учителей.
 4. Прогнать админа.
 5. Пройтись по Task List.
 6. Финальноге тестирование. 
     */
