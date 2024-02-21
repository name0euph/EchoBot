using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;

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

            // ???N?G?X?g??Body?????o??
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // JsonデータをSummarizeRequestオブジェクトにデシリアライズ
            var data = JsonConvert.DeserializeObject<SummarizeRequest>(requestBody);

            // ./Prompts?f?B???N?g??????v?????v?g?????[?f?B???O
            var prompts = _kernel.CreatePluginFromPromptDirectory("../../../Prompts");

            try
            {
                // リクエストボディがない場合は例外をスロー
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(req));
                }

                // API???N?G?X?g???s???A??????擾
                var result = await _kernel.InvokeAsync(
                    prompts["chat"],
                    new() { ["input"] = data.Text });

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