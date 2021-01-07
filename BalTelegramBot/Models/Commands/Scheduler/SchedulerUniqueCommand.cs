using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BalTelegramBot.Controllers;
using BalTelegramBot.Models.Commands.Menu;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BalTelegramBot.Models.Commands.Scheduler
{
    public class SchedulerUniqueCommand
    {
        private static string TeacherCommand => "/t";
        private static string PupilCommand => "/p";
        private static string PupilMenuItemCommandMessageToTeacher => "Написати вчителю 💼";
        private static string TeacherMenuItemCommandMessageToPupils => "Написати учням 👨‍👧‍👧";
        private static string MainMenuItemCommand => new MainMenuCommand().MenuName;

        public static string CreateCommand(UserInfo userInformation, int lesson, string dayOfWeek)
        {
            string command = "/";
            if (userInformation.TypeUser == TypeUser.Pupil.ToString())
            {
                command += "p";
            }
            else if (userInformation.TypeUser == TypeUser.Teacher.ToString())
            {
                command += "t";
            }
            return command + lesson.ToString() + dayOfWeek;
        }

        public static void DecryptionCommand(string command, out TypeUser typeUser, out int lesson, out string dayOfWeek)
        {
            lesson = Convert.ToInt32(command.Substring(2, 1));
            dayOfWeek = command.Split("_").First().Substring(3);
            string user = command.Substring(1, 1);
            if (user == "p")
            {
                typeUser = TypeUser.Pupil;
            }
            else if (user == "t")
            {
                typeUser = TypeUser.Teacher;
            }
            else typeUser = TypeUser.Pupil; // TODO: this
        }

        public class Pupil : Command
        {
            public override string Name => PupilCommand;

            public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
            {
                string command = message.Text;
                DecryptionCommand(command, out _, out int lesson, out string dayOfWeek);

                string[] answer = await Commands.Scheduler.Scheduler.GenerateLessonMessage(
                    user: userInformation,
                    schedulerSheet: new GoogleSpreadsheetController.SchedulerPupil(), 
                    scheduler: new Commands.Scheduler.Scheduler.Pupil(), 
                    day: dayOfWeek,
                    lesson: lesson);
                if (answer.Last() == string.Empty)
                {
                    answer[1] = "Немає даних";
                }
                List<List<InlineKeyboardButton>> answerKeyboard = new List<List<InlineKeyboardButton>>
                {
                    new List<InlineKeyboardButton>()
                    {
                        new InlineKeyboardButton()
                        {
                            Text = PupilMenuItemCommandMessageToTeacher, CallbackData = answer.Last()
                        }
                    }
                };
                
                await client.SendTextMessageAsync(chatId: userInformation.ChatId,
                                                   text: answer.First(), 
                                                   replyMarkup: new InlineKeyboardMarkup(answerKeyboard));

                return true;
            }

            public override bool Contains(Message message)
            {
                if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                    return false;
                if (Regex.IsMatch(message.Text, Name))
                    return true;
                return false;
            }
        }

        public class Teacher : Command
        {
            public override string Name => TeacherCommand;

            public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
            {
                string command = message.Text;
                DecryptionCommand(command, out _, out int lesson, out string dayOfWeek);

                string[] answer = await Commands.Scheduler.Scheduler.GenerateLessonMessage(
                    user: userInformation,
                    schedulerSheet: new GoogleSpreadsheetController.SchedulerTeacher(),
                    scheduler: new Commands.Scheduler.Scheduler.Teacher(), 
                    day: dayOfWeek, 
                    lesson: lesson);

                if (answer.Last() == string.Empty)
                {
                    answer[1] = "Немає даних";
                }

                List<List<InlineKeyboardButton>> answerKeyboard = new List<List<InlineKeyboardButton>>
                {
                    new List<InlineKeyboardButton>()
                    {
                        new InlineKeyboardButton()
                        {
                            Text = TeacherMenuItemCommandMessageToPupils, CallbackData = answer.Last()
                        }
                    }
                };

                await client.SendTextMessageAsync(chatId: userInformation.ChatId,
                                                   text: answer.First(), 
                                                   replyMarkup: new InlineKeyboardMarkup(answerKeyboard));

                return true;
            }

            public override bool Contains(Message message)
            {
                if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                    return false;
                if (Regex.IsMatch(message.Text, Name))
                    return true;
                return false;
            }
        }


    }
}
