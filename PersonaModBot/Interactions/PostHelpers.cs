using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaModBot.Interactions
{
    public class PostHelpers : InteractionModuleBase
    {
        [SlashCommand("solved", "Mark the current post as solved")]
        public async Task Solved()
        {
            if (Context.Channel.GetChannelType() != ChannelType.PublicThread)
            {
                await RespondAsync("You can only solve posts in forums.", ephemeral: true);
                return;
            }

            SocketThreadChannel channel = (SocketThreadChannel)Context.Channel;
            var parent = channel.ParentChannel;
            if(parent.GetChannelType() != ChannelType.Forum)
            {
                await RespondAsync("You can only solve posts in forums.", ephemeral: true);
                return;
            }

            SocketForumChannel forum = (SocketForumChannel)parent;

            var solvedTag = forum.Tags.FirstOrDefault(tag => tag.Name == "Solved");

            var currentTags = channel.AppliedTags;

            if(currentTags.Contains(solvedTag.Id))
            {
                await RespondAsync("This post is already tagged as solved.", ephemeral: true);
                return;
            }

            await channel.ModifyAsync(x =>
            {
                    x.AppliedTags = Optional.Create(currentTags.Append(solvedTag.Id));
            });

            await RespondAsync("This issue has been solved! (This is hopefully true)");
        }
    }
}
