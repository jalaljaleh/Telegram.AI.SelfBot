using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TL;
using WTelegram;

namespace Telegram.User.Net
{
    public class MessageHandler
    {
        readonly IServiceProvider _services;
        readonly Client _client;
        readonly AiChatService _ai;
        readonly ConfigurationService _configuration;
        public MessageHandler(IServiceProvider services)
        {
            _services = services;
            _client = services.GetRequiredService<Client>();
            _ai = services.GetRequiredService<AiChatService>();
            _configuration = services.GetRequiredService<ConfigurationService>();
        }
        public async Task MessageReceivedAsync(MessageBase MessageBase, TL.User user)
        {
            if (user.IsBot) return;
            if (MessageBase is not Message Message) return;
            if (Message.From is null || Message.From.ID == _client.UserId) return;

            // await _client.ReadHistory(user.ToInputPeer());

            // Pass message to aggregator. When it decides to flush, it calls the aiHandler.
            await MessageAggregator.HandleIncomingMessageAsync(
                user.ID,
                Message.message ?? string.Empty,
                async aggregatedText =>
                {
                    // existing AI call - only invoked once per aggregated block
                    bool aiResponse = await _ai.ReplyMessageWithAi(Message, user);
                    if (aiResponse) return true;

                    Helpers.Log.Invoke(2, "user message ignored !");
                    return false;
                });
        }


        public async Task MessageReceivedAsync(UpdateNewMessage Message, TL.ChatBase chat)
        {
            Helpers.Log.Invoke(2, "channel message ignored !");
        }
    }
}
