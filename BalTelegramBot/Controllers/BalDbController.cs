using BalTelegramBot.Models.Commands.Registration_State_Machine;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BalTelegramBot.Controllers
{
    public class BalDbController
    {
        public static async Task CreateUserInDbAsync(UserInfo user) // Create user data in db
        {
            using (var db = new BalDbContext())
            {
                await db.UserInfo.AddAsync(new UserInfo()
                {
                    ChatId = user.ChatId,
                    NameTelegram = user.NameTelegram,
                    NameUser = default,
                    Phone = default,
                    TypeUser = default,
                    State = RegistrationStateMachine.None.ToString(),
                    CanSendMessageOther = true,
                    IsRegistred = false, // default data
                    SchedulerNotification = false, // default data
                    SettingNotification = SchedulerAlertsController.TimeSendNotification.Disabled.ToString(), // default
                    IsAdmin = false
                });

                await db.SaveChangesAsync();
            }
        }

        public static async Task<UserInfo> GetUserInformationAsync(long chatId) 
        {
            using(var db = new BalDbContext())
            {
                try
                {
                    return await Task.Run(() => db.UserInfo.Single(ui => ui.ChatId == chatId));
                }
                catch(InvalidOperationException) // If data not fount
                {
                    return null; 
                }
            }
        }

        public static async Task<List<UserInfo>> GetUsersAsync()
        {
            using(var db = new BalDbContext())
            {
                var result = from ui in db.UserInfo
                             select ui;
                return await result.ToListAsync();
            }
        }

        public static async Task UpdateUserDataAsync(UserInfo User) // Update in db type user except pupil 
        {
            try
            {
                using (var db = new BalDbContext())
                {
                    var user = await db.UserInfo.Where(ui => ui.ChatId == User.ChatId).SingleAsync();
                    user.NameUser = User.NameUser;
                    user.NameTelegram = User.NameTelegram;
                    user.Phone = User.Phone;
                    user.TypeUser = User.TypeUser;
                    user.IsRegistred = User.IsRegistred;
                    user.SchedulerNotification = User.SchedulerNotification;
                    user.SettingNotification = User.SettingNotification;
                    if (user.TypeUser != null)
                    {
                        if (user.TypeUser == TypeUser.Pupil.ToString() && User.Pupils.Count > 0
                        ) //  User.Pupils.Count is not empty
                        {
                            var pupil = await db.Pupils.Where(p => p.ChatId == user.ChatId).SingleAsync();
                            pupil.Class = User.Pupils.First().Class;
                            pupil.ClassromTeacherId = User.Pupils.First().ClassromTeacherId;
                        }

                        if (user.TypeUser == TypeUser.Teacher.ToString() && User.Teachers.Count > 0)
                        {
                            var teacher = await db.Teachers.Where(t => t.ChatId == user.ChatId).SingleAsync();
                            teacher.Class = User.Teachers.First().Class;
                            teacher.FullName = User.Teachers.First().FullName;
                            teacher.Subjects = User.Teachers.First().Subjects;
                        }
                    }

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                ;
            }
        }

        public static async Task ChangeUserStateAsync(string stateMachine, long chatId) // change in db user state 
        {
            using (var context = new BalDbContext())
            {
                var user = await context.UserInfo.Where(ui => ui.ChatId == chatId).SingleAsync();
                user.State = stateMachine;
                await context.SaveChangesAsync();
            }
        }

        public static async Task<UserInfo> GetTeacherInformationAsync(long chatId)
        {
            using (var db = new BalDbContext())
            {
                var user = await db.UserInfo.Where(ui => ui.ChatId == chatId).SingleAsync();
                user.Teachers.Add(await db.Teachers.Where(t => t.ChatId == user.ChatId).SingleAsync());
                return user;
            }
        }

        public static async Task<UserInfo> GetTeacherInformationAsync(string name)
        {
            using(var db = new BalDbContext())
            {
                try
                {
                    return await db.UserInfo.Where(ui => ui.NameUser == name).SingleAsync();
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }

        public static async Task<UserInfo> GetPupilInformationAsync(long chatId)
        {
            using (var db = new BalDbContext())
            {
                var user = await db.UserInfo.Where(ui => ui.ChatId == chatId).SingleAsync();
                user.Pupils.Add(await db.Pupils.Where(p => p.ChatId == user.ChatId).SingleAsync());
                return user;
            }
        }

        public static async Task<List<Pupils>> GetPupilsAsync(string @class)
        {
            using (var db = new BalDbContext())
            {
                var pupils = await db.Pupils.Where(p => p.Class == @class).ToListAsync();
                return pupils;
            }
        }

        public static async Task<List<UserInfo>> GetTeachersAsync()
        {
            using (var db = new BalDbContext())
            {
                var teachers = await db.UserInfo.Where(ui => ui.TypeUser == TypeUser.Teacher.ToString()).ToListAsync();
                return teachers;
            }
        }

        public static async Task<long> GetClassmateTeacherAsync(string @class)
        {
            return await Task.Run(() => GetClassmateTeacher(@class));
        }

        public static long GetClassmateTeacher(string @class)
        {
            try
            {
                using (var db = new BalDbContext())
                {
                    var r = from user in db.UserInfo
                        join teacher in db.Teachers on user.ChatId equals teacher.ChatId
                        where teacher.Class == @class
                        select new {user.ChatId};
                    return r.First().ChatId;
                }
            }
            catch (InvalidOperationException)
            {
                return default;
            }
        }

        public static async Task<bool> AddPupilAsync(long chatId)
        {
            try
            {
                using (var db = new BalDbContext())
                {
                    var user = await db.UserInfo.Where<UserInfo>(ui => ui.ChatId == chatId).SingleAsync();
                    if (db.Pupils.Contains<Pupils>(new Pupils() { ChatId = chatId }) == true)
                        return false;
                    db.Pupils.Add(new Pupils()
                    {
                        Chat = user,
                        Class = default,
                        ClassromTeacherId = default
                    });
                    await db.SaveChangesAsync();
                }
            }
            catch(Exception)
            {
                return false;
            }
            return true;
        }

        public static async Task<bool> AddTeacherAsync(long chatId)
        {
            try
            {
                using (var db = new BalDbContext())
                {
                    var user = await db.UserInfo.Where<UserInfo>(ui => ui.ChatId == chatId).SingleAsync();
                    if (db.Teachers.Contains<Teachers>(new Teachers() { ChatId = chatId }) == true)
                        return false;
                    db.Teachers.Add(new Teachers()
                    {
                        //Id = (int)chatId-1,
                        ChatId = chatId,
                        Class = default,
                        Subjects = default,
                        Chat = user
                    });
                    await db.SaveChangesAsync();
                }
            }catch(Exception ex)
            {
                return false;
            }
            return true;
        }
    }
}
