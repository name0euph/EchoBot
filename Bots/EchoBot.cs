// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        // HTTPクライアントを作成して初期化
        private static readonly HttpClient client = new HttpClient();


        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine(turnContext.Activity.Text);

                //リクエストボディを作成
                var json = $"{{ \"text\" : \"{turnContext.Activity.Text}\" }}";
                var content = new StringContent(json, Encoding.UTF8, @"application/json");

                //POSTリクエストを送信
                var response = await client.PostAsync(
                    "https://semantickernelapi20240215022752.azurewebsites.net/api/HttpExample",
                    content,
                    cancellationToken
                    );

                //エラーチェック
                response.EnsureSuccessStatusCode();

                //レスポンスを文字列として取得
                var replyText = await response.Content.ReadAsStringAsync();

                await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
