using Discord;
using EdgeDB;
using PersonaModBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaModBot
{
    public class DbUtils
    {
        private readonly EdgeDBClient _db;

        public DbUtils(EdgeDBClient db)
        {
            _db = db;
        }

        public async Task<GuildConfig?> GetGuildConfig(ulong guildId)
        {
            var query = "select GuildConfig { guildId, forumConfigs: { forumId, " +
                "helperConfig: { solvedTag, solvedMessage, allowedRoles: { allowRename, allowSolve, allowTag, roleId}}, " +
                "tipConfig: { postTip, tipMessage}} } " +
                "filter .guildId = <int64>$guildId";
            await _db.EnsureConnectedAsync();
            var configRes = await _db.QueryAsync<GuildConfig>(query, new Dictionary<string, object?>() { { "guildId", (long)guildId } }, Capabilities.All);
            return configRes.FirstOrDefault();
        }
    }
}
