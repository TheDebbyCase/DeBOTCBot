using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
namespace DeBOTCBot
{
    public static class Extensions
    {
        public static async Task<bool> RespondRegistered(this CommandContext context, ServerInfo info, string content)
        {
            return info.RegisterMessage(await context.EditResponseAsync(content));
        }
        public static async Task<bool> FollowupRegistered(this SlashCommandContext context, ServerInfo info, string content, bool ephemeral = false)
        {
            return info.RegisterMessage(await context.FollowupAsync(content, ephemeral));
        }
    }
}