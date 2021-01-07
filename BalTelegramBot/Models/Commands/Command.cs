using Microsoft.Extensions.Logging;
using Remotion.Linq.Clauses.ResultOperators;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BalTelegramBot.Models.Commands
{
    public abstract class Command
    {
        public abstract string Name { get; }

        public abstract Task<dynamic> Execute(Message message, TelegramBotClient client, UserInfo userInformation);

        public virtual bool Contains(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return message.Text.Contains(this.Name);
        }
    }
}
