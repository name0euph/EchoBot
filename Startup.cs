// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.BotBuilderSamples.Bots;
using Microsoft.Extensions.Hosting;
using EchoBot.Dialogs;
using EchoBot.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        private IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            services.AddSingleton<IStorage, MemoryStorage>();
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();
            services.AddSingleton<Dialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, EchoBot<Dialog>>();

            services.AddSingleton<ILogService>(provider =>
            {
                var cosmosDBOptions = _configuration.GetRequiredSection(nameof(CosmosDBOptions))
                .Get<CosmosDBOptions>()
                ?? throw new InvalidOperationException();

                var endpointUrl = cosmosDBOptions.Endpoint;
                var authKey = cosmosDBOptions.Key;
                var databaseId = cosmosDBOptions.DatabaseId;
                var messageContainerId = cosmosDBOptions.MessageContainerId;

                var cosmosClient = new CosmosClient(endpointUrl, authKey);
                var database = cosmosClient.GetDatabase(databaseId);
                var cosmosLogContainer = database.GetContainer(messageContainerId);

                return new LogService(cosmosLogContainer, provider.GetService<ILogger<LogService>>());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
