using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;

namespace Telegram.User.Net
{
    public class Program()
    {
        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();
        public async Task MainAsync()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            WTelegram.Helpers.Log = (int lvl, string str) =>
            {
                if (lvl >= 2)
                    Console.WriteLine($" {DateTime.Now:HH:mm:ss} Telegram  {str}");
            };

            var config = await ConfigurationService.LoadAsync();

            IServiceProvider services = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton<WTelegram.Client>(x => new WTelegram.Client(config.api_id, config.api_hash))
                .AddSingleton<UpdatesHandler>()
                .AddSingleton<MessageHandler>()

                .AddSingleton<AiChatService>()
                .BuildServiceProvider();

            await StartAsync(services);
        }
        public async Task StartAsync(IServiceProvider services)
        {
            var config = services.GetRequiredService<ConfigurationService>();

            var client = services.GetRequiredService<WTelegram.Client>();

            var randomProxy = "";
            if (config.Proxy is not null)
            randomProxy = config.Proxy[Random.Shared.Next(config.Proxy.Count())];

            client.MTProxyUrl = randomProxy;

            await client.LoginAsync(config);

            // MessageHandler
            var updatesHandler = services.GetRequiredService<UpdatesHandler>();
            await updatesHandler.InitializeAsync();

            await Task.Delay(-1);
        }


    }
}