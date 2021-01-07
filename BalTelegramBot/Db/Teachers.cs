using System;
using System.Collections.Generic;

namespace BalTelegramBot
{
    public partial class Teachers
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public string Subjects { get; set; }
        public string Class { get; set; }
        public string FullName { get; set; }

        public UserInfo Chat { get; set; }
    }
}
