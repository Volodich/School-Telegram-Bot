using BalTelegramBot.Controllers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BalTelegramBot.Models.Commands.Menu;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BalTelegramBot.Models.Commands.Registration_State_Machine
{
    public class RegistrationState
    {
        // User command in bot after registration
        public string TeacherButtonText => "Вчитель 📚";
        public string CorrectInformationUserButtonText => "Все вірно ✅";
        public string InCorrectInformationUserButtonText => "Змінити ❌";
        public string NotClassmatesTeacherButtonText => "Немає класу 😢";
        public string ConfirmRegistrationButtonText => "Підтвердити ✅";
        public string NotConfirmRegistrationButtonText => "Відхилити ❌";

        public string SendPhoneButtonText => "Надіслати номер 📱";
        internal TelegramBotClient BotClient { get; set; }
        internal UserInfo User { get; set; }
        internal RegistrationStateMachine StateRegistration { get; set; }

        public RegistrationState(TelegramBotClient botClient, UserInfo userInformation)
        {
            User = userInformation;
            BotClient = botClient;
        }

        internal async Task HandleStateRegistrationAsync(Message message)
        {
            string userMessage = message.Text;
            if (StateRegistration == default)
            {
                StateRegistration = (RegistrationStateMachine)Enum.Parse(typeof(RegistrationStateMachine), User.State);
            }
            switch (StateRegistration)
            {
                case RegistrationStateMachine.None: // S-0
                    {
                        StateRegistration = RegistrationStateMachine.EnterRole;
                        await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), User.ChatId); // Next state (s-1)

                        var answerText = $"Якщо Ви учень ліцею, ведіть пароль, для користуванням ботом. Його можна отримати у класного керівника або написавши адміністратору {AppSettings.Admin}.  Якщо Ви вчитель - натисніть кнопку *{TeacherButtonText}* - щоб перейти далі.";

                        await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                                             text: answerText,
                                                             parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                                                             replyMarkup: new ReplyKeyboardMarkup(new List<KeyboardButton>() { 
                                                                          new KeyboardButton() { Text = TeacherButtonText} }) { ResizeKeyboard = true });
                        break;
                    }
                case RegistrationStateMachine.EnterRole: // S-1
                    // connect to db
                    {
                        if (userMessage == TeacherButtonText) // if Teacher
                        {
                            StateRegistration = RegistrationStateMachine.EnterTeacherPassword;
                            await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), User.ChatId); // Next state (s-2)

                            await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                                                 text: $@"Адміністратор боту <a href= ""tg://user?id=363574232"">{AppSettings.NameAdmin}</a> надасть Вам унікальний пароль. Зверніться будь ласка до нього.",
                                                                 parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                                                 replyMarkup: new ReplyKeyboardRemove());
                            return;
                        }
                        // if pupil
                        User.TypeUser = await ConfirmUserRoleAsync(userMessage); // Confirm password from pupil
                        if (User.TypeUser != null)
                        {
                            StateRegistration = RegistrationStateMachine.EnterName;
                            await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), User.ChatId); // Next state (s-2)
                            await BalDbController.AddPupilAsync(User.ChatId)
                                .ContinueWith(_ => BalDbController.UpdateUserDataAsync(User));

                            await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                                            text: $"Вітаємо! Ви успішно авторизувались у системі, як учень ліцею. Як до Вас звертатись? (Напишіть своє прізвище, ім'я, по-батькові українською мовою)",
                                                            replyMarkup: new ReplyKeyboardRemove());
                        }
                        else await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                                            text: @"Секретний ключ не підійшов 😢 Спробуйте будь-ласка ще раз 🙂",
                                                            replyMarkup: new ReplyKeyboardRemove());
                        break;
                    }
                case RegistrationStateMachine.EnterTeacherPassword: // S-2
                    {
                        if (userMessage == null)
                            return;
                        string responceMessage = default;

                        var teacher = await ConfirmTeacherPasswordAsync(userMessage);

                        if (teacher == null)
                        {
                            responceMessage  = User.TypeUser == TypeUser.Pupil.ToString() ? $"Вітаємо! Ви успішно авторизувались у системі, як учень ліцею. Як до Вас звертатись? (Напишіть своє прізвище, ім'я, по-батькові українською мовою)" : "Пароль не підійшов. Спробуйте будь ласка ще.";
                            await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                text: responceMessage,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                            return;
                        }
                        responceMessage = $"Вітаємо! Ви зареєструвалися у системі як: *{teacher.FullName}*.\nПредмет, який викладаєте: *{teacher.Subjects}*.";
                        User.NameUser = teacher.FullName;
                        User.TypeUser = TypeUser.Teacher.ToString();
                        User.Teachers.Add(teacher);

                        if (await BalDbController.AddTeacherAsync(User.ChatId) == true) // if teacher add complete
                        {
                            StateRegistration = RegistrationStateMachine.EnterPhone;
                            await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), User.ChatId); // Next state (s-3)
                            await BalDbController.UpdateUserDataAsync(User);

                            await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                text: responceMessage,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                            await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                text: $"{User.NameUser} теперь введіть свій телефон або надішліть його",
                                replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton() {Text = SendPhoneButtonText, RequestContact = true})
                                            {ResizeKeyboard = true});
                        }
                        else 
                        {
                            responceMessage = "Вчитель з таким паролем вже зареєстрований у системі.";
                            await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                text: responceMessage,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        }
                    }
                    break;
                case RegistrationStateMachine.EnterName: // S-3
                    {
                        User.NameUser = userMessage;
                        StateRegistration = RegistrationStateMachine.EnterPhone;
                        await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), User.ChatId); // Next state (s-4) 
                        await BalDbController.UpdateUserDataAsync(User); // Send user name in db

                        await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                                            text: $"{User.NameUser} теперь введіть свій телефон або надішліть його",
                                                                replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton() { Text = SendPhoneButtonText, RequestContact = true })
                                                                { ResizeKeyboard = true });
                        break;
                    }
                case RegistrationStateMachine.EnterPhone: // S-4
                    {
                        User.Phone = message.Text ?? message.Contact.PhoneNumber;

                        await BalDbController.UpdateUserDataAsync(User); // Send user phone to db

                        var botMessage = $"Сталася помилка при регістрації. Напишіть адміну -> {AppSettings.Admin}";
                        IReplyMarkup keyboard = new ReplyKeyboardRemove();

                        StateRegistration = RegistrationStateMachine.EnterClass;
                        await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), User.ChatId); // Next state (s-5) 
                        await BalDbController.UpdateUserDataAsync(User); // Send user name in db

                        if(User.TypeUser == TypeUser.Teacher.ToString())
                        {
                            botMessage = $"Якщо Ви маєте класне керівництво - введіть будь ласка Ваш клас: (Наприклад *5А* чи *8Г*).\nЯкщо у вас немає класу - натисніть кнопку {NotClassmatesTeacherButtonText}";
                            keyboard = new ReplyKeyboardMarkup(new KeyboardButton(NotClassmatesTeacherButtonText)) { ResizeKeyboard = true };
                        }
                        if(User.TypeUser == TypeUser.Pupil.ToString())
                        {
                            botMessage = @"Введіть клас, в якому навчаєтесь. Пам'ятайте, змінити потім клас буде неможливо! Наприклад: *11А* чи *9Б*";
                        }
                        
                        await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                                            text: botMessage,
                                                            replyMarkup: keyboard,
                                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        break;
                    }
                case RegistrationStateMachine.EnterClass: // S-5
                    {
                        string botMessage = default;
                        // Check correct input class
                        {
                            string pattern = @"^[0-9A-ZА-ЯЁ]{2,3}$";
                            if (Regex.IsMatch(message.Text, pattern) == false &&
                                Regex.IsMatch(message.Text, NotClassmatesTeacherButtonText) == false)
                            {
                                await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                    text: "Неправильно введений клас. Спробуйте ще раз. (_Приклад: 5А, 11Б_)",
                                    parseMode: ParseMode.Markdown);
                                return;
                            }
                        }
                        if(User.TypeUser == TypeUser.Teacher.ToString()) // if teacher
                        {
                            User = await BalDbController.GetTeacherInformationAsync(User.ChatId);
                            User.Teachers.First().Class = message.Text == NotClassmatesTeacherButtonText ? "--" : message.Text;

                            string name = $"\nПрізвище, ім'я, по-батькові: *{User.NameUser}*";
                            string phone = $"\nНомер телефону: *{User.Phone}*";
                            string subject = $"\nПредмет: *{User.Teachers.First().Subjects}*";
                            string @class = $"\nКласне керівництво: *{User.Teachers.First().Class}*";
                            botMessage = $"Будь-ласка перевірте введену інформацію.{name}{phone}{subject}{@class}\nЯкщо вона правильна - натисніть {CorrectInformationUserButtonText}, якщо ні - {InCorrectInformationUserButtonText}";
                        }
                        if (User.TypeUser == TypeUser.Pupil.ToString()) // if pupil
                        {
                            User.Pupils.Add(new Pupils() { Class = message.Text });

                            string name = $"\nПрізвище, ім'я, по-батькові: *{User.NameUser}*";
                            string phone = $"\nНомер телефону: *{User.Phone}*";
                            string @class = $"\nКлас: *{User.Pupils.First().Class}*";
                            botMessage = $"Будь-ласка перевірте введену інформацію.{name}{phone}{@class}\nЯкщо вона правильна - натисніть {CorrectInformationUserButtonText}, якщо ні - {InCorrectInformationUserButtonText}";
                        }
                        await BalDbController.UpdateUserDataAsync(User);

                        StateRegistration = RegistrationStateMachine.CheckInformation;
                        await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), User.ChatId); // Next state (s-6) 

                        await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                                            text: botMessage,
                                                            replyMarkup: new ReplyKeyboardMarkup(new List<KeyboardButton>(){
                                                                                                        new KeyboardButton() { Text = CorrectInformationUserButtonText},
                                                                                                        new KeyboardButton() { Text = InCorrectInformationUserButtonText } })
                                                            { ResizeKeyboard = true },
                                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        break;
                    }
                case RegistrationStateMachine.CheckInformation: // S-6
                    {
                        if (userMessage == CorrectInformationUserButtonText)
                        {
                            StateRegistration = RegistrationStateMachine.WaitingForConfirmation;
                            await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), User.ChatId); // Check for admin confirm
                            
                            string adminMessage = default;
                            string name = default;
                            string phone = default;
                            string @class = default;

                            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new List<InlineKeyboardButton>(){
                                                                                            new InlineKeyboardButton() {Text = ConfirmRegistrationButtonText, CallbackData = EncryptionRegistartionResult(User.ChatId, true)},
                                                                                            new InlineKeyboardButton() {Text = NotConfirmRegistrationButtonText, CallbackData = EncryptionRegistartionResult(User.ChatId, false)}
                            });

                            if (User.TypeUser == TypeUser.Pupil.ToString())
                            {
                                var info = await BalDbController.GetPupilInformationAsync(User.ChatId);
                                name = $"\nУчень: [{User.NameUser}](tg://user?id={User.ChatId})";
                                phone = $"\nНомер телефону: *{info.Phone}*";
                                @class = $"\nКлас: *{info.Pupils.First().Class}*";
                                adminMessage = $"{new ConfirmPupilRegistration().Name}{name}{phone}{@class}\n";
                            }
                            if(User.TypeUser == TypeUser.Teacher.ToString())
                            {
                                var info = await BalDbController.GetTeacherInformationAsync(User.ChatId);
                                name = $"\nВчитель: [{User.NameUser}](tg://user?id={User.ChatId})";
                                phone = $"\nНомер телефону: *{info.Phone}*";
                                @class = $"\nКласний керівник: *{info.Teachers.First().Class}*";
                                string subject = $"\nПредмет: *{info.Teachers.First().Subjects}*";
                                adminMessage = $"{new ConfirmPupilRegistration().Name}{name}{phone}{@class}{subject}\n";
                            }
                            
                            await BotClient.SendTextMessageAsync(User.ChatId,
                                                                text: "Запит на реєстрацію відправлено. Чекайте сповіщення про результат 🙂",
                                                                replyMarkup: new ReplyKeyboardRemove());

                            var admins = await GetAdmins();
                            foreach (var admin in admins)
                            {
                                await BotClient.SendTextMessageAsync(admin.ChatId,
                                                                    text: adminMessage,
                                                                    replyMarkup: inlineKeyboard,
                                                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                            }
                            break;
                        }
                        if (userMessage == InCorrectInformationUserButtonText)
                        {
                            string responceMessage = "default";
                            IReplyMarkup keyboard = new ReplyKeyboardRemove();
                            if (User.TypeUser == TypeUser.Pupil.ToString())
                            {
                                StateRegistration = RegistrationStateMachine.EnterName; // Return to S-3 State
                                responceMessage = $"Як до Вас звертатись?  (Напишіть своє прізвище, ім'я, по-батькові українською мовою)";
                            }
                            if(User.TypeUser == TypeUser.Teacher.ToString())
                            {
                                StateRegistration = RegistrationStateMachine.EnterPhone; // Return to S-4 State
                                responceMessage = $@"Адміністратор боту <a href= ""tg://user?id=363574232"">{AppSettings.NameAdmin}</a> надасть Вам унікальний пароль. Зверніться будь ласка до нього.";
                            }
                            await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), User.ChatId);

                            await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                                                 text: responceMessage,
                                                                 replyMarkup: keyboard);
                        }
                        break;
                    }
                case RegistrationStateMachine.WaitingForConfirmation: // S-7
                    {
                        if (bool.TryParse(message.Text, out bool result) == true)
                        {
                            if (Convert.ToBoolean(message.Text) == false) // if registred not confirm
                            {
                                var requestUser = await BalDbController.GetUserInformationAsync(message.Chat.Id);

                                await BotClient.SendTextMessageAsync(chatId: requestUser.ChatId,
                                                                    text: "Нажаль, Вам відмовлено в реєстрації за введеними даними. Виправте данні, та спробуйте ще раз.");
                                string responceMessage = "default";
                                IReplyMarkup keyboard = new ReplyKeyboardRemove();
                                if (requestUser.TypeUser == TypeUser.Pupil.ToString())
                                {
                                    StateRegistration = RegistrationStateMachine.EnterName; // Return to S-3 State
                                    responceMessage = $"Як до Вас звертатись?  (Напишіть своє прізвище, ім'я, по-батькові українською мовою)";
                                }
                                if (requestUser.TypeUser == TypeUser.Teacher.ToString())
                                {
                                    StateRegistration = RegistrationStateMachine.EnterTeacherPassword; // Return to S-4 State
                                    responceMessage = $@"Адміністратор боту <a href= ""tg://user?id=363574232"">{AppSettings.NameAdmin}</a> надасть Вам унікальний пароль. Зверніться будь ласка до нього.";
                                }

                                await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), requestUser.ChatId); 
                                await BotClient.SendTextMessageAsync(chatId: requestUser.ChatId, 
                                                                     text: responceMessage,
                                                                     replyMarkup: keyboard,
                                                                     parseMode: ParseMode.Html);

                                return;
                            }
                            else // true
                            {
                                var requestUser = await BalDbController.GetUserInformationAsync(message.Chat.Id);
                                
                                StateRegistration = RegistrationStateMachine.Registred;
                                await BalDbController.ChangeUserStateAsync(StateRegistration.ToString(), requestUser.ChatId); // Finaly state 

                                requestUser.IsRegistred = true;
                                await BalDbController.UpdateUserDataAsync(requestUser);
                               
                                if (requestUser.TypeUser == TypeUser.Teacher.ToString())
                                {
                                    var teacherInfo = await BalDbController.GetTeacherInformationAsync(requestUser.ChatId);
                                    if (teacherInfo.Teachers.First().Class == "--")
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        var pupils = await BalDbController.GetPupilsAsync(teacherInfo.Teachers.First().Class); // Add pupils chatId her classmatesTeacher
                                        if (pupils != null && pupils.Count > 0)
                                        {
                                            foreach (var pupil in pupils)
                                            {
                                                var pupilInfo = await BalDbController.GetPupilInformationAsync(pupil.ChatId);
                                                pupilInfo.Pupils.First().ClassromTeacherId = teacherInfo.Teachers.First().Id;
                                                await BalDbController.UpdateUserDataAsync(pupilInfo);
                                            }
                                        }
                                    }
                                }
                                if(requestUser.TypeUser == TypeUser.Pupil.ToString()) // Add teacher Id to pupil
                                {
                                    var pupilInfo = await BalDbController.GetPupilInformationAsync(requestUser.ChatId);
                                    var teacherChatId = await BalDbController.GetClassmateTeacherAsync(pupilInfo.Pupils.First().Class);
                                    if(teacherChatId != default)
                                    {
                                        pupilInfo.Pupils.First().ClassromTeacherId = BalDbController.GetTeacherInformationAsync(teacherChatId).Result.Teachers.First().Id;
                                        await BalDbController.UpdateUserDataAsync(pupilInfo);
                                    }
                                }
                                await BotClient.SendTextMessageAsync(chatId: requestUser.ChatId, 
                                                            text: $"Ваш профіль підтверджено! Вітаємо у системі 🥳",
                                                                  replyMarkup: new ReplyKeyboardRemove());
                                await Task.Run(() => new MainMenuCommand().Execute(null, BotClient, requestUser)); // go to main menu
                                break;
                            }
                        }
                        else
                        {
                            await BotClient.SendTextMessageAsync(chatId: User.ChatId,
                                                                 text: $"Вашу заявку ще не підтвердили... Будь ласка зачекайте ⏳",
                                                                 replyMarkup: new ReplyKeyboardRemove());
                            break;
                        }
                    }
                default:
                    break;
            }
        }
       
        private async Task<string> ConfirmUserRoleAsync(string password) // Connect to db and check user password 
        {
            try
            {
                using (var db = new BalDbContext())
                {
                    var userRole = await db.PasswordInfo.Where<PasswordInfo>(pi => pi.Key == password).SingleAsync();
                    return userRole.Value;
                }
            }
            catch(System.InvalidOperationException)
            {
                return null;
            }
        }
        
        private async Task<Teachers> ConfirmTeacherPasswordAsync(string key)
        {
            if(await GoogleSpreadsheetController.ConnectToSheetsAsync() == true)
            {
                GoogleSpreadsheetController.TeacherInformation teacherInformation  = new GoogleSpreadsheetController.TeacherInformation();
                var datas = await teacherInformation.GetSheetDataAsync().
                    ContinueWith<IList<IList<object>>>(t => teacherInformation.GetTeacherInformation().Result);
                foreach (var data in datas)
                {
                    if(data.Count == 4 && key == data[1].ToString())
                    {
                        var teacher = new Teachers()
                        {
                            FullName = data[2].ToString(),
                            Subjects = data[3].ToString()
                        };
                        return teacher;
                    }
                }
                User.TypeUser = await ConfirmUserRoleAsync(key);
                if(User.TypeUser != null && User.TypeUser == TypeUser.Pupil.ToString()) // if pupil
                {
                    User.State = RegistrationStateMachine.EnterName.ToString();
                    await BalDbController.ChangeUserStateAsync(User.State, User.ChatId)
                        .ContinueWith(_ => BalDbController.AddPupilAsync(User.ChatId).Wait())
                        .ContinueWith(_ => BalDbController.UpdateUserDataAsync(User).Wait());
                }
            }
            return null;
        }
        

        internal async Task<List<UserInfo>> GetAdmins()
        {
            using(var db = new BalDbContext())
            {
                var admins = await db.UserInfo.Where(ui => ui.IsAdmin == true).ToListAsync();
                return admins;
            }
        }

        internal static string EncryptionRegistartionResult(long chatId, bool result)
        {
            return $"{chatId}_{result}";
        }
        internal static void DecipherRegistartionResult(string message, out long chatId, out bool result)
        {
            chatId = Convert.ToInt64(message.Split("_").First());
            string str = message.Split("_").Last();
            result = Convert.ToBoolean(str);
        }
    }

    public enum RegistrationStateMachine
    {
        None, // Not registration
        EnterRole, // S-1
        EnterTeacherPassword, // S-2
        EnterName, // S-3
        EnterPhone, // S-4
        EnterClass, // S-5
        CheckInformation, // S-6
        WaitingForConfirmation, // S-7
        Denied, // S-8
        Registred // Finaly
    }
}
