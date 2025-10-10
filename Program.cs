using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using System.ComponentModel;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Exceptions;
using System.Diagnostics;
using DSharpPlus.Commands.Trees;
using System.Reflection;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
namespace DeBOTCBot
{
    public enum LogType
    {
        Info,
        Surpressed,
        Error
    }
    public class ServerSaveInfo
    {
        public ServerSaveInfo() { }
        public ServerSaveInfo(ServerInfo info)
        {
            botcScripts = info.botcGame.scripts;
        }
        public ulong storytellerRoleID = default;
        public ulong genericPlayerRoleID = default;
        public ulong announcementsChannelID = default;
        public ulong storytellerChannelID = default;
        public ulong townChannelID = default;
        public ulong homesCategoryID = default;
        public Dictionary<string, string[]> botcScripts = default;
        public Dictionary<string, int> botcChannels = default;
        public List<string> botcHomes = default;
    }
    public class ServerInfo(DiscordGuild guild)
    {
        public readonly static Type[] surpressedExceptions = [typeof(NotFoundException), typeof(OperationCanceledException), typeof(TaskCanceledException)];
        public Dictionary<ulong, DiscordMessage> messages = [];
        public Dictionary<ulong, DiscordChannel> channels = [];
        public Dictionary<ulong, DiscordRole> roles = [];
        public Dictionary<ulong, DiscordMember> members = [];
        public bool hasInfo = false;
        public DiscordGuild server = guild;
        public BOTCCharacters botcGame = new();
        public async Task Initialize()
        {
            await Destroy(true);
            await DeBOTCBot.SaveServerInfo(DeBOTCBot.activeServers[server.Id]);
            hasInfo = true;
        }
        public async Task FillSavedValues(ServerSaveInfo savedInfo)
        {
            await Task.Run(() =>
            {
                botcGame.scripts = savedInfo.botcScripts;
                botcGame.Initialize(this);
                hasInfo = true;
            });
        }
        public async Task Destroy(bool refresh = false)
        {
            if (!refresh)
            {
                await DeBOTCBot.SaveServerInfo(this);
            }
            hasInfo = false;
        }
        public static async Task BotLog(string contents, ConsoleColor overrideColour = ConsoleColor.Gray, LogType type = LogType.Info)
        {
            if (type != LogType.Surpressed)
            {
                string[] splitLines = contents.Split("\n");
                for (int i = 0; i < splitLines.Length; i++)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write($"Bot Log: ");
                    Console.ForegroundColor = overrideColour;
                    Console.Write($"{splitLines[i]}\n");
                }
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            await Serialization.WriteLog("Bot Log", contents, type);
        }
        public static async Task BotLog(Exception exception)
        {
            Type exceptionType = exception.GetType();
            if (surpressedExceptions.Contains(exceptionType))
            {
                string exceptionName = exceptionType.ToString().Split('.').Last();
                await BotLog($"Surpressed {exceptionName}", type: LogType.Surpressed);
                return;
            }
            await BotLog($"{exception.GetType()}, {exception.Message}\n{exception.StackTrace}", ConsoleColor.Red, LogType.Error);
        }
        public async Task Log(string contents, ConsoleColor overrideColour = ConsoleColor.Gray, LogType type = LogType.Info)
        {
            if (type != LogType.Surpressed)
            {
                string serverString = $"{server.Name}: ";
                string[] splitLines = contents.Split("\n");
                for (int i = 0; i < splitLines.Length; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write(serverString);
                    Console.ForegroundColor = overrideColour;
                    Console.Write($"{splitLines[i]}\n");
                }
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            await Serialization.WriteLog(server.Name, contents, type);
        }
        public async Task Log(Exception exception)
        {
            Type exceptionType = exception.GetType();
            if (surpressedExceptions.Contains(exceptionType))
            {
                string exceptionName = exceptionType.ToString().Split('.').Last();
                await Log($"Surpressed {exceptionName}", type: LogType.Surpressed);
                return;
            }
            await Log($"{exception.GetType()}, {exception.Message}\n{exception.StackTrace}", ConsoleColor.Red, LogType.Error);
        }
        public async Task<ulong> NewMessage(DiscordChannel channel, DiscordMessageBuilder builder)
        {
            ulong id = 0;
            try
            {
                DiscordMessage message = await channel.SendMessageAsync(builder);
                id = message.Id;
                messages.Add(id, message);
            }
            catch (Exception exception)
            {
                await Log(exception);
            }
            return id;
        }
        public async Task<ulong> NewMessage(DiscordChannel channel, string content)
        {
            return await NewMessage(channel, new DiscordMessageBuilder().WithContent(content));
        }
        public async Task<ulong> NewMessage(ulong channelID, string content)
        {
            return await NewMessage(channelID, new DiscordMessageBuilder().WithContent(content));
        }
        public async Task<ulong> NewMessage(ulong channelID, DiscordMessageBuilder builder)
        {
            if (!channels.TryGetValue(channelID, out DiscordChannel channel))
            {
                return 0;
            }
            return await NewMessage(channel, builder);
        }
        public bool RegisterMessage(DiscordMessage message)
        {
            return messages.TryAdd(message.Id, message);
        }
        public async Task<bool> RegisterMessage(ulong id)
        {
            return RegisterMessage(await GetMessage(id));
        }
        public async Task<bool> DeleteMessage(ulong id)
        {
            if (!messages.TryGetValue(id, out DiscordMessage message))
            {
                return false;
            }
            bool success = true;
            try
            {
                await message.DeleteAsync();
            }
            catch (Exception exception)
            {
                await Log(exception);
                success = false;
            }
            finally
            {
                messages.Remove(id);
            }
            return success;
        }
        public async Task<bool> DeleteMessage(DiscordMessage message)
        {
            return await DeleteMessage(message.Id);
        }
        public async Task<ulong> NewChannel(string name, DiscordChannelType type, DiscordChannel parent = null, Optional<string> topic = default, int? userLimit = null, IEnumerable<DiscordOverwriteBuilder> overwrites = null)
        {
            ulong id = 0;
            try
            {
                DiscordChannel channel = await server.CreateChannelAsync(name, type, parent, topic, userLimit: userLimit, overwrites: overwrites);
                id = channel.Id;
                channels.Add(id, channel);
            }
            catch (Exception exception)
            {
                await Log(exception);
            }
            return id;
        }
        public async Task<ulong> NewChannel(string name, DiscordChannelType type, ulong parentID, Optional<string> topic = default, int? userLimit = null, IEnumerable<DiscordOverwriteBuilder> overwrites = null)
        {
            if (!channels.TryGetValue(parentID, out DiscordChannel parent))
            {
                return 0;
            }
            return await NewChannel(name, type, parent, topic, userLimit, overwrites);
        }
        public async Task<bool> DeleteChannel(ulong id)
        {
            if (!channels.TryGetValue(id, out DiscordChannel channel))
            {
                return false;
            }
            bool success = true;
            try
            {
                await channel.DeleteAsync();
            }
            catch (Exception exception)
            {
                await Log(exception);
                success = false;
            }
            finally
            {
                channels.Remove(id);
            }
            return success;
        }
        public async Task<bool> DeleteChannel(DiscordChannel channel)
        {
            return await DeleteChannel(channel.Id);
        }
        public async Task<ulong> NewRole(string name, DiscordPermissions? permissions = null, DiscordColor? color = null, bool hoist = false, bool mentionable = false)
        {
            ulong id = 0;
            try
            {
                DiscordRole role = await server.CreateRoleAsync(name, permissions, color, hoist, mentionable);
                id = role.Id;
                roles.Add(id, role);
            }
            catch (Exception exception)
            {
                await Log(exception);
            }
            return id;
        }
        public async Task<bool> DeleteRole(ulong id)
        {
            if (!roles.TryGetValue(id, out DiscordRole role))
            {
                return false;
            }
            bool success = true;
            try
            {
                await role.DeleteAsync();
            }
            catch (Exception exception)
            {
                await Log(exception);
                success = false;
            }
            finally
            {
                roles.Remove(id);
            }
            return success;
        }
        public async Task<bool> DeleteRole(DiscordRole role)
        {
            return await DeleteRole(role.Id);
        }
        public bool RegisterChannel(DiscordChannel channel)
        {
            return channels.TryAdd(channel.Id, channel);
        }
        public async Task<bool> RegisterChannel(ulong id)
        {
            return RegisterChannel(await server.GetChannelAsync(id));
        }
        public bool RegisterRole(DiscordRole role)
        {
            return roles.TryAdd(role.Id, role);
        }
        public async Task<bool> RegisterRole(ulong id)
        {
            return RegisterRole(await server.GetRoleAsync(id));
        }
        public ulong RegisterMember(DiscordMember member)
        {
            ulong id = member.Id;
            members.Add(id, member);
            return id;
        }
        public async Task<ulong> RegisterMember(ulong id)
        {
            return RegisterMember(await server.GetMemberAsync(id));
        }
        public bool UnregisterMember(DiscordMember member)
        {
            return members.Remove(member.Id);
        }
        public async Task<bool> UnregisterMember(ulong id)
        {
            return UnregisterMember(await server.GetMemberAsync(id));
        }
        public async Task<DiscordMessage> GetMessage(ulong id, DiscordChannel channel)
        {
            DiscordMessage message = null;
            try
            {
                message = await channel.GetMessageAsync(id);
                messages.TryAdd(id, message);
            }
            catch (Exception exception)
            {
                await Log(exception);
                messages.Remove(id);
            }
            return message;
        }
        public async Task<DiscordMessage> GetMessage(ulong id, ulong channelID = 0)
        {
            if (!messages.TryGetValue(id, out DiscordMessage message))
            {
                DiscordChannel channel = await GetChannel(channelID);
                if (channel != null)
                {
                    message = await GetMessage(id, channel);
                }
            }
            return message;
        }
        public async Task<DiscordChannel> GetChannel(ulong id)
        {
            DiscordChannel channel = null;
            try
            {
                channel = await server.GetChannelAsync(id);
                channels.TryAdd(id, channel);
            }
            catch (Exception exception)
            {
                await Log(exception);
                channels.Remove(id);
            }
            return channel;
        }
        public async Task<DiscordRole> GetRole(ulong id)
        {
            DiscordRole role = null;
            try
            {
                role = await server.GetRoleAsync(id);
                roles.TryAdd(id, role);
            }
            catch (Exception exception)
            {
                await Log(exception);
                roles.Remove(id);
            }
            return role;
        }
        public async Task<DiscordMember> GetMember(ulong id)
        {
            DiscordMember member = null;
            try
            {
                member = await server.GetMemberAsync(id);
                members.TryAdd(id, member);
            }
            catch (Exception exception)
            {
                await Log(exception);
                members.Remove(id);
            }
            return member;
        }
        public async Task<bool> EditMessage(DiscordMessage message, DiscordMessageBuilder builder)
        {
            bool success = false;
            try
            {
                await message.ModifyAsync(builder);
                success = true;
            }
            catch (Exception exception)
            {
                await Log(exception);
            }
            return success;
        }
        public async Task<bool> EditMessage(ulong id, DiscordMessageBuilder builder)
        {
            return await EditMessage(await GetMessage(id), builder);
        }
        public async Task<bool> EditMessage(DiscordMessage message, string content)
        {
            return await EditMessage(message, new DiscordMessageBuilder().WithContent(content));
        }
        public async Task<bool> EditMessage(ulong id, string content)
        {
            return await EditMessage(await GetMessage(id), content);
        }
        public async Task<bool> EditChannel(DiscordChannel channel, Action<ChannelEditModel> action)
        {
            bool success = false;
            try
            {
                await channel.ModifyAsync(action);
                success = true;
            }
            catch (Exception exception)
            {
                await Log(exception);
            }
            return success;
        }
        public async Task<bool> EditChannel(ulong id, Action<ChannelEditModel> action)
        {
            return await EditChannel(await GetChannel(id), action);
        }
        public async Task<bool> GiveRole(DiscordMember member, DiscordRole role)
        {
            bool success = false;
            try
            {
                await member.GrantRoleAsync(role);
                success = true;
            }
            catch (Exception exception)
            {
                await Log(exception);
            }
            return success;
        }
        public async Task<bool> GiveRole(DiscordMember member, ulong roleID)
        {
            return await GiveRole(member, await GetRole(roleID));
        }
        public async Task<bool> GiveRole(ulong memberID, DiscordRole role)
        {
            return await GiveRole(await GetMember(memberID), role);
        }
        public async Task<bool> GiveRole(ulong memberID, ulong roleID)
        {
            return await GiveRole(await GetMember(memberID), await GetRole(roleID));
        }
        public async Task<bool> TakeRole(DiscordMember member, DiscordRole role)
        {
            bool success = false;
            try
            {
                await member.RevokeRoleAsync(role);
                success = true;
            }
            catch (Exception exception)
            {
                await Log(exception);
            }
            return success;
        }
        public async Task<bool> TakeRole(DiscordMember member, ulong roleID)
        {
            return await TakeRole(member, await GetRole(roleID));
        }
        public async Task<bool> TakeRole(ulong memberID, DiscordRole role)
        {
            return await TakeRole(await GetMember(memberID), role);
        }
        public async Task<bool> TakeRole(ulong memberID, ulong roleID)
        {
            return await TakeRole(await GetMember(memberID), await GetRole(roleID));
        }
        public async Task<bool> EditMember(DiscordMember member, Action<MemberEditModel> action)
        {
            bool success = false;
            try
            {
                await member.ModifyAsync(action);
                success = true;
            }
            catch (Exception exception)
            {
                await Log(exception);
            }
            return success;
        }
        public async Task<bool> EditMember(ulong id, Action<MemberEditModel> action)
        {
            return await EditMember(await GetMember(id), action);
        }
    }
    [Command("app")]
    [RequireApplicationOwner]
    public static class AppCommands
    {
        [Command("end")]
        [Description("Stops the bot")]
        public static async Task EndCommand(SlashCommandContext context)
        {
            ServerInfo info = DeBOTCBot.activeServers[1396921797160472576];
            await context.DeferResponseAsync(true);
            bool end = false;
            try
            {
                if (info.hasInfo)
                {
                    await info.Destroy();
                }
                end = true;
            }
            catch (Exception exception)
            {
                await info.Log(exception);
            }
            try
            {
                string response = "Command for Debug Server use only!";
                if (context.Guild.Id == 1396921797160472576)
                {
                    response = "Success!";
                }
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                await info.Log(exception);
            }
            finally
            {
                if (end)
                {
                    Environment.Exit(1);
                }
            }
        }
    }
    public static class BOTCCommands
    {
        public static readonly Dictionary<string, string> helpResponses = GenerateHelpResponses();
        public static Dictionary<string, string> GenerateHelpResponses()
        {
            Dictionary<string, string> finalDict = [];
            finalDict.Add("help", "Idk what to tell you");
            finalDict.Add("save", "### Required Permission: Administrator\rForces this server's information to save to the bot's database.");
            finalDict.Add("reset", "### Required Permission: Administrator\rForces bot to remove this server's information, resetting to default values.");
            finalDict.Add("pandemonium", "Sends an ephemeral message with links to the official BOTC website and Patreon.");
            finalDict.Add("tokens show", "Sends an ephemeral message with a list of all character tokens, organised by type.");
            finalDict.Add("tokens description", "Sends an ephemeral message with the token description of a specified character token.");
            finalDict.Add("scripts all", "Sends an ephemeral message with a list of all available scripts.");
            finalDict.Add("scripts show", "Sends an ephemeral message with a list of all characters in a specified script, organised by type.");
            finalDict.Add("scripts new", "### Required Permission: Manage Channels\rAdds a new available script, specifying name and tokens to use, then sends an ephemeral message with the script and its tokens, organised by type.");
            finalDict.Add("scripts edit", "### Required Permission: Manage Channels\rAdds and removes specified tokens from an available script, then sends an ephemeral message with successfully added and removed tokens.");
            finalDict.Add("scripts remove", "### Required Permission: Manage Channels\rRemoves a specified, available, script.");
            finalDict.Add("scripts night", "Creates a night order from a specified, available, script, then sends an ephemeral message with each character token, organised by the order they wake at night.");
            finalDict.Add("scripts roll", "Creates a grimoire from a specified, available, script, and number of players, then sends an ephemeral message with a grimoire, with characters organised by type, and a night order sheet.");
            finalDict.Add("scripts default", "### Required Permission: Manage Channels\rResets available scripts to the 3 official scripts.");
            return finalDict;
        }
        public class HelpAutoComplete : SimpleAutoCompleteProvider
        {
            public readonly static DiscordAutoCompleteChoice[] commands = [..helpResponses.Keys.Select((x) => new DiscordAutoCompleteChoice($"/{x}", x))];
            protected override bool AllowDuplicateValues => false;
            protected override SimpleAutoCompleteStringMatchingMethod MatchingMethod => SimpleAutoCompleteStringMatchingMethod.Fuzzy;
            protected override StringComparison Comparison => StringComparison.InvariantCultureIgnoreCase;
            protected override IEnumerable<DiscordAutoCompleteChoice> Choices => commands;
        }
        [Command("help")]
        [Description("Display details for the specified command")]
        public static async Task BOTCHelpCommand(SlashCommandContext context, [Description("Command to get the details for")][SlashAutoCompleteProvider<HelpAutoComplete>] string command)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            await info.Log($"Running help command for: \"{command}\"");
            await context.DeferResponseAsync(true);
            string response = "Help for this command could not be found!";
            try
            {
                List<string> helpOptions = [..helpResponses.Keys];
                for (int i = 0; i < helpOptions.Count; i++)
                {
                    if (command.AreSimilar(helpOptions[i]))
                    {
                        response = helpResponses[helpOptions[i]];
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                await info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                await info.Log(exception);
            }
        }
        [Command("save")]
        [Description("Force server data to save")]
        [RequirePermissions(DiscordPermission.Administrator)]
        public static async Task ForceSaveCommand(SlashCommandContext context)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            await info.Log("Attempting to save server info");
            await context.DeferResponseAsync(true);
            string response;
            try
            {
                await DeBOTCBot.SaveServerInfo(info);
                response = "Wrote info to file";
                await info.Log(response);
            }
            catch (Exception exception)
            {
                response = "Something went wrong while saving server info!";
                await info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                await info.Log(exception);
            }
        }
        [Command("reset")]
        [Description("Force server data to reset")]
        [RequirePermissions(DiscordPermission.Administrator)]
        public static async Task ForceResetCommand(SlashCommandContext context, [Description("Are you sure?")] bool confirm = false)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            await info.Log("Attempting to reset server info");
            await context.DeferResponseAsync(true);
            string response;
            try
            {
                if (confirm)
                {
                    await DeBOTCBot.ResetServerInfo(info);
                    response = "Reset server info";
                }
                else
                {
                    response = "Confirm that you'd like to reset server info by setting \"confirm\" to \"true\"";
                    await info.Log(response);
                }
            }
            catch (Exception exception)
            {
                response = "Something went wrong while resetting server info!";
                await info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                await info.Log(exception);
            }
        }
        [Command("pandemonium")]
        [Description("See info about the game and its creators")]
        public static async Task BOTCPandemoniumCommand(SlashCommandContext context)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            await info.Log("Attempting to show Pandemonium Institute info!");
            await context.DeferResponseAsync(true);
            string response;
            try
            {
                response = "This bot is not associated with official Blood on The Clocktower or Pandemonium Institute, please see the official website and patreon for official tools, information and to show support for the game\r[Blood on The Clocktower Website](https://bloodontheclocktower.com)\r[Blood on The Clocktower Patreon](https://www.patreon.com/botconline)";
            }
            catch (Exception exception)
            {
                response = "Something went wrong while showing Pandemonium Institute information!";
                await info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                await info.Log(exception);
            }
        }
        [Command("tokens")]
        public static class TokenCommands
        {
            public class TokensAutoComplete : IAutoCompleteProvider
            {
                public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
                {
                    try
                    {
                        List<string> tokens = [..BOTCCharacters.allTokens.Keys];
                        return await Task.Run(() => { return tokens.Where((x) => x.StartsWith(context.UserInput, StringComparison.InvariantCultureIgnoreCase)).Select((x) => new DiscordAutoCompleteChoice(x, x)); });
                    }
                    catch (Exception exception)
                    {
                        _ = DeBOTCBot.activeServers[context.Guild.Id].Log(exception);
                    }
                    return [];
                }
            }
            [Command("show")]
            [Description("Display all possible BOTC tokens")]
            public static async Task BOTCShowTokensCommand(SlashCommandContext context)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log("Attempting to show BOTC tokens!");
                await context.DeferResponseAsync(true);
                string response;
                bool splitMessage = false;
                int halfMessage = -1;
                try
                {
                    response = "# Tokens:";
                    CharacterType[] characterTypes = Enum.GetValues<CharacterType>();
                    int loopHalf = (int)MathF.Floor(characterTypes.Length / 2f) - 1;
                    for (int i = 0; i < characterTypes.Length; i++)
                    {
                        CharacterType type = characterTypes[i];
                        response += $"\r## {type}:\r";
                        List<Token> tokensOfType = [.. BOTCCharacters.allTokens.Values.Where((x) => x.characterType == type)];
                        for (int j = 0; j < tokensOfType.Count; j++)
                        {
                            response += tokensOfType[j].characterName;
                            if (j != tokensOfType.Count - 1)
                            {
                                response += ", ";
                            }
                            else if (i == loopHalf)
                            {
                                halfMessage = response.Length;
                            }
                        }
                    }
                    splitMessage = response.Length > 2000;
                }
                catch (Exception exception)
                {
                    response = "Something went wrong while showing BOTC tokens!";
                    await info.Log(exception);
                }
                try
                {
                    if (splitMessage && halfMessage > -1)
                    {
                        await context.RespondRegistered(info, response[..halfMessage]);
                        await context.FollowupRegistered(info, response[halfMessage..], true);
                    }
                    else
                    {
                        await context.RespondRegistered(info, response);
                    }
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }

            }
            [Command("description")]
            [Description("Display the description of a specific BOTC token")]
            public static async Task BOTCTokenDescriptionCommand(SlashCommandContext context, [Description("Token to show description of")][SlashAutoCompleteProvider<TokensAutoComplete>] string token)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log($"Attempting to show the description of \"{token}\"!");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (BOTCCharacters.TokenExists(token, out string exactToken))
                    {
                        response = $"## {exactToken}:\r{BOTCCharacters.allTokens[exactToken].description}";
                    }
                    else
                    {
                        response = $"Token by name \"{token}\" could not be found!!";
                        await info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while showing the description of \"{token}\"!";
                    await info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
        }
        [Command("scripts")]
        public static class ScriptCommands
        {
            public class ScriptsAutoComplete : IAutoCompleteProvider
            {
                public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
                {
                    ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                    try
                    {
                        Dictionary<string, string[]> scripts = info.botcGame.scripts;
                        if (scripts != null)
                        {
                            return await Task.Run(() => { return scripts.Keys.Where((x) => x.StartsWith(context.UserInput, StringComparison.InvariantCultureIgnoreCase)).Select((x) => new DiscordAutoCompleteChoice(x, x)); });
                        }
                    }
                    catch (Exception exception)
                    {
                        await info.Log(exception);
                    }
                    return [];
                }
            }
            [Command("all")]
            [Description("Display all available BOTC scripts")]
            public static async Task BOTCScriptAllCommand(SlashCommandContext context)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log("Showing all scripts");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (info.botcGame.scripts != null && info.botcGame.scripts.Count > 0)
                    {
                        response = "# Scripts:\r## ";
                        string[] scriptNames = [.. info.botcGame.scripts.Keys];
                        for (int i = 0; i < scriptNames.Length; i++)
                        {
                            response += $"{scriptNames[i]}";
                            if (i != scriptNames.Length - 1)
                            {
                                response += ", ";
                            }
                        }
                    }
                    else
                    {
                        response = $"Could not find any scripts!";
                        await info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while displaying available scripts!";
                    await info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
            [Command("show")]
            [Description("Display the tokens available in a script")]
            public static async Task BOTCScriptShowCommand(SlashCommandContext context, [Description("The script to see the tokens of")][SlashAutoCompleteProvider<ScriptsAutoComplete>] string script)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log($"Showing all tokens in script of name: \"{script}\"");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (info.botcGame.ScriptExists(script, out string correctedScript))
                    {
                        
                        string[] scriptTokens = info.botcGame.scripts[correctedScript];
                        response = $"## {correctedScript}:";
                        CharacterType[] characterTypes = Enum.GetValues<CharacterType>();
                        for (int j = 0; j < characterTypes.Length; j++)
                        {
                            CharacterType type = characterTypes[j];
                            List<Token> tokensOfType = [..BOTCCharacters.allTokens.Values.Where((x) => x.characterType == type && scriptTokens.Contains(x.characterName))];
                            for (int k = 0; k < tokensOfType.Count; k++)
                            {
                                if (k == 0)
                                {
                                    response += $"\r### {type}:\r- ";
                                }
                                response += $"{tokensOfType[k].characterName}";
                                if (k != tokensOfType.Count - 1)
                                {
                                    response += ", ";
                                }
                            }
                        }
                    }
                    else
                    {
                        response = $"Script of name: {script} could not be found!";
                        await info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while displaying the tokens of script of name {script}!";
                    await info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
            [Command("new")]
            [Description("Add a new BOTC script")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCScriptAddCommand(SlashCommandContext context, [Description("The name of the new script")][SlashAutoCompleteProvider<ScriptsAutoComplete>] string script, [Description("Tokens to add to the script (Separated by \",\")")] string tokens)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log($"Adding new script with name: \"{script}\"");
                await context.DeferResponseAsync(true);
                string response;
                bool split = false;
                int halfway = -1;
                try
                {
                    if (!info.botcGame.ScriptExists(script, out string correctedScript))
                    {
                        response = $"# {script}:\r## - Tokens\r";
                        string[] tokensArray = tokens.Split(", ");
                        int loopHalf = (int)MathF.Floor(tokensArray.Length / 2f) - 1;
                        List<string> tokensToAdd = [];
                        for (int i = 0; i < tokensArray.Length; i++)
                        {
                            if (BOTCCharacters.TokenExists(tokensArray[i], out string newToken) && !tokensToAdd.Contains(newToken))
                            {
                                response += $"  - {newToken}\r";
                                tokensToAdd.Add(newToken);
                            }
                            if (i == loopHalf)
                            {
                                halfway = response.Length;
                            }
                        }
                        info.botcGame.scripts.Add(script, [.. tokensToAdd]);
                        await DeBOTCBot.SaveServerInfo(info);
                        split = response.Length > 2000;
                    }
                    else
                    {
                        response = $"Script: \"{correctedScript}\" already exists!";
                        await info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while adding script: \"{script}\"!";
                    await info.Log(exception);
                }
                try
                {
                    if (split && halfway > -1)
                    {
                        await context.RespondRegistered(info, response[..halfway]);
                        await context.FollowupRegistered(info, response[halfway..], true);
                    }
                    else
                    {
                        await context.RespondRegistered(info, response);
                    }
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
            [Command("edit")]
            [Description("Edit the specified BOTC script")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCScriptEditCommand(SlashCommandContext context, [Description("The existing script to edit")][SlashAutoCompleteProvider<ScriptsAutoComplete>] string script, [Description("Tokens to add to the script (Separated by \",\")")] string add = null, [Description("Tokens to remove from the script (Separated by \",\")")] string remove = null)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log($"Editing script with name: \"{script}\"");
                await context.DeferResponseAsync(true);
                string response;
                bool split = false;
                int halfway = -1;
                try
                {
                    if (info.botcGame.ScriptExists(script, out string correctedScript))
                    {
                        response = $"# {correctedScript}\r";
                        List<string> tokensToAdd = [];
                        if (add != null)
                        {
                            string[] toAddArray = add.Split(",");
                            if (toAddArray.Length > 0)
                            {
                                response += "\r## - Adding:\r";
                                for (int i = 0; i < toAddArray.Length; i++)
                                {
                                    if (BOTCCharacters.TokenExists(toAddArray[i], out string newToken) && !tokensToAdd.Contains(newToken) && !info.botcGame.scripts[correctedScript].Contains(newToken))
                                    {
                                        response += $"  - {newToken}\r";
                                        tokensToAdd.Add(newToken);
                                    }
                                }
                            }
                        }
                        halfway = response.Length;
                        List<string> tokensToRemove = [];
                        if (remove != null)
                        {
                            string[] toRemoveArray = remove.Split(",");
                            if (toRemoveArray.Length > 0)
                            {
                                response += "\r## - Removing:\r";
                                for (int i = 0; i < toRemoveArray.Length; i++)
                                {
                                    if (BOTCCharacters.TokenExists(toRemoveArray[i], out string newToken) && !tokensToRemove.Contains(newToken) && info.botcGame.scripts[correctedScript].Contains(newToken))
                                    {
                                        response += $"  - {newToken}\r";
                                        tokensToRemove.Add(newToken);
                                    }
                                }
                            }
                        }
                        List<string> finalTokens = [..info.botcGame.scripts[correctedScript]];
                        finalTokens.AddRange(tokensToAdd);
                        finalTokens.RemoveAll(tokensToRemove.Contains);
                        info.botcGame.scripts[correctedScript] = [..finalTokens];
                        await DeBOTCBot.SaveServerInfo(info);
                        split = response.Length > 2000;
                    }
                    else
                    {
                        response = $"Script: \"{script}\" could not be found!";
                        await info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while editing script: \"{script}\"!";
                    await info.Log(exception);
                }
                try
                {
                    if (split && halfway > -1)
                    {
                        await context.RespondRegistered(info, response[..halfway]);
                        await context.FollowupRegistered(info, response[halfway..], true);
                    }
                    else
                    {
                        await context.RespondRegistered(info, response);
                    }
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
            [Command("remove")]
            [Description("Remove a specified BOTC script")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCScriptRemoveCommand(SlashCommandContext context, [Description("The existing script to remove")][SlashAutoCompleteProvider<ScriptsAutoComplete>] string script)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log($"Removing script with name: \"{script}\"");
                string response;
                await context.DeferResponseAsync(true);
                try
                {
                    if (info.botcGame.ScriptExists(script, out string correctedScript))
                    {
                        info.botcGame.scripts.Remove(correctedScript);
                        response = $"Removed script: \"{correctedScript}\"";
                        await DeBOTCBot.SaveServerInfo(info);
                    }
                    else
                    {
                        response = $"Script: \"{script}\" could not be found!";
                        await info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while removing script: \"{script}\"!";
                    await info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
            [Command("night")]
            [Description("Show the night order for a specific script")]
            public static async Task BOTCScriptOrderCommand(SlashCommandContext context, [Description("The script to see the night order of")][SlashAutoCompleteProvider<ScriptsAutoComplete>] string script)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log($"Displaying night order of script with name: \"{script}\"");
                await context.DeferResponseAsync(true);
                string response;
                bool split = false;
                int halfway = -1;
                try
                {
                    if (info.botcGame.ScriptExists(script, out string correctedScript))
                    {
                        response = $"# {correctedScript}\r{DeBOTCBot.GenerateNightOrderMessage(info.botcGame.scripts[correctedScript], out split, out halfway)}";
                    }
                    else
                    {
                        response = $"Script: \"{script}\" could not be found!";
                        await info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while displaying the night order of script: \"{script}\"!";
                    await info.Log(exception);
                }
                try
                {
                    if (split && halfway > -1)
                    {
                        await context.RespondRegistered(info, response[..halfway]);
                        await context.FollowupRegistered(info, response[halfway..]);
                    }
                    else
                    {
                        await context.RespondRegistered(info, response);
                    }
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
            [Command("roll")]
            [Description("Roll a grimoire")]
            public static async Task BOTCScriptRollCommand(SlashCommandContext context, [Description("The script to use for this roll")][SlashAutoCompleteProvider<ScriptsAutoComplete>] string script, [Description("The number of players to roll for (5 - 15 inclusive)")] int players)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log($"Rolling a grimoire using script with name: \"{script}\", and player count of {players}");
                await context.DeferResponseAsync(true);
                string response;
                string responseNext = string.Empty;
                bool split = false;
                int halfway = -1;
                try
                {
                    if (info.botcGame.ScriptExists(script, out string correctedScript))
                    {
                        string[] scriptTokens = info.botcGame.scripts[correctedScript];
                        response = await Task.Run(() => DeBOTCBot.GenerateTokenMessage(info, scriptTokens, players));
                        responseNext = await Task.Run(() => DeBOTCBot.GenerateNightOrderMessage(scriptTokens, out split, out halfway));
                    }
                    else
                    {
                        response = $"Script with name: \"{script}\" does not exist!";
                        await info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while rolling a grimoire using script: \"{script}\" with player count: \"{players}\"! Are there enough of each token type for this number of players?";
                    await info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                    if (responseNext != string.Empty)
                    {
                        if (split && halfway > -1)
                        {
                            await context.FollowupRegistered(info, responseNext[..halfway], true);
                            await context.FollowupRegistered(info, responseNext[halfway..], true);
                        }
                        else
                        {
                            await context.FollowupRegistered(info, responseNext, true);
                        }
                    }
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
            [Command("default")]
            [Description("Reset scripts to default")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCScriptDefaultCommand(SlashCommandContext context, [Description("Are you sure?")] bool confirm = false)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log($"Setting scripts to default");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (confirm)
                    {
                        info.botcGame.Initialize();
                        response = "Set scripts to default";
                    }
                    else
                    {
                        response = "Confirm that you'd like to reset server info by setting \"confirm\" to \"true\"";
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while setting scripts to default!";
                    await info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
            [Command("json")]
            [Description("Create a JSON file for a specific script")]
            public static async Task BOTCScriptJSONCommand(SlashCommandContext context, [Description("The author of this script")] string author, [Description("The script to create a JSON for")][SlashAutoCompleteProvider<ScriptsAutoComplete>] string script)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                await info.Log($"Creating JSON file for script with name: \"{script}\"");
                string response;
                string filePath = null;
                FileStream file = null;
                await context.DeferResponseAsync(true);
                try
                {
                    if (info.botcGame.ScriptExists(script, out string correctedScript))
                    {
                        response = $"Script JSON created!";
                        filePath = $"{Serialization.infoFilePath}\\{correctedScript.ValidFileName()} by {author.ValidFileName()}.json";
                        StreamWriter stream = File.CreateText(filePath);
                        await stream.WriteAsync(ScriptJSONPart.WriteScriptJSON(author, correctedScript, info.botcGame.scripts[correctedScript].Reverse().ToArray()));
                        stream.Close();
                        file = new(filePath, FileMode.Open);
                        await DeBOTCBot.SaveServerInfo(info);
                    }
                    else
                    {
                        response = $"Script: \"{script}\" could not be found!";
                        await info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while creating JSON for script: \"{script}\"!";
                    await info.Log(exception);
                }
                try
                {
                    if (file == null)
                    {
                        await context.RespondRegistered(info, response);
                    }
                    else
                    {
                        await context.RespondRegistered(info, new DiscordMessageBuilder().WithContent(response).AddFile(file, AddFileOptions.None));
                    }
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
                finally
                {
                    if (file != null)
                    {
                        await file.DisposeAsync();
                    }
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
        }
    }
    public class DeBOTCBot
    {
        public const bool DebugBuild = true;
        public const DiscordIntents Intents = DiscordIntents.Guilds | DiscordIntents.GuildMembers | DiscordIntents.GuildMessages | DiscordIntents.MessageContents | DiscordIntents.GuildVoiceStates;
        public static readonly Dictionary<ulong, ServerInfo> activeServers = [];
        public static async Task Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            await ServerInfo.BotLog("deBOTCBot Started!");
            string token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                await ServerInfo.BotLog("DISCORD_TOKEN was null!");
                Environment.Exit(1);
                return;
            }
            await Serialization.ResetDebugLog();
            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(token, Intents);
            builder.ConfigureEventHandlers(b => b
            .HandleGuildCreated(JoinedServer)
            .HandleComponentInteractionCreated(Interaction)
            .HandleGuildDeleted(LeftServer)
            .HandleGuildUnavailable(ServerUnavailable)
            .HandleGuildAvailable(ServerAvailable)
            .HandleZombied(ConnectionZombied))
            .UseInteractivity(new InteractivityConfiguration() { ResponseBehavior = DSharpPlus.Interactivity.Enums.InteractionResponseBehavior.Ack, Timeout = TimeSpan.FromSeconds(30) })
            .UseCommands(CommandsAction)
            .DisableDefaultLogging();
            DiscordClient client = builder.Build();
            await client.ConnectAsync(new DiscordActivity("Blood on The Clocktower", DiscordActivityType.Playing), DiscordUserStatus.Online);
            await Task.Delay(-1);
        }
        public static void CommandsAction(IServiceProvider serviceProvider, CommandsExtension extension)
        {
            try
            {
                Type[] commandContainers = [typeof(AppCommands), typeof(BOTCCommands)];
                List<CommandBuilder> builders = [];
                for (int i = 0; i < commandContainers.Length; i++)
                {
                    builders.AddRange(CommandsFromType(commandContainers[i], DebugBuild));
                }
                if (DebugBuild)
                {
                    _ = DebugCommandsLoop(builders);
                }
                extension.AddCommands(builders);
            }
            catch (Exception exception)
            {
                _ = ServerInfo.BotLog(exception);
            }
        }
        public static List<CommandBuilder> CommandsFromType(Type type, bool debug = false)
        {
            if (type.GetCustomAttribute<CommandAttribute>() != null)
            {
                CommandBuilder newBuilder = CommandBuilder.From(type);
                if (debug)
                {
                    newBuilder.WithGuildIds([1396921797160472576]);
                }
                return [newBuilder];
            }
            List<CommandBuilder> builders = [];
            MethodInfo[] mainMethods = type.GetMethods();
            for (int i = 0; i < mainMethods.Length; i++)
            {
                MethodInfo method = mainMethods[i];
                if (method.GetCustomAttribute<CommandAttribute>() != null)
                {
                    CommandBuilder newBuilder = CommandBuilder.From(method);
                    if (debug)
                    {
                        newBuilder.WithGuildIds([1396921797160472576]);
                    }
                    builders.Add(newBuilder);
                }
            }
            Type[] nestedTypes = type.GetNestedTypes();
            for (int i = 0; i < nestedTypes.Length; i++)
            {
                Type nestedType = nestedTypes[i];
                
                if (nestedType.GetCustomAttribute<CommandAttribute>() != null)
                {
                    CommandBuilder newBuilder = CommandBuilder.From(nestedType);
                    if (debug)
                    {
                        newBuilder.WithGuildIds([1396921797160472576]);
                    }
                    builders.Add(newBuilder);
                }
                else
                {
                    builders.AddRange(CommandsFromType(nestedType));
                }
            }
            return builders;
        }
        public static async Task DebugCommandsLoop(List<CommandBuilder> builders)
        {
            for (int i = 0; i < builders.Count; i++)
            {
                CommandBuilder builder = builders[i];
                string debugLine = $"Command name: \"{builder}\", ";
                if (builder.Method == null)
                {
                    debugLine += "nested";
                }
                else
                {
                    debugLine += $"method: {builder.Method.Name}";
                }
                if (builder.Subcommands != null && builder.Subcommands.Count > 0)
                {
                    debugLine += $", subcommands: {builder.Subcommands.Count}";
                }
                if (builder.Attributes != null && builder.Attributes.Count > 0)
                {
                    debugLine += ", attributes: ";
                    for (int j = 0; j < builder.Attributes.Count; j++)
                    {
                        if (j != 0)
                        {
                            debugLine += ", ";
                        }
                        debugLine += $"\"{builder.Attributes[j]}\"";
                    }
                }
                if (builder.Parameters != null && builder.Parameters.Count > 0)
                {
                    debugLine += ", parameters: ";
                    for (int j = 0; j < builder.Parameters.Count; j++)
                    {
                        if (j != 0)
                        {
                            debugLine += ", ";
                        }
                        debugLine += $"{builder.Parameters[j].Type} - \"{builder.Parameters[j].Name}\"";
                    }
                }
                await ServerInfo.BotLog(debugLine, ConsoleColor.DarkGreen);
                await DebugCommandsLoop(builder.Subcommands);
            }
        }
        public static async Task SaveServerInfo(ServerInfo info)
        {
            await info.Log("Saving server info");
            ServerSaveInfo saveInfo = new(info);
            await Task.Run(() => Serialization.WriteToFile($"{Serialization.infoFilePath}\\{info.server.Id}.json", saveInfo));
        }
        public static async Task ResetServerInfo(ServerInfo info)
        {
            await info.Log("Resetting server info");
            ServerInfo newInfo = new(info.server);
            ServerSaveInfo saveInfo = new(newInfo);
            await newInfo.FillSavedValues(saveInfo);
            activeServers[info.server.Id] = newInfo;
            await Task.Run(() => Serialization.WriteToFile($"{Serialization.infoFilePath}\\{info.server.Id}.json", saveInfo));
        }
        public static async Task<ServerSaveInfo> GetSavedInfo(ulong id)
        {
            return await Serialization.ReadFromFile<ServerSaveInfo>($"{Serialization.infoFilePath}\\{id}.json");
        }
        public static async Task InitializeServer(DiscordGuild guild)
        {
            try
            {
                ulong id = guild.Id;
                ServerInfo info = new(guild);
                ServerSaveInfo saveInfo = await GetSavedInfo(id);
                if (saveInfo != null)
                {
                    await info.FillSavedValues(saveInfo);
                }
                activeServers.Add(id, info);
            }
            catch (Exception exception)
            {
                await ServerInfo.BotLog($"Failed to load server: \"{guild.Name}\"");
                await ServerInfo.BotLog(exception);
            }
        }
        public static async Task Interaction(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            if (!args.Id.StartsWith("deB_BOTC"))
            {
                return;
            }
            ServerInfo info = activeServers[args.Guild.Id];
            await info.Log($"Button Pressed with ID: \"{args.Id}\"");
            if (args.Interaction.ResponseState == DiscordInteractionResponseState.Replied)
            {
                info.RegisterMessage(await args.Interaction.GetOriginalResponseAsync());
            }
            else if (args.Interaction.ResponseState == DiscordInteractionResponseState.Unacknowledged)
            {
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
            }
        }
        public static async Task JoinedServer(DiscordClient client, GuildCreatedEventArgs args)
        {
            DiscordGuild guild = args.Guild;
            await ServerInfo.BotLog($"Joined server: \"{guild.Name}\"");
            await InitializeServer(guild);
        }
        public static async Task LeftServer(DiscordClient client, GuildDeletedEventArgs args)
        {
            ulong id = args.Guild.Id;
            ServerInfo info = activeServers[id];
            try
            {
                await SaveServerInfo(info);
                activeServers.Remove(id);
            }
            catch (Exception exception)
            {
                await info.Log(exception);
            }
        }
        public static async Task ServerUnavailable(DiscordClient client, GuildUnavailableEventArgs args)
        {
            DiscordGuild guild = args.Guild;
            ulong id = guild.Id;
            await ServerInfo.BotLog($"Server: \"{guild.Name}\" is unavailable!");
            activeServers.TryGetValue(id, out ServerInfo info);
            try
            {
                if (info != null)
                {
                    await SaveServerInfo(info);
                    activeServers.Remove(id);
                }
            }
            catch (Exception exception)
            {
                if (info != null)
                {
                    await info.Log(exception);
                }
                else
                {
                    await ServerInfo.BotLog($"Error occurred as server \"{guild.Name}\" became unavailable!");
                    await ServerInfo.BotLog(exception);
                }
            }
        }
        public static async Task ServerAvailable(DiscordClient client, GuildAvailableEventArgs args)
        {
            DiscordGuild guild = args.Guild;
            ulong id = guild.Id;
            await ServerInfo.BotLog($"Server: \"{guild.Name}\" is available!");
            activeServers.TryGetValue(id, out ServerInfo info);
            try
            {
                if (info == null)
                {
                    await InitializeServer(guild);
                }
                else
                {
                    ServerSaveInfo saveInfo = await GetSavedInfo(id);
                    saveInfo ??= new(info);
                    await info.FillSavedValues(saveInfo);
                }
            }
            catch (Exception exception)
            {
                if (info != null)
                {
                    await info.Log(exception);
                }
                else
                {
                    await ServerInfo.BotLog($"Error occurred as server \"{guild.Name}\" became available!");
                    await ServerInfo.BotLog(exception);
                }
            }
        }
        public static async Task ConnectionZombied(DiscordClient client, ZombiedEventArgs args)
        {
            await ServerInfo.BotLog($"Connection zombied!");
            List<ulong> serverIDs = [..activeServers.Keys];
            for (int i = 0; i < serverIDs.Count; i++)
            {
                ulong id = serverIDs[i];
                ServerInfo info = activeServers[id];
                try
                {
                    await SaveServerInfo(info);
                }
                catch (Exception exception)
                {
                    await info.Log(exception);
                }
            }
            activeServers.Clear();
        }
        public static string GenerateTokenMessage(ServerInfo info, string[] finalScript, int players)
        {
            bool playerCountChange = false;
            string finalString = "# Tokens\r\r";
            if (players < 5 || players > 15)
            {
                players = Math.Clamp(players, 5, 15);
                playerCountChange = true;
            }
            List<Token> tokens = info.botcGame.RollTokens([..finalScript.Distinct()], players, out bool monsta);
            if (monsta)
            {
                finalString += "**Demon**: Lil' Monsta\r";
            }
                string[] tokenNames = [..tokens.Select((x) => x.characterName)];
                for (int i = 0; i < tokenNames.Length; i++)
                {
                    finalString += $"**{BOTCCharacters.allTokens[tokenNames[i]].characterType}**: {tokenNames[i]}\r";
                }
            if (playerCountChange)
            {
                finalString += "-# Player count was invalid and was clamped between 5 and 15!";
            }
            return finalString;
        }
        public static string GenerateNightOrderMessage(string[] scriptNames, out bool split, out int half)
        {
            string finalString = "## Night 0 Order:\r";
            List<Token> scriptTokens = [..BOTCCharacters.allTokens.Values.Where((x) => scriptNames.Contains(x.characterName) && (x.firstOrder != -1 || x.otherOrder != -1))];
            scriptTokens.Sort((x, y) => x.firstOrder.CompareTo(y.firstOrder));
            int tokenNo = 1;
            for (int i = 0; i < scriptTokens.Count; i++)
            {
                Token token = scriptTokens[i];
                if (token.firstOrder == -1)
                {
                    continue;
                }
                finalString += $"**{tokenNo} - {token.characterName}**: {BOTCCharacters.firstNightOrder[token.firstOrder].Item2}\r";
                tokenNo++;
            }
            half = finalString.Length;
            finalString += "\r## Other Nights Order:\r";
            scriptTokens.Sort((x, y) => x.otherOrder.CompareTo(y.otherOrder));
            tokenNo = 1;
            for (int i = 0; i < scriptTokens.Count; i++)
            {
                Token token = scriptTokens[i];
                if (token.otherOrder == -1)
                {
                    continue;
                }
                finalString += $"**{tokenNo} - {token.characterName}**: {BOTCCharacters.otherNightOrder[token.otherOrder].Item2}\r";
                tokenNo++;
            }
            split = finalString.Length > 2000;
            return finalString;
        }
        public static int Iterate(int count, int index, bool clock = true)
        {
            if (clock)
            {
                index++;
            }
            else
            {
                index--;
            }
            if (index >= count)
            {
                index = 0;
            }
            else if (index < 0)
            {
                index = count - 1;
            }
            return index;
        }
    }
}