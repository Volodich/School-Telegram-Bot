using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using BalTelegramBot.Controllers;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Generic;
using BalTelegramBot.Models.Commands.Menu;

namespace BalTelegramBot.Models.Commands
{
    public class StartCommand : Command
    {
        public override string Name => @"/start";
        private string RegistartionButtonText  => new RegistrationReplyButtonCommand().Name;
        private string LoginGuestButtonText => new LoginGuestReplyButtonCommand().Name; 

        public override async Task<dynamic> Execute(Message message, TelegramBotClient botClient, UserInfo userInformation)
        {
            if (userInformation != null) 
            {  
                if(userInformation.IsRegistred == true || userInformation.TypeUser == TypeUser.Guest.ToString()) // Main Menu
                {
                    await Task.Run(() => new MainMenuCommand().Execute(null, botClient, userInformation)); // go to main menu
                    return true;
                }
            }
            else //  Hello start message && Create data user in db
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, 
                    text: "Привіт! Я радий, що ти вирішив мене запустити 🙂 Через декілька секунд я почну тобі допомогати. Зачекай будь ласка ⏳",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    replyMarkup: new ReplyKeyboardRemove());

                await BalDbController.CreateUserInDbAsync(new UserInfo() { ChatId = message.Chat.Id, NameTelegram = message.Chat.FirstName + " " + message.Chat.LastName });
            }
            // Start- State Machine Registration
            ReplyKeyboardMarkup registrationKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton>()
            {
                new KeyboardButton() {Text = RegistartionButtonText},
                new KeyboardButton() {Text = LoginGuestButtonText}
            }) {ResizeKeyboard = true};

            await botClient.SendTextMessageAsync(message.Chat.Id, 
            text: $"Нажаль, не можу знайти Вас у системі. Якщо Ви вже зареєстровані - напишіть адміністратору - {AppSettings.Admin}. Або зареєструйтесь для повноцінного користування ботом 🤖👌",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: registrationKeyboard);
            return true; 
        }
    }
}
