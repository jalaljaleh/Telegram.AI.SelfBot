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

            bool _aiResponse = await _ai.ReplyMessageWithAi(Message, user);
            if (!_aiResponse)
                Helpers.Log.Invoke(2, "user message ignored !");

                // await _client.Messages_SendReaction(user.ToInputPeer(), Message.id, new TL.Reaction[] { new TL.ReactionEmoji { emoticon = "👀" } }, true, true);
        }

       
        public async Task MessageReceivedAsync(UpdateNewMessage Message, TL.ChatBase chat)
        {
            Helpers.Log.Invoke(2, "channel message ignored !");
        }
    }
}
