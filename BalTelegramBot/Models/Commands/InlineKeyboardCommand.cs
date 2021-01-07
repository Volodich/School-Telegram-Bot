using System;
using System.Collections.Generic;
using BalTelegramBot.Controllers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BalTelegramBot.Models.Commands.Menu;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BalTelegramBot.Models.Commands
{
    public abstract class InlineKeyboardCommand
    {
        internal abstract string Name { get; }

        internal abstract Task Execute(CallbackQuery message, TelegramBotClient client, UserInfo userInformation);

        internal virtual bool Contains(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return message.Text.Contains(this.Name);
        }
    }

    public class ConcreteDayScheduler : InlineKeyboardCommand
    {
        internal override string Name => "оберіть потрібний день";
        private static string MainMenuItemCommandName => new MainMenuCommand().MenuName;


        internal override async Task Execute(CallbackQuery message, TelegramBotClient client, UserInfo userInformation)
        {
            if (message.Data == MainMenuItemCommandName)
            {
                await new MainMenuCommand().Execute(message.Message, client, userInformation);
                return;
            }

            string answer; // bots answer

            string dayOfWeek = message.Data;

            if (userInformation.TypeUser == TypeUser.Pupil.ToString()) // Pupil
            {
                answer = await Scheduler.Scheduler.GenerateSchedulerMessage(scheduler: new Scheduler.Scheduler.Pupil(),
                    userInformation: userInformation, day: dayOfWeek);
            }
            else if (userInformation.TypeUser == TypeUser.Teacher.ToString()) // Teacher
            {
                answer = await Scheduler.Scheduler.GenerateSchedulerMessage(
                    scheduler: new Scheduler.Scheduler.Teacher(),
                    userInformation: userInformation, day: dayOfWeek);
            }
            else
            {
                answer = "щось пішло не так...🤔 Спробуйте будь-ласка пізніше.";
            }

            await client.SendTextMessageAsync(userInformation.ChatId, text: answer,
                replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton() {Text = MainMenuItemCommandName})
                    {ResizeKeyboard = true});
        }
    }

    public class ConfirmPupilRegistration : InlineKeyboardCommand
    {
        internal override string Name => "Запит на реєстрацію в боті.";

        internal override async Task Execute(CallbackQuery message, TelegramBotClient client, UserInfo userInformation)
        {
            if (message.Data != null)
            {
                Registration_State_Machine.RegistrationState.DecipherRegistartionResult(message.Data,
                    out long pupilChatId, out bool resultRegistration);

                var user = await BalDbController.GetUserInformationAsync(pupilChatId);
                if (user.IsRegistred == true) //-V3080
                    return;

                Registration_State_Machine.RegistrationState registration =
                    new Registration_State_Machine.RegistrationState(client, userInformation)
                    {
                        StateRegistration = Registration_State_Machine.RegistrationStateMachine
                            .WaitingForConfirmation
                    };

                string messageCreator = resultRegistration == true 
                    ? $"Користувач chatId: {pupilChatId} [{user.NameUser}](tg://user?id={pupilChatId}) додан до системи" 
                    : $"Користувачеві chatId: {pupilChatId} [{user.NameUser}](tg://user?id={pupilChatId}) відмовлено";
                await client.SendTextMessageAsync(chatId: AppSettings.ChatIdCreator, text: messageCreator,
                    parseMode: ParseMode.Markdown);
                string messageAdmin = resultRegistration
                    ? $"Користувач: [{user.NameUser}](tg://user?id={pupilChatId}) додан до системи"
                    : $"Користувачеві: [{user.NameUser}](tg://user?id={pupilChatId}) відмовлено";
                await client.SendTextMessageAsync(chatId: userInformation.ChatId,
                    text: messageAdmin, ParseMode.Markdown);

                await registration.HandleStateRegistrationAsync(new Message()
                {
                    Text = resultRegistration.ToString(),
                    Chat = new Chat() {Id = pupilChatId}
                });
            }
        }

        internal override bool Contains(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return Regex.IsMatch(message.Text, Name);
        }

    }

    /// <summary>
    /// Send message from pupil
    /// </summary>
    public class SendTeacherMessage : InlineKeyboardCommand
    {
        internal override string Name => "🗂 Інформація про ";

        internal override async Task Execute(CallbackQuery message, TelegramBotClient client, UserInfo userInformation)
        {
            if (message.Data is null)
            {
                return;
            }

            var teacher = await BalDbController.GetTeacherInformationAsync(message.Data);
            var responceMessage = teacher == null
                ? $"Повідомленя неможливо надіслати вчителю *{message.Data}*. Вчитель не зареєстрований у системі."
                : "Введіть текст повідомлення: ";

            await client.SendTextMessageAsync(chatId: userInformation.ChatId,
                text: responceMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            userInformation.State =
                SendMessagesCommand.SendMessageState.FromPupilToTeacher.ToString() + "_" + message.Data;
            await BalDbController.ChangeUserStateAsync(userInformation.State, userInformation.ChatId);
        }

        internal override bool Contains(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return Regex.IsMatch(message.Text, Name);
        }
    }

    public class SendPupilsMessage : InlineKeyboardCommand
    {
        internal override string Name => "🗒 Інформація про ";

        internal override async Task Execute(CallbackQuery message, TelegramBotClient client, UserInfo userInformation)
        {
            if (message.Data is null)
            {
                return;
            }

            var pupils = await BalDbController.GetPupilsAsync(message.Data);
            string responceMessage;
            if (pupils is null || pupils.Count < 1)
            {
                responceMessage =
                    $"Повідомленя неможливо надіслати *{message.Data}* класу. Жоден учень не зареєстрований у системі.";
            }
            else
            {
                responceMessage = "Введіть текст повідомлення: ";
            }

            await client.SendTextMessageAsync(chatId: userInformation.ChatId,
                text: responceMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            userInformation.State =
                SendMessagesCommand.SendMessageState.FromTeacherToPupils.ToString() + "_" + message.Data;
            await BalDbController.ChangeUserStateAsync(userInformation.State, userInformation.ChatId);
        }

        internal override bool Contains(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return Regex.IsMatch(message.Text, Name);
        }
    }

    public class SettingCommand
    {
        private static string NoSmile => " ❌";
        private static string YesSmile => " ✅";
        public class Menu : InlineKeyboardCommand
        {
            internal override string Name =>
                "Ви перейшли у налаштування бота. Ви можете дізнатися більше про можливості користуванням ботом. Чи увімкнути нагадування.";

            public string AlertsButtonText => "Нагадування 🛎";
            public string BotInformationButtonText => "Про бота ❓🤖";

            internal override async Task Execute(CallbackQuery message, TelegramBotClient client,
                UserInfo userInformation)
            {
                if (message == null)
                    return;

                if (message.Data == AlertsButtonText)
                {
                    var msg = new Alerts().Name + " В залежності від обраного часу - бот буде надсилати розклад на наступний день, нагадуючи які у вас будуть уроки (класи).\nЦю функцію ви зможете відключити у будь який момент.";
                    string text2000 = "20:00";
                    string text0700 = "7:30";
                    var selector = (SchedulerAlertsController.TimeSendNotification)Enum.Parse(typeof(SchedulerAlertsController.TimeSendNotification),
                        userInformation.SettingNotification);
                    switch (selector)
                    {
                        case SchedulerAlertsController.TimeSendNotification.Evning2000:
                            text2000 += YesSmile;
                            text0700 += NoSmile;
                            break;
                        case SchedulerAlertsController.TimeSendNotification.Morning0730:
                            text2000 += NoSmile;
                            text0700 += YesSmile;
                            break;
                        case SchedulerAlertsController.TimeSendNotification.EvMorning:
                            text2000 += YesSmile;
                            text0700 += YesSmile;
                            break;
                        case SchedulerAlertsController.TimeSendNotification.Disabled:
                            text2000 += NoSmile;
                            text0700 += NoSmile;
                            break;
                    }
                    
                    await client.SendTextMessageAsync(chatId: userInformation.ChatId,
                        text: msg,
                        replyMarkup: new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>()
                        {
                            new List<InlineKeyboardButton>()
                            {
                                new InlineKeyboardButton()
                                {
                                    Text = text2000,
                                    CallbackData = SchedulerAlertsController.TimeSendNotification.Evning2000.ToString()
                                }
                            },
                            new List<InlineKeyboardButton>()
                            {
                                new InlineKeyboardButton()
                                {
                                    Text = text0700,
                                    CallbackData = SchedulerAlertsController.TimeSendNotification.Morning0730.ToString()
                                }
                            },
                            new List<InlineKeyboardButton>()
                            {
                                new InlineKeyboardButton()
                                {
                                    Text = "Вимкнути ⛔️",
                                    CallbackData = SchedulerAlertsController.TimeSendNotification.Disabled.ToString()
                                }
                            }
                        }));
                }

                if (message.Data == BotInformationButtonText)
                {
                    var msg =
                        $"Що таке бот 🤖❓\r\nЦе програма, що живе у телеграмі. Вона відповідає 24/7. Завжди допоможе та підкаже 😉\r\nНавіщо потрібен❔\r\n📆 подивитись розклад. Ви можете у два клики подивитися розклад на сьогодні, завтра чи тиждень.\r\n✉️ написати учням термінове оголошення. Ви можете написати усім учням класу важливу інформацію. \r\n🛎 нагадати з вечора чи ранку розклад на день, щоб не помолитись. \r\n\r\nЯкщо є питання ➡️ {AppSettings.Admin}";

                    await client.SendTextMessageAsync(chatId: userInformation.ChatId,
                        text: msg);
                }

            }
        }

        public class Alerts : InlineKeyboardCommand
        {
            internal override string Name =>
                "Нагадування - це функція, яка допомагає взяти потрібні речі до школи.";

            internal override async Task Execute(CallbackQuery message, TelegramBotClient client,
                UserInfo userInformation)
            {
                if (message == null)
                    return;

                string msg = default;
                if (message.Data == SchedulerAlertsController.TimeSendNotification.Evning2000.ToString())
                {
                    if (userInformation.SchedulerNotification == false)
                    {
                        msg = "Сповіщення увімкненні. Вони надходитимуть з пн-пт о 20:00.";
                        userInformation.SchedulerNotification = true;
                        userInformation.SettingNotification =
                            SchedulerAlertsController.TimeSendNotification.Evning2000.ToString();
                    }
                    else // if alerts true
                    {
                        if (userInformation.SettingNotification ==
                            SchedulerAlertsController.TimeSendNotification.EvMorning.ToString())
                        {
                            userInformation.SettingNotification =
                                SchedulerAlertsController.TimeSendNotification.Morning0730.ToString();
                            msg = "Сповіщення з пн-пт о 20:00 вимкненні.";
                        }
                        else if(userInformation.SettingNotification == SchedulerAlertsController.TimeSendNotification.Morning0730.ToString())
                        {
                            userInformation.SettingNotification =
                                SchedulerAlertsController.TimeSendNotification.EvMorning.ToString();
                            msg = "Сповіщення увімкненні. Вони надходитимуть з пн-пт о 20:00.";
                        } 
                        else if (userInformation.SettingNotification ==
                                   SchedulerAlertsController.TimeSendNotification.Evning2000.ToString())
                        {
                            userInformation.SettingNotification =
                                SchedulerAlertsController.TimeSendNotification.Disabled.ToString();
                            userInformation.SchedulerNotification = false;
                            msg = "Сповіщення з пн-пт о 20:00 вимкненні.";
                        }
                    }

                }

                if (message.Data == SchedulerAlertsController.TimeSendNotification.Morning0730.ToString())
                {
                    if (userInformation.SchedulerNotification == false)
                    {
                        msg = "Сповіщення увімкненні. Вони надходитимуть з пн-пт о 07:30.";
                        userInformation.SchedulerNotification = true;
                        userInformation.SettingNotification =
                            SchedulerAlertsController.TimeSendNotification.Morning0730.ToString();
                    }
                    else // if alerts true
                    {
                        if (userInformation.SettingNotification ==
                            SchedulerAlertsController.TimeSendNotification.EvMorning.ToString())
                        {
                            userInformation.SettingNotification =
                                SchedulerAlertsController.TimeSendNotification.Evning2000.ToString();
                            msg = "Сповіщення з пн-пт о 07:30 вимкненні.";
                        } 
                        else if (userInformation.SettingNotification == SchedulerAlertsController.TimeSendNotification.Evning2000.ToString())
                        {
                            msg = "Сповіщення увімкненні. Вони надходитимуть з пн-пт о 07:30.";
                            userInformation.SettingNotification =
                                SchedulerAlertsController.TimeSendNotification.EvMorning.ToString();
                        }
                        else if (userInformation.SettingNotification ==
                                 SchedulerAlertsController.TimeSendNotification.Morning0730.ToString())
                        {
                            userInformation.SettingNotification =
                                SchedulerAlertsController.TimeSendNotification.Disabled.ToString();
                            userInformation.SchedulerNotification = false;
                            msg = "Сповіщення з пн-пт о 07:30 вимкненні.";
                        }
                    }
                }

                if (message.Data == SchedulerAlertsController.TimeSendNotification.Disabled.ToString())
                {
                    msg = "Усі сповіщення вимкненні.";
                    userInformation.SettingNotification =
                        SchedulerAlertsController.TimeSendNotification.Disabled.ToString();
                    userInformation.SchedulerNotification = false;
                }

                await BalDbController.UpdateUserDataAsync(userInformation);

                await client.SendTextMessageAsync(chatId: userInformation.ChatId,
                    text: msg);
            }

            internal override bool Contains(Message message)
            {
                if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                    return false;

                return Regex.IsMatch(message.Text, Name);
            }
        }
    }
}
