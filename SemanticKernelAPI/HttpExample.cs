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


            // ���N�G�X�g��Body�����o��
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Json�f�[�^��SummarizeRequest�I�u�W�F�N�g�Ƀf�V���A���C�Y
            var data = JsonConvert.DeserializeObject<SummarizeRequest>(requestBody);

            // ./Prompts�f�B���N�g������v�����v�g�����[�f�B���O
            var prompts = _kernel.CreatePluginFromPromptDirectory("Prompts");

            // �v��̃v�����v�g�e���v���[�g���쐬
            var prompt = @"
            <message role=""system"">���Ȃ���AI�`���b�g�{�b�g�ł��B���[�U�̎���ɑ΂��ĉ񓚂��Ă��������B
            �񓚂�������Ȃ����̂ɂ́u������܂���v�Ɖ񓚂��Ă��������B</message>

            <message role=""user"">{{$input}}</message>
            ";

            try
            {
                // ���N�G�X�g�{�f�B���Ȃ��ꍇ�͗�O���X���[
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(req));
                }

                var result = await _kernel.InvokeAsync(
                    prompts["chat"],
                    new() { ["input"] = data.Text });

                Console.WriteLine(result);

                // �v�񌋉ʂ�Ԃ�
                return new OkObjectResult(result.GetValue<string>());
            }
            // ���N�G�X�g�{�f�B���Ȃ��ꍇ�̗�O����
            catch (ArgumentNullException e)
            {
                _logger.LogError(e, "���N�G�X�g�{�f�B������܂���B");
                return new OkObjectResult("���N�G�X�g�{�f�B������܂���B");
            }
        }
    }

        public class SummarizeRequest
    {
        public string Text { get; set; }
    }
}