using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WTelegram;

namespace Telegram.User.Net
{
    internal static class ClientExtensions
    {
        public static async Task<WTelegram.Client> LoginAsync(this WTelegram.Client client, ConfigurationService config)
        {
            string loginInfo = config.phone_number;
            while (client.User == null)
                // returns which config is needed to continue login
                switch (loginInfo = await client.Login(loginInfo))
                {
                    case "verification_code": Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
                    case "name": loginInfo = "Halun"; break;    // if sign-up is required (first/last_name)
                    case "password": Console.Write("2FA secret: "); loginInfo = Console.ReadLine(); break; // if user has enabled 2FA


                    case "server_address": loginInfo = config.ip; break;// test DC

                    case "session_pathname": loginInfo = $"/{config.phone_number}.session"; break;

                    default: loginInfo = null; break;
                }
            Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");

            return client;
        }
    }
}
