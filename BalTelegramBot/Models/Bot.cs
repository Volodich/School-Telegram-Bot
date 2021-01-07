using System.Collections.Generic;
using System.Threading.Tasks;
using BalTelegramBot.Controllers;
using Telegram.Bot;
using BalTelegramBot.Models.Commands;
using BalTelegramBot.Models.Commands.Registration_State_Machine;
using BalTelegramBot.Models.Commands.Menu;
using BalTelegramBot.Models.Commands.Scheduler;

namespace BalTelegramBot.Models
{
    public class Bot
    {
        private static TelegramBotClient _botClient;
        private static List<Command> _commandsList;
        private static List<InlineKeyboardCommand> _inlineKeyboardcommandsList;

        public static IReadOnlyList<Command> Commands => _commandsList.AsReadOnly();
        public static IReadOnlyList<InlineKeyboardCommand> InlineKeyboardCommands => _inlineKeyboardcommandsList.AsReadOnly();

        public static async Task<TelegramBotClient> GetBotClientAsync()
        {
            if (_botClient != null)
            {
                return _botClient;
            }
            //HACK: Add more commands
            _commandsList = new List<Command>
            {
                new StartCommand(),
                new RegistrationReplyButtonCommand(),
                new RegistrationCommand(),
                new LoginGuestReplyButtonCommand(),
                new MainMenuCommand(),
                new MenuItemsCommand.Scheduler(),
                new MenuItemsCommand.Settings(),
                new SchedulerUniqueCommand.Pupil(),
                new SchedulerUniqueCommand.Teacher(),
                new SchedulerAlertsController(),
                new GiveSuperuser(),
                new MenuItemsCommand.Contacts(),
                new MenuItemsCommand.AboutSchool()
            };
            // Inline keyborad commands List
            _inlineKeyboardcommandsList = new List<InlineKeyboardCommand>
            {
                new ConcreteDayScheduler(),
                new ConfirmPupilRegistration(),
                new SendTeacherMessage(),
                new SendPupilsMessage(),
                new SettingCommand.Menu(),
                new SettingCommand.Alerts()
            };

            _botClient = new TelegramBotClient(AppSettings.Key);
            string hook = string.Format(AppSettings.Url, "api/message/update");
            await _botClient.SetWebhookAsync(hook);
            return _botClient;
        }

        /// <summary>
        /// Уровень доступа пользователя в боте
        /// </summary>
        public enum TypeUser 
        { 
            Admin = 0, 
            Teacher,
            Pupil,
            Guest,
            Director
        }
    }
}
