using System.Collections.Generic;
using System.Threading.Tasks;
using BalTelegramBot.Models.Commands.Registration_State_Machine;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BalTelegramBot.Models.Commands.Menu
{
    public class MainMenuCommand : Command
    {
        public override string Name => "/menu";

        public string MenuName => "Головне меню 🎛";

        private string SchedulerButtonText => new MenuItemsCommand.Scheduler().Name;
        private string SettingsButtonText => new MenuItemsCommand.Settings().Name;
        private string AboutSchoolButtonText => new MenuItemsCommand.AboutSchool().Name;
        private string ContactsButtonText => new MenuItemsCommand.Contacts().Name;
        private string RegistrationButtonText => new RegistrationReplyButtonCommand().Name;
        public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
        {
            IReplyMarkup keyboard = null;
            if (userInformation.TypeUser == TypeUser.Guest.ToString())
            {
                List<List<KeyboardButton>> list = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>() {new KeyboardButton() {Text = RegistrationButtonText}, 
                                                new KeyboardButton() {Text = ContactsButtonText}},
                    new List<KeyboardButton>() {new KeyboardButton() {Text = AboutSchoolButtonText}}
                };
                keyboard = new ReplyKeyboardMarkup(list) { ResizeKeyboard = true, OneTimeKeyboard = true };
            } else if ((userInformation.TypeUser == TypeUser.Pupil.ToString() ||
                        userInformation.TypeUser == TypeUser.Teacher.ToString()) && userInformation.IsRegistred == true)
            {
                List<List<KeyboardButton>> list = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>() {new KeyboardButton() {Text = SchedulerButtonText}},
                    new List<KeyboardButton>() {new KeyboardButton() {Text = SettingsButtonText}}
                };
                keyboard = new ReplyKeyboardMarkup(list) { ResizeKeyboard = true, OneTimeKeyboard = true };
            }
            else
            {
                await client.SendTextMessageAsync(chatId: userInformation.ChatId, text: "Завершіть реєстрацію в боті.");
                await new RegistrationState(client, userInformation).HandleStateRegistrationAsync(message);
                return true;
            }
            //HACK: add in latest version //mainKeyboard.Add(new List<KeyboardButton>() { new KeyboardButton() { Text = AlertsButtonText } });

            await client.SendTextMessageAsync(userInformation.ChatId, 
                                               text: "Головне меню",
                                               replyMarkup: keyboard);
            return true;

        }

        public override bool Contains(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;
            return message.Text.Contains(Name) || message.Text.Contains(MenuName);
        }
    }
}
