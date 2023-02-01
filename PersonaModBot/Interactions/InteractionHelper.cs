using PersonaModBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaModBot.Interactions
{
    public class InteractionHelper
    {
        public Dictionary<ulong, (ulong forum, ulong solvedTag)[]> SetupHelperForums { get; } = new();

        public Dictionary<ulong, RoleConfig[]> SetupHelperRoles { get; } = new();

        public Dictionary<ulong, ulong[]> SetupTips { get; } = new();
    }
}
