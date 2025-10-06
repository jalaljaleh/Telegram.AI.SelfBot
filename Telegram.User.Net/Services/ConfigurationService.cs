using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.User.Net
{
    internal class ConfigurationService
    {
        public ConfigurationService() { }
        public int api_id { get; set; }
        public string api_hash { get; set; }
        public string phone_number { get; set; }
        public string ip { get; set; }




        public string Prompt { get; set; }
        public string[] Proxy { get; set; }
        /// <summary>
        /// Loads the configuration based on the current environment (debug or production).
        /// </summary>
        public async static Task<ConfigurationService> LoadAsync()
        {
            var config = LoadFromFile(PathHelper.ConfigFilePath);
            config.Prompt =await LoadPromptAsync(PathHelper.PromptFilePath, Encoding.UTF8);
            config.Proxy = await LoadProxiesAsync(PathHelper.ProxyFilePath, Encoding.UTF8);

            return config;
        }

        // Load proxies: returns non-empty trimmed lines as string[]
        public static async Task<string[]> LoadProxiesAsync(string path, Encoding? enc = null)
        {
            enc ??= Encoding.UTF8;
            if (!File.Exists(path)) return Array.Empty<string>();
            var text = await File.ReadAllTextAsync(path, enc).ConfigureAwait(false);
            // split on any common newline, trim, ignore empty lines
            return text
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();
        }

        // Load prompt (single string). Returns empty if missing.
        public static async Task<string> LoadPromptAsync(string path, Encoding? enc = null)
        {
            enc ??= Encoding.UTF8;
            if (!File.Exists(path)) return string.Empty;
            return await File.ReadAllTextAsync(path, enc).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads configuration from a plain JSON file.
        /// </summary>
         static ConfigurationService LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Configuration file not found: {path}");

            var json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<ConfigurationService>(json);

            return config ?? throw new InvalidOperationException("Configuration deserialization failed.");
        }
    }
}
