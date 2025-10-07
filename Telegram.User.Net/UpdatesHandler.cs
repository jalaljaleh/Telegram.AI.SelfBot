using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TL;
using WTelegram;

namespace Telegram.User.Net
{
    public class UpdatesHandler
    {
        private readonly IServiceProvider _services;
        private readonly Client _client;
        private readonly UpdateManager _manager;
        private readonly MessageHandler _messageHandler;
        public UpdatesHandler(IServiceProvider services)
        {
            _services = services;
            _client = services.GetRequiredService<Client>();
            _messageHandler = services.GetRequiredService<MessageHandler>();
            _manager = _client.WithUpdateManager(Client_OnUpdate);
        }
        public async Task InitializeAsync()
        {
            var dialogs = await _client.Messages_GetAllDialogs(); // dialogs = groups/channels/users
            dialogs.CollectUsersChats(_manager.Users, _manager.Chats);
        }
        private async Task Client_OnUpdate(Update update)
        {
            if (update is UpdateNewMessage newMessage)
            {
                if (_manager.UserOrChat(newMessage.message.Peer) is TL.User user)
                {
                    await _messageHandler.MessageReceivedAsync(newMessage.message, user);
                    return;
                }
                if (_manager.UserOrChat(newMessage.message.Peer) is TL.ChatBase chat)
                {
                    await _messageHandler.MessageReceivedAsync(newMessage, chat);
                    return;
                }
                Helpers.Log.Invoke(2, "Unknown message !");
            }

        }
    }
}
