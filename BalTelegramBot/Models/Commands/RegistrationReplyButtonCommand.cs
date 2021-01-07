using BalTelegramBot.Models.Commands.Registration_State_Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BalTelegramBot.Models.Commands
{
    public class RegistrationReplyButtonCommand : Command
    {
        public override string Name => "Реєстрація ✏️";

        public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
        {
            await new RegistrationState(client, userInformation).HandleStateRegistrationAsync(message); // Start State Machine
            return true;
        }
    }
}
