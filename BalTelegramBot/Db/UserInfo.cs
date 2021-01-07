using System;
using System.Collections.Generic;

namespace BalTelegramBot
{
    public partial class UserInfo
    {
        public UserInfo()
        {
            Pupils = new HashSet<Pupils>();
            Teachers = new HashSet<Teachers>();
        }

        public long ChatId { get; set; }
        public string NameTelegram { get; set; }
        public string NameUser { get; set; }
        public string Phone { get; set; }
        public string TypeUser { get; set; }
        public string State { get; set; }
        public bool? CanSendMessageOther { get; set; }
        public bool? IsRegistred { get; set; }
        public bool? IsAdmin { get; set; }
        public bool? SchedulerNotification { get; set; }
        public string SettingNotification { get; set; }
        public ICollection<Pupils> Pupils { get; set; }
        public ICollection<Teachers> Teachers { get; set; }
    }

    public enum TypeUser
    {
        Guest,
        Pupil,
        Teacher
    }
}
