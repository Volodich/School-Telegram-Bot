using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BalTelegramBot.Controllers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace BalTelegramBot.Models.Commands.Scheduler
{
    public interface ISchedulerController
    {
        Dictionary<string, List<List<string>>> SchedulerDictionary { get; set; }
        Dictionary<string, List<List<string>>> FormatingDataFromGS(IList<IList<object>> scheduler);
        Task<Dictionary<string, List<List<string>>>> FormatingDataFromGSAsync(IList<IList<object>> scheduler);
        List<List<string>> GetConcreteWeekInformation(string key);
        Task<List<List<string>>> GetConcreteWeekInformationAsync(string key);
        List<string> GetConcreteDayInformation(List<List<string>> scheduler, string dayOfWeek);
        Task<List<string>> GetConcreteDayInformationAsync(List<List<string>> scheduler, string dayOfWeek);
    }
    internal class Scheduler
    {
        internal static string[] timeLessons = new string[] { "7:40-8:25", "8:30-9:15", "9:25-10:10", "10:25-11:10", "11:25-12:10", "12:30-13:05", "13:15-14:10", "14:20-15:05", "15:10-15:55" };
        internal static string[] smileNumber = new string[] { "0️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣" };

        private static readonly string[] weekDays = new string[] { "monday", "tuesday", "wednesday", "thursday", "friday" };

        public enum LanguageCases
        {
            Nazyvnyy,
            Rodovyy
        }

        public static Task<string> ConverteEngMonthToUkr(string engMonth, LanguageCases languageCases)
        {
            return Task.Run(() =>
            {
                switch (engMonth.ToLower())
                {
                    case "monday":
                        return "понеділок";
                    case "tuesday":
                        return "вівторок";
                    case "wednesday":
                        if (languageCases == LanguageCases.Rodovyy)
                        {
                            return "середу";
                        }
                        return "середа";
                    case "thursday":
                        return "четвер";
                    case "friday":
                        if (languageCases == LanguageCases.Rodovyy)
                        {
                            return "п'ятницю";
                        }
                        return "п'ятниця";
                    case "saturday":
                        if (languageCases == LanguageCases.Rodovyy)
                        {
                            return "суботу";
                        }
                        return "субота";
                    case "sunday":
                        if (languageCases == LanguageCases.Rodovyy)
                        {
                            return "неділю";
                        }
                        return "неділя";
                    default:
                        return "весь тиждень";
                }
            });
        }

        public static async Task<string> 
            GenerateSchedulerMessage(ISchedulerController scheduler, UserInfo userInformation, string day)
        { 
            GoogleSpreadsheetController.SchedulerSheet schedulerSheet = null;

            if (userInformation.TypeUser == TypeUser.Teacher.ToString())
            {
                schedulerSheet = new GoogleSpreadsheetController.SchedulerTeacher();
            }

            if (userInformation.TypeUser == TypeUser.Pupil.ToString()) // if Pupil
            {
                schedulerSheet = new GoogleSpreadsheetController.SchedulerPupil();
            }

            if (schedulerSheet == null)
                return null;

            await schedulerSheet.GetSheetDataAsync();
            var schedulerInformation = await schedulerSheet.GetScheduler();
            var formattingData = await scheduler.FormatingDataFromGSAsync(schedulerInformation);

            string answer = default;
            if (day == "AllWeek")
            {
                answer += $"Розклад на весь тиждень.\n";
                string body = default;
                foreach (var dayy in weekDays)
                {
                    body += await CreateDayScheduler(scheduler, userInformation, dayy);
                }
                return answer + body;
            }
            else
            {
                return await CreateDayScheduler(scheduler, userInformation, day);
            }
        }

        public static async Task<string> CreateDayScheduler(ISchedulerController scheduler, UserInfo userInformation, string day)
        {
            if (day.ToLowerInvariant() == DayOfWeek.Saturday.ToString().ToLowerInvariant() ||
                day.ToLowerInvariant() == DayOfWeek.Sunday.ToString().ToLowerInvariant())
            {
                return "Насолоджуйтесь вихідним 🌞";
            }
            string title = $"Розклад на {ConverteEngMonthToUkr(day, LanguageCases.Rodovyy).Result}:\n";
            string bodyAnswer = default;
            string answer = default;

            string subject = default;
            string classroom = default;
            string uniqueCommand = default;
            int lesson = default;


            if (userInformation.TypeUser == TypeUser.Teacher.ToString())
            {
                var teacherInfo = await BalDbController.GetTeacherInformationAsync(userInformation.ChatId);
                var schedulerDay = await scheduler.GetConcreteDayInformationAsync(
                    scheduler: await scheduler.GetConcreteWeekInformationAsync(teacherInfo.Teachers.First().FullName),
                    dayOfWeek: day);

                if (schedulerDay == null)
                {
                    return "Невдалось завантажити розклад. ";
                }
                for (int lessonPointer = 0; lessonPointer < schedulerDay.Count; lessonPointer += 3)
                {
                    string @class = default;
                    if (schedulerDay[lessonPointer] == String.Empty || schedulerDay[lessonPointer] == "-")
                    {
                        subject = "Вікно 🙂";
                        @class = String.Empty;
                        classroom = String.Empty;
                        uniqueCommand = String.Empty;
                    }
                    else
                    {
                        @class = $"Кл.: {schedulerDay[lessonPointer]}";
                        subject = $" {schedulerDay[lessonPointer + 1]}";
                        classroom = $" Каб.: {schedulerDay[lessonPointer + 2]}.";
                        uniqueCommand = $"\nДетальніше -->" + $" {new SchedulerUniqueCommand.Teacher().Name }{lesson}{day.ToLower()}_scheduler";
                    }
                    bodyAnswer += $"{smileNumber[lesson]} {@class}{subject}{classroom}{uniqueCommand}\n";
                    lesson++;
                }
                answer = title + bodyAnswer;
                return answer;
            }

            if (userInformation.TypeUser == TypeUser.Pupil.ToString())
            {
                var pupilInfo = await BalDbController.GetPupilInformationAsync(userInformation.ChatId);
                var schedulerDay = await scheduler.GetConcreteDayInformationAsync(
                    scheduler: await scheduler.GetConcreteWeekInformationAsync(pupilInfo.Pupils.First().Class),
                    dayOfWeek: day);

                if (schedulerDay == null)
                {
                    return "Невдалось завантажити розклад. ";
                }

                string teacher = default;

                for (int lessonPointer = 0; lessonPointer < schedulerDay.Count; lessonPointer += 3)
                {
                    if (schedulerDay[lessonPointer] == String.Empty)
                    {
                        subject = " Уроку немає 🙂";
                        teacher = String.Empty;
                        classroom = String.Empty;
                        uniqueCommand = String.Empty;
                    }
                    else
                    {
                        subject = $" {schedulerDay[lessonPointer]}";
                        teacher = $" Вч.: {schedulerDay[lessonPointer + 1]}";
                        classroom = $" Каб.: {schedulerDay[lessonPointer + 2]}.";
                        uniqueCommand = $"\nДетальніше -->" + $" /p{lesson}{day.ToLower()}_scheduler";
                    }
                    bodyAnswer += $"{smileNumber[lesson]} {subject}{classroom}{uniqueCommand}\n";
                    lesson++;
                }
                answer = title + bodyAnswer;
                return answer;
            }

            return "Невдалось завантажити розклад. ";
        }

        public static async Task<string[]> GenerateLessonMessage(UserInfo user, GoogleSpreadsheetController.SchedulerSheet schedulerSheet, ISchedulerController scheduler, string day, int lesson)
        {
            await schedulerSheet.GetSheetDataAsync();
            var formattingData =
                await scheduler.FormatingDataFromGSAsync(scheduler: await schedulerSheet.GetScheduler());

            string answer = " Невдалось знайти урок.";
            string callBackData = default; 
            if (user.TypeUser == TypeUser.Teacher.ToString())
            {
                var teacherInfo = await BalDbController.GetTeacherInformationAsync(user.ChatId);
                var schedulerDay = await scheduler.GetConcreteDayInformationAsync(
                    scheduler: await scheduler.GetConcreteWeekInformationAsync(teacherInfo.Teachers.First().FullName),
                    dayOfWeek: day);

                string title = $"{new SendPupilsMessage().Name}{lesson} урок у {ConverteEngMonthToUkr(day, LanguageCases.Rodovyy).Result}:\n";
                int pointLesson = lesson * 3;
                string @class = schedulerDay[pointLesson];
                callBackData = @class;
                string subject = schedulerDay[pointLesson + 1];
                string classRoom = schedulerDay[pointLesson + 2];
                string timeLesson = timeLessons[lesson];

                string body = $"🕐 Час: {timeLesson}\n" + $"📚 Предмет: {subject}\n" + $"💡 Клас: {@class}\n" + $"🏴 Кабінет: {classRoom}\n";
                answer = title + body;
            }

            if (user.TypeUser == TypeUser.Pupil.ToString())
            {
                var pupilInfo = await BalDbController.GetPupilInformationAsync(user.ChatId);
                var schedulerDay = await scheduler.GetConcreteDayInformationAsync(
                    scheduler: await scheduler.GetConcreteWeekInformationAsync(pupilInfo.Pupils.First().Class),
                    dayOfWeek: day);

                string title = $"{new SendTeacherMessage().Name}{lesson} урок у {ConverteEngMonthToUkr(day, LanguageCases.Rodovyy).Result}:\n";
                int pointLesson = lesson * 3;
                string subject = schedulerDay[pointLesson];
                string nameTeacher = schedulerDay[pointLesson + 1];
                callBackData = nameTeacher;
                string classRoom = schedulerDay[pointLesson + 2];
                string timeLesson = timeLessons[lesson];

                string body = $"🕐 Час: {timeLesson}\n" + $"📚 Предмет: {subject}\n" + $"💡 Вчитель: {nameTeacher}\n" + $"🏴 Кабінет: {classRoom}\n";
                answer = title + body;
            }

            return new[] {answer, callBackData};
        }
        public class Pupil : ISchedulerController
        {
            internal string NameTable => "Учні";

            public Dictionary<string, List<List<string>>> SchedulerDictionary { get; set; }

            public Dictionary<string, List<List<string>>> FormatingDataFromGS(IList<IList<object>> scheduler)
            {
                var data = (List<IList<object>>)scheduler;
                Dictionary<string, List<List<string>>> finalFormatting = new Dictionary<string, List<List<string>>>();
                int lessonsCount = 10; // lessons count (9) + title
                int classes = default;
                int lesson = default;

                int mondayPointer = 0; // pointer from start data in array
                int tuesdayPointer = 4;
                int wednesdayPointer = 7;
                int thursdayPointer = 10;
                int fridayPointer = 13;

                for (classes = 1; classes < data.Count; classes += lessonsCount)
                {
                    List<List<string>> @class = new List<List<string>>();
                    string nameClass = data[classes - 1][0].ToString(); // Add name pupil class

                    List<string> monday = new List<string>();
                    List<string> tuesday = new List<string>();
                    List<string> wednesday = new List<string>();
                    List<string> thursday = new List<string>();
                    List<string> friday = new List<string>();

                    for (lesson = 0; lesson < 9; lesson++) // 9 - lesson count (0 - 8)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            monday.Add(data[classes + lesson][mondayPointer + i].ToString());
                            tuesday.Add(data[classes + lesson][tuesdayPointer + i].ToString());
                            wednesday.Add(data[classes + lesson][wednesdayPointer + i].ToString());
                            thursday.Add(data[classes + lesson][thursdayPointer + i].ToString());
                            friday.Add(data[classes + lesson][fridayPointer + i].ToString());
                        }
                    }
                    @class.AddRange(new List<List<string>>() { monday, tuesday, wednesday, thursday, friday });
                    finalFormatting.Add(nameClass, @class);
                }
                SchedulerDictionary = finalFormatting;
                return finalFormatting;
            }

            public async Task<Dictionary<string, List<List<string>>>> FormatingDataFromGSAsync(IList<IList<object>> scheduler)
            {
                return await Task.Run(() => FormatingDataFromGS(scheduler));
            }

            public List<List<string>> GetConcreteWeekInformation(string @class)
            {
                SchedulerDictionary.TryGetValue(@class, out List<List<string>> concreteScheduler);
                return concreteScheduler;
            }

            public async Task<List<List<string>>> GetConcreteWeekInformationAsync(string @class)
            {
                return await Task.Run(() => GetConcreteWeekInformation(@class));
            }

            public List<string> GetConcreteDayInformation(List<List<string>> scheduler, string dayOfWeek)
            {
                if (scheduler == null)
                {
                    return null;
                }

                List<string> concreteScheduler = default;
                switch (dayOfWeek.ToLower())
                {
                    case "monday":
                        concreteScheduler = scheduler[0];
                        break;
                    case "tuesday":
                        concreteScheduler = scheduler[1];
                        break;
                    case "wednesday":
                        concreteScheduler = scheduler[2];
                        break;
                    case "thursday":
                        concreteScheduler = scheduler[3];
                        break;
                    case "friday":
                        concreteScheduler = scheduler[4];
                        break;
                    case "saturday":
                        return null;
                    case "sunday":
                        return null;
                    case "allweek":
                        concreteScheduler = new List<string>();
                        concreteScheduler.AddRange(scheduler[0]);
                        concreteScheduler.AddRange(scheduler[1]);
                        concreteScheduler.AddRange(scheduler[2]);
                        concreteScheduler.AddRange(scheduler[3]);
                        concreteScheduler.AddRange(scheduler[4]);
                        return concreteScheduler;
                }
                return concreteScheduler;
            }

            public async Task<List<string>> GetConcreteDayInformationAsync(List<List<string>> scheduler, string dayOfWeek)
            {
                return await Task.Run(() => GetConcreteDayInformation(scheduler, dayOfWeek));
            }
        }

        public class Teacher : ISchedulerController
        {
            internal string NameTable => "Вчителі";

            public Dictionary<string, List<List<string>>> SchedulerDictionary { get; set; }

            public Dictionary<string, List<List<string>>> FormatingDataFromGS(IList<IList<object>> scheduler)
            {
                var data = (List<IList<object>>)scheduler;
                Dictionary<string, List<List<string>>> finalFormatting = new Dictionary<string, List<List<string>>>();

                int lessonsCount = 10; // lessons count (9) + title
                int classes = default;
                int lesson = default;

                int mondayPointer = 1; // pointer from start data in array
                int tuesdayPointer = 4;
                int wednesdayPointer = 7;
                int thursdayPointer = 10;
                int fridayPointer = 13;

                for (classes = 1; classes < data.Count; classes += lessonsCount)
                {
                    List<List<string>> teacher = new List<List<string>>();
                    string nameTeacher = data[classes - 1][0].ToString(); // Add name pupil class

                    List<string> monday = new List<string>();
                    List<string> tuesday = new List<string>();
                    List<string> wednesday = new List<string>();
                    List<string> thursday = new List<string>();
                    List<string> friday = new List<string>();

                    for (lesson = 0; lesson < 9; lesson++) // 9 - lesson count (0 - 8)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            monday.Add(data[classes + lesson][mondayPointer + i].ToString());
                            tuesday.Add(data[classes + lesson][tuesdayPointer + i].ToString());
                            wednesday.Add(data[classes + lesson][wednesdayPointer + i].ToString());
                            thursday.Add(data[classes + lesson][thursdayPointer + i].ToString());
                            friday.Add(data[classes + lesson][fridayPointer + i].ToString());
                        }
                    }
                    teacher.AddRange(new List<List<string>>() { monday, tuesday, wednesday, thursday, friday });
                    finalFormatting.Add(nameTeacher, teacher);
                }
                SchedulerDictionary = finalFormatting;
                return finalFormatting;
            }

            public async Task<Dictionary<string, List<List<string>>>> FormatingDataFromGSAsync(IList<IList<object>> scheduler)
            {
                return await Task.Run(() => FormatingDataFromGS(scheduler));
            }

            public List<List<string>> GetConcreteWeekInformation(string nameTeacher)
            {
                SchedulerDictionary.TryGetValue(nameTeacher, out List<List<string>> concreteScheduler);
                return concreteScheduler;
            }

            public async Task<List<List<string>>> GetConcreteWeekInformationAsync(string nameTeacher)
            {
                return await Task.Run(() => GetConcreteWeekInformation(nameTeacher));
            }

            public List<string> GetConcreteDayInformation(List<List<string>> scheduler, string dayOfWeek)
            {
                if (scheduler == null)
                {
                    return null;
                }

                List<string> concreteScheduler = default;
                switch (dayOfWeek.ToLower())
                {
                    case "monday":
                        concreteScheduler = scheduler[0];
                        break;
                    case "tuesday":
                        concreteScheduler = scheduler[1];
                        break;
                    case "wednesday":
                        concreteScheduler = scheduler[2];
                        break;
                    case "thursday":
                        concreteScheduler = scheduler[3];
                        break;
                    case "friday":
                        concreteScheduler = scheduler[4];
                        break;
                    case "saturday":
                        return null;
                    case "sunday":
                        return null;
                    case "allweek":
                        concreteScheduler = new List<string>();
                        concreteScheduler.AddRange(scheduler[0]);
                        concreteScheduler.AddRange(scheduler[1]);
                        concreteScheduler.AddRange(scheduler[2]);
                        concreteScheduler.AddRange(scheduler[3]);
                        concreteScheduler.AddRange(scheduler[4]);
                        return concreteScheduler;
                }
                return concreteScheduler;
            }

            public async Task<List<string>> GetConcreteDayInformationAsync(List<List<string>> scheduler, string dayOfWeek)
            {
                return await Task.Run(() => GetConcreteDayInformation(scheduler, dayOfWeek));
            }
        }

    }
}
