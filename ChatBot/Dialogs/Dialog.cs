using EchoBot.Database;
using EchoBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoBot.Dialogs
{
    public class Dialog : ComponentDialog
    {
        // フィールド作成
        private static List<object> conversation = new List<object>();
        private static readonly HttpClient httpclient;
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;
        private string _conversationId;
        private string _aadObjectId;

        // コンストラクタ
        public Dialog(UserState userstate, IConfiguration configuration, ILogService logService)
            : base(nameof(Dialog))
        {
            _configuration = configuration;
            _logService = logService;

            var waterfallSteps = new WaterfallStep[]
            {
                SendAnswerAsync,
            };

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        // HttpClientオブジェクトを作成
        static Dialog()
        {
            httpclient = new HttpClient();
        }

        // ユーザ発言をCosmos DBに保存して、Semantic Kernel APIにPOSTリクエストする
        private async Task<DialogTurnResult> SendAnswerAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Teamsの場合、AadObjectIdを取得
            if (stepContext.Context.Activity.ChannelId == "msteams")
            {
                // ユーザ情報を取得
                var member = await TeamsInfo.GetMemberAsync(stepContext.Context, stepContext.Context.Activity.From.Id, cancellationToken);

                _aadObjectId = member.AadObjectId;
            }
            else
            {
                _aadObjectId = "Unknown";
            }

            // ユーザ発言を取得
            var userMessage = stepContext.Context.Activity.Text;

            var json = $"{{ \"text\" : \"{userMessage}\" }}";
            var content = new StringContent(json, Encoding.UTF8, @"application/json");

            // POSTリクエストを送信
            var response = await httpclient.PostAsync(
                _configuration["SemanticKernelAPIEndpoint"],
                content,
                cancellationToken
                );

            if (response.IsSuccessStatusCode)
            {
                //レスポンスを文字列として取得
                var replyText = await response.Content.ReadAsStringAsync();

                //レスポンスをBotの発言として送信
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(replyText), cancellationToken);

                // CosmosDB に保管するmessageの組み立て
                var message = new Message
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now,
                    AadObjectId = _aadObjectId,
                    ConversationId = _conversationId,
                    TextMessage = userMessage,
                    AIMessage = replyText,
                    IsUser = true
                };

                // Cosmos DBに保存
                await _logService.SaveConversationAsync(message);

            }
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
