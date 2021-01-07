using BalTelegramBot.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BalTelegramBot.Models.Commands.Menu
{
    public class MenuItemsCommand
    {
        private UserInfo User { get; set; }
        public class Scheduler : Command
        {
            private string InlineKeyboardCommand => new ConcreteDayScheduler().Name;
            public override string Name => "Розклад 📆";
            private static string MainMenuItemCommand => new MainMenuCommand().MenuName;


            public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
            {
                if(await GoogleSpreadsheetController.ConnectToSheetsAsync() == true)  // connect to google spreadsheet
                {
                    var keyboard = GenerateInlineKeyBoardAsync().Result;
                    await client.SendTextMessageAsync(userInformation.ChatId, 
                                                        text: InlineKeyboardCommand, replyMarkup: keyboard);
                    return true;
                }
                await client.SendTextMessageAsync(userInformation.ChatId,
                                                    text: "нажаль сталася технічна помилка. Спробуйте будь ласка пізніше. ");
                return true;
            }

            private async Task<InlineKeyboardMarkup> GenerateInlineKeyBoardAsync()
            { 
                return await Task.Run(() => 
                { 
                    IList<List<InlineKeyboardButton>> btns = new List<List<InlineKeyboardButton>>();
                    btns.Add(new List<InlineKeyboardButton>() { new InlineKeyboardButton { Text = "Сьогодні", CallbackData = DateTime.Now.DayOfWeek.ToString() } });
                    btns.Add(new List<InlineKeyboardButton>() { new InlineKeyboardButton { Text = "Завтра", CallbackData = DateTime.Today.AddDays(1).DayOfWeek.ToString() } });
                    btns.Add(new List<InlineKeyboardButton>() { new InlineKeyboardButton { Text = "Післязавтра", CallbackData = DateTime.Today.AddDays(2).DayOfWeek.ToString() } });
                    btns.Add(new List<InlineKeyboardButton>() { new InlineKeyboardButton { Text = "Весь тиждень", CallbackData = "AllWeek" } });
                    btns.Add(new List<InlineKeyboardButton>() { new InlineKeyboardButton { Text = "Повернутись", CallbackData = MainMenuItemCommand } });


                    return new InlineKeyboardMarkup(btns);
                });
            }
        }

        public class Alerts : Command
        {
            public override string Name => "Оголошення 🛎";

            public override Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
            {
                throw new NotImplementedException();
            }
            
            enum RecipientAlertType
            {
                All,
                Teachers,
                Pupil,
                ClassLeaders
            }
        }

        public class Settings : Command
        {
            public override string Name => "Налаштування ⚙️";
            public string SettingsCommandText => "/settings";

            public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
            {
                var sc = new SettingCommand.Menu();
                var respMessage =sc.Name;
    
                await client.SendTextMessageAsync(chatId: userInformation.ChatId,
                                                    text: respMessage,
                                                    replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>() {
                                                            new InlineKeyboardButton() {Text = sc.BotInformationButtonText, CallbackData = sc.BotInformationButtonText},
                                                            new InlineKeyboardButton() {Text = sc.AlertsButtonText, CallbackData = sc.AlertsButtonText}
                                                    }));

                return true;
            }

            public override bool Contains(Message message)
            {
                if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                    return false;
                return message.Text == Name || message.Text == SettingsCommandText;
            }
        }

        public class AboutSchool : Command
        {
            public override string Name => "Інформація про заклад 🏫";
            public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
            {
                string msg = "Бородянський академічний ліцей  🏫є флагманом освіти 🏁 Бородянської об’єднаної територіальної громади, однією з кращих у Київській області. Заклад відкрито в 1985 році 🏗\r\n🧑‍💼 Директор ліцею – Лазутіна Олена Вікторівна.";
                IReplyMarkup keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton() {Url = "http://brschool-2.in.ua/?page_id=5", Text = "Детальніше 🔗" });
                await client.SendTextMessageAsync(chatId: userInformation.ChatId, text: msg, replyMarkup: keyboard);
                return true;
            }
        }

        public class Contacts : Command
        {
            public override string Name => "Контакти 📞";
            public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
            {
                string msg = "<b>Наша адреса:</b>\n📍07800, Україна, Київська область, смт. Бородянка, вул. Паркова, 5\n 📠 телефон/факс: (04577) 5-10-35\n ☎️ телефон: (04577) 5-10-34\n📭 e-mail: brschool2_85@ukr.net";
                IReplyMarkup keyboard = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>()
                {
                    new List<InlineKeyboardButton>() { new InlineKeyboardButton(){ Url = "https://instagram.com/bordo.lyceum?igshid=inf4b8fk20cq", Text = "🔗 instagram"}},
                    new List<InlineKeyboardButton>() {new InlineKeyboardButton(){ Url = "https://www.facebook.com/groups/131390164164492/", Text =  "🔗 facebook"}},
                    new List<InlineKeyboardButton>() {new InlineKeyboardButton(){ Url = "https://t.me/stadybal", Text = "🔗 telegram канал"}},
                    new List<InlineKeyboardButton>() {new InlineKeyboardButton(){ Url = "https://www.google.com/maps/dir//50.6473448,29.9279487/@50.647345,29.927949,14z?hl=ru-RU", Text = "🏁 Як доїхати?"}}
                    
                });
                await client.SendTextMessageAsync(chatId: userInformation.ChatId, text: msg, replyMarkup: keyboard, parseMode: ParseMode.Html);
                return true;
            }
        }
    }
}
