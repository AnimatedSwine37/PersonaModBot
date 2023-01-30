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
        public Dictionary<ulong, (ulong forum, ulong solvedTag)[]> SetupForums { get; } = new();

        public Dictionary<ulong, RoleConfig[]> SetupRoles { get; } = new();
    }
}
