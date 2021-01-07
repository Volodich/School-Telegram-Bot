using System;
using System.Collections.Generic;

namespace BalTelegramBot
{
    public partial class Pupils
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public string Class { get; set; }
        public int? ClassromTeacherId { get; set; }

        public UserInfo Chat { get; set; }
    }
}
