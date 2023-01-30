using EdgeDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaModBot.Models
{
    [EdgeDBType]
    public class ForumConfig
    {
        [EdgeDBProperty("forumId")]
        public ulong ForumId { get; set; }

        [EdgeDBProperty("solvedTag")]
        public ulong SolvedTag { get; set; }

        [EdgeDBProperty("solvedMessage")]
        public string SolvedMessage { get; set; }

        [EdgeDBProperty("allowedRoles")]
        public List<RoleConfig> AllowedRoles { get; set; }
    }
}
