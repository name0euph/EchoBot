using EchoBot.Database;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EchoBot.Services;

public class LogService : ILogService
{
    // CosmosDBのコンテナーを保持するプライベートフィールド
    private readonly Container _cosmosLogContainer;
    private readonly ILogger<LogService> _logger;

    // コンストラクタ
    public LogService(Container cosmosLogContainer, ILogger<LogService> logger)
    {
        _logger = logger;
        _cosmosLogContainer = cosmosLogContainer;
    }

    // メッセージをCosmosDBに書き込む
    public async Task SaveConversationAsync(Message message)
    {
        var document = new
        {
            id = Guid.NewGuid().ToString(),
            MessageId = message.MessageId,
            Timestamp = message.Timestamp,
            AadObjectId = message.AadObjectId,
            ConversationId = message.ConversationId,
            TextMessage = message.TextMessage,
            AIMessage = message.AIMessage,
            IsUser = message.IsUser
        };

        // CosmosDBに書き込み
        await _cosmosLogContainer.CreateItemAsync(document);
    }

}
