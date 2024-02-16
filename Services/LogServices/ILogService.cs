using EchoBot.Database;
using System.Threading.Tasks;

namespace EchoBot.Services;

public interface ILogService
{
    Task SaveConversationAsync(Message message);
}
