using BalTelegramBot.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BalTelegramBot.Models.Commands
{
    public class SendMessagesCommand 
    {

        public async Task Execute(Message message, TelegramBotClient client, UserInfo user)
        {
            if(message is null || client is null || user is null)
            {
                return;
            }
            string state = user.State.Split("_").First();
            string data = user.State.Split("_").Last();

            if(state == SendMessageState.FromPupilToClassmates.ToString()) // From pupil to classmates
            {
                user = await BalDbController.GetPupilInformationAsync(user.ChatId);
                var classmates = await BalDbController.GetPupilsAsync(user.Pupils.First().Class);

                string userMessage = $"Повідомлення від _{user.NameUser}_:\n*{message.Text}*";
                if (classmates != null)
                {
                    foreach (var pupil in classmates)
                    {
                        if (pupil.ChatId == user.ChatId)
                        {
                            await client.SendTextMessageAsync(chatId: user.ChatId, text: "Повідомлення однокласникам надіслано.");
                            continue;
                        }
                        await client.SendTextMessageAsync(pupil.ChatId, text: userMessage, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                } 
            }
            if(state == SendMessageState.FromTeacherToPupils.ToString()) // From teacher to pupils
            {
                var pupils = await BalDbController.GetPupilsAsync(data);
                string teacherMessage = $"Повідомлення від вчителя _{user.NameUser}_:\n*{message.Text}*";

                foreach(var pupil in pupils)
                {
                    await client.SendTextMessageAsync(chatId: pupil.ChatId,
                                                        text: teacherMessage,
                                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }

                await client.SendTextMessageAsync(chatId: user.ChatId,
                                                    text: $"Ваше повідомлення надіслано _{pupils.Count}_ учням(ю) *{data}* класу.",
                                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                
            }

            if (state == SendMessageState.FromPupilToTeacher.ToString())
            {
                var teacher = await BalDbController.GetTeacherInformationAsync(data);
                user = await BalDbController.GetPupilInformationAsync(user.ChatId);
                string pupilMessage = $"Повідомлення від учня {user.Pupils.First().Class} класу [{user.NameUser}](tg://user?id={user.ChatId}): *{message.Text}*";

                await client.SendTextMessageAsync(chatId: teacher.ChatId, text: pupilMessage,
                    parseMode: ParseMode.Markdown);
                await client.SendTextMessageAsync(chatId: user.ChatId, text: $"Ваше повідомлення надіслано вчителю _{teacher.NameUser}_", parseMode: ParseMode.Markdown);
            }
            await BalDbController.ChangeUserStateAsync(SendMessageState.Sended.ToString(), user.ChatId);
        }
        /*  class PupilToClassmates
        public class PupilToClassmates : Command
        {
            public override string Name => "Написати однокласникам 👨‍👧‍👧";

            public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
            {
                await client.SendTextMessageAsync(userInformation.ChatId, "Відправте повідомлення, яке хочете надіслати: ");

                userInformation.State = SendMessageState.Sending.ToString();
                await BalDbController.ChangeUserStateAsync(userInformation.State, userInformation.ChatId);

                return true;
            }

            public async Task SendMessage(Message message, TelegramBotClient client, UserInfo userInformation)
            {
                if(message.Text == null)
                {
                    return;
                }

                userInformation = await BalDbController.GetPupilInformation(userInformation.ChatId);
                var classmates = await BalDbController.GetClassmates(userInformation.Pupils.First().Class);

                string userMessage = $"Повідомлення від _{userInformation.NameUser}_:\n*{message.Text}*";
                if (classmates != null)
                {
                    foreach (var pupil in classmates)
                    {
                        if(pupil.ChatId == userInformation.ChatId)
                        {
                            await client.SendTextMessageAsync(chatId: userInformation.ChatId, text: "Повідомлення однокласникам надіслано.");
                            continue; 
                        }
                        await client.SendTextMessageAsync(pupil.ChatId, text: userMessage, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                }

                await BalDbController.ChangeUserStateAsync(SendMessageState.Sended.ToString(), userInformation.ChatId);
            }
        }
        */

        public enum SendMessageState
        {
            Sending,
            FromPupilToTeacher,
            FromTeacherToPupils,
            FromPupilToClassmates,
            Sended
        }
    }
}
