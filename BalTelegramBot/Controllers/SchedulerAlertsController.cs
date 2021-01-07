using BalTelegramBot.Models.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BalTelegramBot.Models;
using BalTelegramBot.Models.Commands.Scheduler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BalTelegramBot.Controllers
{
    [Route("api/message/alerts")]
    public class StartAlertsController : Controller
    {
        [HttpPost]
        public async Task<OkResult> Post([FromBody]JToken result)
        {
            var botClient = await Bot.GetBotClientAsync();
            var user = await BalDbController.GetUserInformationAsync(AppSettings.ChatIdCreator);

            //await new SchedulerAlertsController().Execute(
            //    new Message() {Text = new SchedulerAlertsController().StopAlerts}, botClient, user);
            await new SchedulerAlertsController().Execute(
                new Message() {Text = new SchedulerAlertsController().StartAlerts}, botClient, user);
            await Task.Delay(1080000); // 18 min
            SendMessageOtherAppTask().Wait();
            return Ok();
        }

        private async Task SendMessageOtherAppTask()
        {
            string request = "{\"key\": \"0000\"}";
            string Site = "https://dontsleepotherapp.azurewebsites.net:443/home/index";

            using (var httpClient = new HttpClient())
            {
                var httpContent = new StringContent(request, Encoding.UTF8, "application/json");

                // Do the actual request and await the response
                var httpResponse = await httpClient.PostAsync(Site, content: httpContent);
            }
        }
    }
    public class SchedulerAlertsController : Command
    {
        private static List<UserInfo> _users;
        private static Task alertsTask;
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private static readonly CancellationToken _token = CancellationTokenSource.Token;
        public static bool CommandStop = false;
        private static readonly int _ukrUtc = 3;
        protected static DateTime Time { get; set; }

        public override string Name => StartAlerts;
        public string StartAlerts => "/alerts start";
        public string StopAlerts => "/alerts stop";
        public override async Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation)
        {
            if (userInformation.IsAdmin == false)
                return true;

            if (message.Text == StartAlerts)
            {
                client.SendTextMessageAsync(chatId: userInformation.ChatId, text: "$сповіщення увімкнені.").Wait();
                CommandStop = false;
                if (alertsTask != null && alertsTask.IsCompleted == false) // If task is Run
                {
                    CancellationTokenSource.Cancel();
                }
                alertsTask = new Task(() => Start(client), _token);
                alertsTask.Start();
            }

            if (message.Text == StopAlerts)
            {
                await client.SendTextMessageAsync(chatId: userInformation.ChatId, text: "$сповіщення вимкненні.");
                CommandStop = true;
            }

            return true;
        }

        public override bool Contains(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return message.Text == Name || message.Text == StopAlerts;
        }

        public static async Task Start(TelegramBotClient client)
        {
            while(true)
            {
                Time = DateTime.Now;
                if((Time.Hour == (20-_ukrUtc) && Time.Minute ==  00) || 
                   (Time.Hour == (7-_ukrUtc)  && Time.Minute == 30))
                {
                    if (await CheckHolidayDayAsync() == true)
                        continue;
                    var allUsers = await BalDbController.GetUsersAsync();
                    _users = allUsers.Where<UserInfo>(ui => ui.SchedulerNotification == true).ToList();

                    await SendScheduler(client); // Send message to users
                }

                ThreadPool.GetAvailableThreads(out int workerThreads, out int _);
                ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int _);
                await client.SendTextMessageAsync(chatId: AppSettings.ChatIdCreator, text: $"Notification {DateTime.Now}. DateTime.Now.Hour: {DateTime.Now.Hour}; DateTime.Now.Minute: {DateTime.Now.Minute}; DateTime.UtcNow: {DateTime.UtcNow}; IsThreadPool: {Thread.CurrentThread.IsThreadPoolThread}; ThreadPool workers: {workerThreads} in max: {maxWorkerThreads}");
                Task.Delay(60000).Wait(); // 60 seconds
                /*
                 * 1. Каждую 1 минуту подлючаться к бд и проверять пользователей (может кто-то добавился). V
                 * 2. Проверять, есть ли у пользователей вкл. на уведомление.
                 * 3. Если есть, сравнивать, не нужно ли отправлять уведомление пользователю. 
                 * 4. Если уведомление отправлено, помечать это.
                 */
            }
        }

        public static async Task SendScheduler(TelegramBotClient client)
        {
            if (_users == null || _users.Count < 1 ||
                await GoogleSpreadsheetController.ConnectToSheetsAsync() == false)
            {
                return;
            }

            var localUsers = _users;
            string dayOfWeek = Time.DayOfWeek.ToString();
            string amPm = Time.ToString("tt", CultureInfo.InvariantCulture);

            if (Time.Hour == (20 - _ukrUtc) && Time.Minute == 00) // 20:00
            {
                localUsers = _users.Where<UserInfo>(ui => ui.SettingNotification == TimeSendNotification.EvMorning.ToString() ||
                                                            ui.SettingNotification == TimeSendNotification.Evning2000.ToString()).ToList();
            }
            if(Time.Hour == (7 - _ukrUtc) && Time.Minute == 30) // 07:30
            {
                localUsers = _users.Where(ui => ui.SettingNotification == TimeSendNotification.Morning0730.ToString()).ToList();
            }

            foreach (var @user in localUsers)
            {
                string message = default;
                if(user.TypeUser == TypeUser.Pupil.ToString()) // if Pupil
                {
                    message = await Scheduler.GenerateSchedulerMessage(scheduler: new Scheduler.Pupil(), 
                        userInformation: user, day: dayOfWeek);
                }
                if(user.TypeUser == TypeUser.Teacher.ToString()) // If Teacher
                {
                    message = await Scheduler.GenerateSchedulerMessage(scheduler: new Scheduler.Teacher(),
                        userInformation: user, day: dayOfWeek);
                }
                await client.SendTextMessageAsync(chatId: user.ChatId, text: message);
            }
        }

        /// <summary>
        /// Check the current day - is holiday?
        /// </summary>
        /// <returns>Return true - if current day is holiday, return false if current day is work day.</returns>
        public static async Task<bool> CheckHolidayDayAsync()
        {
            DateTime dateTimeNow = DateTime.Now;
            if (dateTimeNow.DayOfWeek == DayOfWeek.Saturday || dateTimeNow.DayOfWeek == DayOfWeek.Sunday)
                return true;
            if (dateTimeNow.Month == 6 || dateTimeNow.Month == 7) // if month == Summer month TODO: Add August
                return true;
            // work with data of google sheets
            {
                GoogleSpreadsheetController.Holidays gsHolidays = new GoogleSpreadsheetController.Holidays();
                var holidaysGs = await gsHolidays.GetSheetDataAsync().ContinueWith(_ => gsHolidays.GetHolidaysAsync().Result);
               
                if (holidaysGs.Count > 1)
                {
                    List<DateTime> holidays = new List<DateTime>();
                    for (int i = 1; i < holidaysGs.Count; i++)
                    {
                        if (DateTime.TryParse(holidaysGs[i][0].ToString(), out DateTime holidayDate) == true)
                        {
                            holidays.Add(holidayDate);
                        }
                    }

                    foreach (var holiday in holidays)
                    {
                        if (dateTimeNow.DayOfYear == holiday.DayOfYear)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public enum TimeSendNotification
        {
            Evning2000, // every day at 20:00
            Morning0730, // every day at 07:30
            EvMorning, // every day at 20:00 and 07:30
            Disabled // alerts OFF
        }
    }
}
