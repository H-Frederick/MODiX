﻿using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Modix.Services.AutoCodePaste
{
    enum ReactionState
    {
        Added,
        Removed
    }

    public class CodePasteHandler
    {
        public CodePasteHandler(CodePasteService service)
        {
            _service = service;
        }

        private Dictionary<ulong, int> _repasteRatings = new Dictionary<ulong, int>();
        private CodePasteService _service;

        private async Task ModifyRatings(Cacheable<IUserMessage, ulong> cachedMessage, SocketReaction reaction, ReactionState state)
        {
            if (reaction.Emote.Name != "tldr")
            {
                return;
            }

            var message = await cachedMessage.GetOrDownloadAsync();

            if (message.Content.Length < 100)
            {
                return;
            }

            var roles = (reaction.User.GetValueOrDefault() as SocketGuildUser)?.Roles;

            if (roles == null)
            {
                return;
            }

            _repasteRatings.TryGetValue(message.Id, out int currentRating);

            int modifier = (state == ReactionState.Added ? 1 : -1);

            if (roles.Count > 1)
            {
                currentRating += 2 * modifier;
            }
            else
            {
                currentRating += 1 * modifier;
            }

            _repasteRatings[message.Id] = currentRating;

            if (currentRating >= 2)
            {
                await UploadMessage(message);
                _repasteRatings.Remove(message.Id);
            }
        }

        internal async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await ModifyRatings(cachedMessage, reaction, ReactionState.Added);
        }

        internal async Task ReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await ModifyRatings(cachedMessage, reaction, ReactionState.Removed);
        }

        private async Task UploadMessage(IUserMessage arg)
        {
            try
            {
                string url = await _service.UploadCode(arg);
                var embed = _service.BuildEmbed(arg.Author, arg.Content, url);

                await arg.Channel.SendMessageAsync(arg.Author.Mention, false, embed);
                await arg.DeleteAsync();
            }
            catch (WebException ex)
            {
                await arg.Channel.SendMessageAsync($"I would have reuploaded your long message, but: {ex.Message}");
            }
        }
    }
}
