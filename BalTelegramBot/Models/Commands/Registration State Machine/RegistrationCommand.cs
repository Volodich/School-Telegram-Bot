using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BalTelegramBot.Models.Commands.Registration_State_Machine
{
    public class RegistrationCommand : Command
    {
        public override string Name => "/reg";

        public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
        {
            await Task.Run(() => new RegistrationReplyButtonCommand().Execute(message, client, userInformation));
            return true;
        }
    }

    public class GiveSuperuser : Command
    {
        public override string Name => "/root" + AppSettings.SuperuserPassword;
        public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
        {
            await AddSuperuser(userInformation).ContinueWith(_ => client.SendTextMessageAsync(chatId: userInformation.ChatId, text: "Вам надано права адміна."));

            await client.SendTextMessageAsync(chatId: AppSettings.ChatIdCreator, text: $"Користувачу chatId: {userInformation.ChatId} name: {userInformation.NameUser} надано права адміна.");

            return true;
        }

        private static async Task AddSuperuser(UserInfo user)
        {
            using (var db = new BalDbContext())
            {
                var admin = await db.UserInfo.Where(ui => ui.ChatId == user.ChatId).SingleAsync();
                admin.IsAdmin = true;
                await db.SaveChangesAsync();
            }
        }
    }

}
