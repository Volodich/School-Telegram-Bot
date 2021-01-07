using BalTelegramBot.Controllers;
using BalTelegramBot.Models.Commands.Registration_State_Machine;
using System.Threading.Tasks;
using BalTelegramBot.Models.Commands.Menu;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BalTelegramBot.Models.Commands
{
    public class LoginGuestReplyButtonCommand : Command
    { 
        public override string Name => "Війти гостем 👀";

        private string RegistrationCommandText => new RegistrationCommand().Name;

        public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
        {
            userInformation.TypeUser = TypeUser.Guest.ToString();
            userInformation.State = RegistrationStateMachine.None.ToString();
            
            await BalDbController.UpdateUserDataAsync(userInformation);

            await client.SendTextMessageAsync(userInformation.ChatId,
                                              text: $"Ви увійшли як гість. Щоб зареєструватись - відправте команду {RegistrationCommandText}",
                                              replyMarkup: new ReplyKeyboardRemove());
            await Task.Run(() => new MainMenuCommand().Execute(null, client, userInformation)); // go to main menu
            return true;
        }
    }
}
