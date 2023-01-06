using Discord.Interactions;
using EdgeDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaModBot.Interactions
{
    public class Setup : InteractionModuleBase
    {
        private readonly EdgeDBClient _db;

        public Setup(EdgeDBClient db)
        {
            _db = db;
        }

        [SlashCommand("setup", "Setup the bot for your server")]
        public async Task SetupCmd()
        {
            await RespondAsync("Pretend that we set stuff up here, I'll do it later.");
        }
    }
}
