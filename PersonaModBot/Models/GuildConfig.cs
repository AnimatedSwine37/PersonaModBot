using EdgeDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaModBot.Models
{
    [EdgeDBType]
    public class GuildConfig
    {
        [EdgeDBProperty("guildId")]
        public ulong GuildId { get; set; }

        [EdgeDBProperty("forumConfigs")]
        public List<ForumConfig> ForumConfigs { get; set; }
    }
}
