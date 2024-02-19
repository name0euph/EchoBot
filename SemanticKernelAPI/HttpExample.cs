using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace SemanticKernelAPI
{
    public class HttpExample
    {
        private readonly ILogger<HttpExample> _logger;
        private readonly Kernel _kernel;

        public HttpExample(Kernel kernel, ILogger<HttpExample> logger)
        {
            _logger = logger;
            _kernel = kernel;
        }

        [Function("HttpExample")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");


            // リクエストのBodyを取り出し
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // JsonデータをSummarizeRequestオブジェクトにデシリアライズ
            var data = JsonConvert.DeserializeObject<SummarizeRequest>(requestBody);

            // 要約のプロンプトテンプレートを作成
            var prompt = @"
            <message role=""system"">あなたはAIチャットボットです。ユーザの質問に対して回答してください。
            回答が分からないものには「分かりません」と回答してください。</message>

            <message role=""user"">{{$input}}</message>
            ";

            // 要約を実行する関数を作成
            var summarize = _kernel.CreateFunctionFromPrompt(
                promptTemplate: prompt,
                executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 1000 }
                );

            try
            {
                // リクエストボディがない場合は例外をスロー
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(req));
                }

                var result = await _kernel.InvokeAsync(summarize, new KernelArguments() { ["input"] = data.Text });

                Console.WriteLine(result);

                // 要約結果を返す
                return new OkObjectResult(result.GetValue<string>());
            }
            // リクエストボディがない場合の例外処理
            catch (ArgumentNullException e)
            {
                _logger.LogError(e, "リクエストボディがありません。");
                return new OkObjectResult("リクエストボディがありません。");
            }
        }
    }

        public class SummarizeRequest
    {
        public string Text { get; set; }
    }
}