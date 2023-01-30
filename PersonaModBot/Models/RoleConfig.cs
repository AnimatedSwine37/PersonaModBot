using EdgeDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaModBot.Models
{
    [EdgeDBType]
    public class RoleConfig
    {
        [EdgeDBProperty("roleId")]
        public ulong RoleId { get; set; }

        [EdgeDBProperty("allowSolve")]
        public bool AllowSolve { get; set; }

        [EdgeDBProperty("allowTag")]
        public bool AllowTag { get; set; }

        [EdgeDBProperty("allowRename")]
        public bool AllowRename { get; set; }

        public RoleConfig(ulong roleId, bool allowSolve, bool allowTag, bool allowRename)
        {
            RoleId = roleId;
            AllowSolve = allowSolve;
            AllowTag = allowTag;
            AllowRename = allowRename;
        }
    }
}
