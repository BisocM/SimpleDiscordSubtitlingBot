using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDiscordSubtitlingBot
{
    internal class BotUser
    {
        private static readonly Lazy<BotUser> lazy = new(() => new BotUser());

        public static BotUser Instance { get { return lazy.Value; } }

        private BotUser() { }

        public string DiscordBotToken { get; set; }
        public string DiscordClientId { get; set; }
        public string ServiceRegion { get; set; }
        public string MicrosoftCognitiveServicesKey { get; set; }
        public DiscordMember? SelectedUser { get; set; }
    }
}
