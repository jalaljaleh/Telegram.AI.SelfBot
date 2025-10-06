using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TL;

namespace Telegram.User.Net
{
    internal static class PathHelper
    {
        // Lazily initialize the base directory only once
        private static readonly Lazy<string> _baseDirectory = new Lazy<string>(() => AppContext.BaseDirectory);

        /// <summary>
        /// The directory where the application is running.
        /// </summary>
        public static string BaseDirectory => _baseDirectory.Value;


        /// <summary>
        /// Full path to the assets directory.
        /// </summary>
        public static string AssetsDirectoryPath =>  GetDirectoryPath("assets");

        /// <summary>
        /// Combines the base directory with a file name.
        /// </summary>
        /// <param name="fileName">Name of the file (with extension).</param>
        public static string GetFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Filename must be provided", nameof(fileName));

            return Path.Combine(BaseDirectory, fileName);
        }

        /// <summary>
        /// Combines the base directory with a sub-directory name.
        /// </summary>
        /// <param name="directoryName">Name of the sub-directory.</param>
        public static string GetDirectoryPath(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName))
                throw new ArgumentException("Directory name must be provided", nameof(directoryName));

            return Path.Combine(BaseDirectory, directoryName);
        }


        /// <summary>
        /// Full path to the configuration file (config.json).
        /// </summary>
        public static string ConfigFilePath => GetFilePath(AssetsDirectoryPath + "/config.json");
        /// <summary>
        /// Full path to the Proxy file (proxy.txt).
        /// </summary>
        public static string ProxyFilePath => GetFilePath(AssetsDirectoryPath + "/proxy.txt");
        /// <summary>
        /// Full path to the Prompt file (Prompt.txt).
        /// </summary>
        public static string PromptFilePath => GetFilePath(AssetsDirectoryPath + "/Prompt.txt");

    }
}
