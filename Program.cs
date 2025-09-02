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
            storytellerRoleID = info.storytellerRole;
            genericPlayerRoleID = info.genericPlayerRole;
            announcementsChannelID = info.announcementsChannel;
            storytellerChannelID = info.storytellerChannel;
            townChannelID = info.townChannel;
            homesCategoryID = info.homesCategory;
            botcScripts = info.botcGame.scripts;
            botcChannels = info.townChannels;
            botcHomes = info.homeChannels;
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
        public class InGameControls(ulong newControls)
        {
            public ulong controls = newControls;
            public List<ulong> scriptMessages = [];
        }
        public readonly static Type[] surpressedExceptions = [typeof(NotFoundException), typeof(OperationCanceledException), typeof(TaskCanceledException)];
        public Dictionary<ulong, DiscordMessage> messages = [];
        public Dictionary<ulong, DiscordChannel> channels = [];
        public Dictionary<ulong, DiscordRole> roles = [];
        public Dictionary<ulong, DiscordMember> members = [];
        public bool hasInfo = false;
        public DiscordGuild server = guild;
        public ulong storytellerRole;
        public ulong genericPlayerRole;
        public ulong announcementsChannel;
        public ulong storytellerChannel;
        public ulong townChannel;
        public Dictionary<string, int> townChannels = [];
        public List<string> homeChannels;
        public ulong homesCategory;
        public ulong currentStoryteller;
        public DiscordMessageBuilder controlsMessageBuilder;
        public DiscordMessageBuilder controlsInGameMessageBuilder;
        public ulong voteMessage;
        public InGameControls storytellerControls;
        public Dictionary<ulong, ulong> playerDictionary;
        public BOTCCharacters botcGame = new();
        public bool gameStarted;
        public CancellationTokenSource voteCancelToken;
        public async Task Initialize()
        {
            await Destroy(true);
            DiscordPermissions storytellerPerms = new(DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.Connect, DiscordPermission.Speak, DiscordPermission.MuteMembers, DiscordPermission.DeafenMembers, DiscordPermission.MoveMembers, DiscordPermission.PrioritySpeaker);
            Log("Creating Storyteller role");
            storytellerRole = await NewRole("Storyteller", color: DiscordColor.Goldenrod, hoist: true, mentionable: true);
            Log("Creating BOTC Player role");
            genericPlayerRole = await NewRole("BOTC Player");
            Log("Creating Storyteller channels");
            ulong storytellerCategory = await NewChannel("Storyteller's Corner", DiscordChannelType.Category, overwrites: [new DiscordOverwriteBuilder(roles[storytellerRole]) { Allowed = storytellerPerms }]);
            announcementsChannel = await NewChannel("botc-announcements", DiscordChannelType.Text, storytellerCategory, overwrites: [new DiscordOverwriteBuilder(server.EveryoneRole) { Allowed = DiscordPermission.ViewChannel, Denied = DiscordPermission.SendMessages }]);
            await NewChannel("Watchtower", DiscordChannelType.Voice, storytellerCategory, overwrites: [new DiscordOverwriteBuilder(roles[genericPlayerRole]) { Denied = new DiscordPermissions(DiscordPermission.ViewChannel, DiscordPermission.Connect) }]);
            storytellerChannel = await NewChannel("Storyteller's Crypt", DiscordChannelType.Voice, storytellerCategory, overwrites: [new DiscordOverwriteBuilder(roles[storytellerRole]) { Allowed = storytellerPerms }, new DiscordOverwriteBuilder(roles[genericPlayerRole]) { Denied = new DiscordPermissions(DiscordPermission.ViewChannel, DiscordPermission.Connect) }, new DiscordOverwriteBuilder(server.EveryoneRole) { Denied = new DiscordPermissions(DiscordPermission.Connect, DiscordPermission.SendMessages) }]);
            Log("Creating Town channels");
            ulong townCategory = await NewChannel("Town", DiscordChannelType.Category, overwrites: [new DiscordOverwriteBuilder(roles[storytellerRole]) { Allowed = storytellerPerms }, new DiscordOverwriteBuilder(roles[genericPlayerRole]) { Allowed = new DiscordPermissions(DiscordPermission.ViewChannel, DiscordPermission.Connect) }, new DiscordOverwriteBuilder(server.EveryoneRole) { Allowed = DiscordPermission.ViewChannel, Denied = new DiscordPermissions(DiscordPermission.Connect, DiscordPermission.SendMessages) }]);
            townChannel = await NewChannel("Town Square", DiscordChannelType.Voice, townCategory, userLimit: 0);
            if (townChannels == null || (townChannels != null && townChannels.Count == 0))
            {
                townChannels = DeBOTCBot.DefaultTownData();
            }
            foreach (KeyValuePair<string, int> pair in townChannels)
            {
                await NewChannel(pair.Key, DiscordChannelType.Voice, townCategory, userLimit: pair.Value);
            }
            Log("Creating Homes category");
            homesCategory = await NewChannel("Homes", DiscordChannelType.Category, overwrites: [new DiscordOverwriteBuilder(roles[storytellerRole]) { Allowed = storytellerPerms }, new DiscordOverwriteBuilder(roles[genericPlayerRole]) { Allowed = new DiscordPermissions(DiscordPermission.SendMessages, DiscordPermission.Connect), Denied = new DiscordPermissions(DiscordPermission.ViewChannel) }, new DiscordOverwriteBuilder(server.EveryoneRole) { Allowed = DiscordPermission.ViewChannel, Denied = new DiscordPermissions(DiscordPermission.Connect, DiscordPermission.SendMessages) }]);
            await DeBOTCBot.SaveServerInfo(DeBOTCBot.activeServers[server.Id]);
            hasInfo = true;
        }
        public async Task FillSavedValues(ServerSaveInfo savedInfo)
        {
            List<DiscordChannel> parentChannels = [];
            DiscordRole role = await GetRole(savedInfo.storytellerRoleID);
            if (role != null)
            {
                storytellerRole = role.Id;
                RegisterRole(role);
            }
            role = await GetRole(savedInfo.genericPlayerRoleID);
            if (role != null)
            {
                genericPlayerRole = role.Id;
                RegisterRole(role);
            }
            DiscordChannel channel = await GetChannel(savedInfo.announcementsChannelID);
            if (channel != null)
            {
                announcementsChannel = channel.Id;
            }
            channel = await GetChannel(savedInfo.storytellerChannelID);
            if (channel != null)
            {
                storytellerChannel = channel.Id;
                if (channel.Parent != null)
                {
                    parentChannels.Add(channel.Parent);
                }
            }
            channel = await GetChannel(savedInfo.townChannelID);
            if (channel != null)
            {
                townChannel = channel.Id;
                if (channel.Parent != null)
                {
                    parentChannels.Add(channel.Parent);
                }
            }
            channel = await GetChannel(savedInfo.homesCategoryID);
            if (channel != null)
            {
                homesCategory = channel.Id;
                parentChannels.Add(channel);
            }
            List<DiscordChannel> existingChannels = [];
            for (int i = 0; i < parentChannels.Count; i++)
            {
                DiscordChannel nextChannel = parentChannels[i];
                existingChannels.Add(nextChannel);
                existingChannels.AddRange(nextChannel.Children);
            }
            for (int i = 0; i < existingChannels.Count; i++)
            {
                RegisterChannel(existingChannels[i]);
            }
            botcGame.scripts = savedInfo.botcScripts;
            botcGame.Initialize(this);
            townChannels = savedInfo.botcChannels;
            if (townChannels == null || (townChannels != null && townChannels.Count == 0))
            {
                townChannels = DeBOTCBot.DefaultTownData();
            }
            homeChannels = savedInfo.botcHomes;
            if (homeChannels == null || (homeChannels != null && homeChannels.Count == 0))
            {
                homeChannels = DeBOTCBot.DefaultHomeData();
            }
            hasInfo = storytellerRole != 0 || genericPlayerRole != 0 || announcementsChannel != 0 || storytellerChannel != 0 || townChannel != 0 || homesCategory != 0;
        }
        public async Task Destroy(bool refresh = false)
        {
            Log("Destroying BOTC channels");
            gameStarted = false;
            playerDictionary?.Clear();
            botcGame.currentNomination = null;
            botcGame.playerSeats?.Clear();
            currentStoryteller = 0;
            storytellerControls = null;
            controlsMessageBuilder = null;
            controlsInGameMessageBuilder = null;
            voteMessage = 0;
            ulong storytellerCategory = 0;
            if (channels.TryGetValue(storytellerChannel, out DiscordChannel storytellerChannelRef) && storytellerChannelRef.Parent != null)
            {
                storytellerCategory = storytellerChannelRef.Parent.Id;
            }
            storytellerChannel = 0;
            ulong townCategory = 0;
            if (channels.TryGetValue(townChannel, out DiscordChannel townChannelRef) && townChannelRef.Parent != null)
            {
                townCategory = townChannelRef.Parent.Id;
            }
            townChannel = 0;
            List<ulong> childChannels = [];
            if (storytellerCategory != 0)
            {
                childChannels.AddRange([..channels[storytellerCategory].Children.Select((x) => x.Id)]);
            }
            if (townCategory != 0)
            {
                childChannels.AddRange([..channels[townCategory].Children.Select((x) => x.Id)]);
            }
            if (homesCategory != 0)
            {
                childChannels.AddRange([..channels[homesCategory].Children.Select((x) => x.Id)]);
            }
            for (int i = 0; i < childChannels.Count; i++)
            {
                await DeleteChannel(childChannels[i]);
            }
            if (homesCategory != 0)
            {
                await DeleteChannel(homesCategory);
            }
            homesCategory = 0;
            if (townCategory != 0)
            {
                await DeleteChannel(townCategory);
            }
            if (storytellerCategory != 0)
            {
                await DeleteChannel(storytellerCategory);
            }
            Log("Destroying BOTC roles");
            if (genericPlayerRole != 0)
            {
                await DeleteRole(genericPlayerRole);
            }
            genericPlayerRole = 0;
            if (storytellerRole != 0)
            {
                await DeleteRole(storytellerRole);
            }
            storytellerRole = 0;
            if (!refresh)
            {
                await DeBOTCBot.SaveServerInfo(this);
            }
            hasInfo = false;
        }
        public void Log(string contents, ConsoleColor overrideColour = ConsoleColor.Gray, LogType type = LogType.Info)
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
            Serialization.WriteLog(server.Name, contents, type);
        }
        public void Log(Exception exception)
        {
            Type exceptionType = exception.GetType();
            if (surpressedExceptions.Contains(exceptionType))
            {
                string exceptionName = exceptionType.ToString().Split('.').Last();
                Log($"Surpressed {exceptionName}", type: LogType.Surpressed);
                return;
            }
            Log($"{exception.GetType()}, {exception.Message}\n{exception.StackTrace}", ConsoleColor.Red, LogType.Error);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
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
                Log(exception);
            }
            return success;
        }
        public async Task<bool> EditMember(ulong id, Action<MemberEditModel> action)
        {
            return await EditMember(await GetMember(id), action);
        }
    }
    [Command("app")]
    public class AppCommands
    {
        [Command("end")]
        [Description("Ends the session")]
        [RequireApplicationOwner]
        public static async Task EndCommand(SlashCommandContext context)
        {
            ServerInfo debugServerInfo = DeBOTCBot.activeServers[1396921797160472576];
            await context.DeferResponseAsync(true);
            bool end = false;
            try
            {
                if (debugServerInfo.hasInfo)
                {
                    await debugServerInfo.Destroy();
                    end = true;
                }
            }
            catch (Exception exception)
            {
                debugServerInfo.Log(exception);
            }
            try
            {
                string response = "Command for Debug Server use only!";
                if (context.Guild.Id == 1396921797160472576)
                {
                    response = "Success!";
                }
                await context.RespondRegistered(debugServerInfo, response);
            }
            catch (Exception exception)
            {
                debugServerInfo.Log(exception);
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
    [Command("botc")]
    public class BOTCCommands
    {
        [Command("create")]
        [Description("Create BOTC channels and roles")]
        [RequirePermissions(DiscordPermission.ManageChannels)]
        public static async Task BOTCCreateCommand(SlashCommandContext context)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            info.Log("Attempting to create BOTC environment!");
            await context.DeferResponseAsync(true);
            string response;
            try
            {
                if (info.hasInfo)
                {
                    response = "BOTC environment already created!";
                }
                else
                {
                    await info.Initialize();
                    response = "Successfully created BOTC environment!";
                }
                info.Log(response);
            }
            catch (Exception exception)
            {
                response = "Something went wrong while trying to create BOTC environment!";
                info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                info.Log(exception);
            }
        }
        [Command("storyteller")]
        [Description("Choose a server member to become the storyteller")]
        [RequirePermissions(DiscordPermission.ManageChannels)]
        public static async ValueTask BOTCStorytellerCommand(SlashCommandContext context, [Description("User to turn into the storyteller")] DiscordMember member)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            info.Log("Attempting to choose a new storyteller!");
            await context.DeferResponseAsync(true);
            string response;
            try
            {
                DiscordRole role = await info.GetRole(info.storytellerRole);
                if (!info.hasInfo || role == null)
                {
                    response = "Invalid BOTC environment!";
                }
                else if (info.currentStoryteller != member.Id)
                {
                    response = string.Empty;
                    if (info.currentStoryteller != 0)
                    {
                        DiscordMember storyteller = await info.GetMember(info.currentStoryteller);
                        response += $"\"{storyteller.DisplayName}\" is no longer the storyteller,\r";
                        await info.TakeRole(storyteller, role);
                        info.Log("Deleting Storyteller Controls");
                        await info.DeleteMessage(info.storytellerControls.controls);
                        for (int i = 0; i < info.storytellerControls.scriptMessages.Count; i++)
                        {
                            await info.DeleteMessage(info.storytellerControls.scriptMessages[i]);
                        }
                        info.storytellerControls = null;
                    }
                    response += $"\"{member.DisplayName}\" is the new storyteller";
                    await info.GiveRole(member, role);
                    info.currentStoryteller = (await info.GetMember(member.Id)).Id;
                }
                else
                {
                    response = $"\"{member.DisplayName}\" was already the storyteller!";
                }
            }
            catch (Exception exception)
            {
                response = "Something went wrong while choosing a storyteller!";
                info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                info.Log(exception);
            }
        }
        [Command("destroy")]
        [Description("Remove BOTC channels and roles")]
        [RequirePermissions(DiscordPermission.ManageChannels)]
        public static async Task BOTCDestroyCommand(SlashCommandContext context)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            info.Log("Attempting to destroy BOTC environment!");
            await context.DeferResponseAsync(true);
            string response;
            try
            {
                if (info.hasInfo)
                {
                    await info.Destroy();
                    response = "Successfully destroyed BOTC environment!";
                }
                else
                {
                    response = "No BOTC environment was found!";
                }
                info.Log(response);
            }
            catch (Exception exception)
            {
                response = "Something went wrong while trying to destroy BOTC environment!";
                info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                info.Log(exception);
            }
        }
        [Command("save")]
        [Description("Force server data to save")]
        [RequirePermissions(DiscordPermission.Administrator)]
        public static async Task ForceSaveCommand(SlashCommandContext context)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            info.Log("Attempting to save server info");
            await context.DeferResponseAsync(true);
            string response;
            try
            {
                await DeBOTCBot.SaveServerInfo(info);
                response = "Wrote info to file";
                info.Log(response);
            }
            catch (Exception exception)
            {
                response = "Something went wrong while saving server info!";
                info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                info.Log(exception);
            }
        }
        [Command("reset")]
        [Description("Force server data to reset")]
        [RequirePermissions(DiscordPermission.Administrator)]
        public static async Task ForceResetCommand(SlashCommandContext context, [Description("Are you sure?")] bool confirm = false)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            info.Log("Attempting to reset server info");
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
                    info.Log(response);
                }
            }
            catch (Exception exception)
            {
                response = "Something went wrong while resetting server info!";
                info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                info.Log(exception);
            }
        }
        [Command("pandemonium")]
        [Description("See info about the game and its creators")]
        public static async Task BOTCPandemoniumCommand(SlashCommandContext context)
        {
            ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
            info.Log("Attempting to show Pandemonium Institute info!");
            await context.DeferResponseAsync(true);
            string response;
            try
            {
                response = "This bot is not associated with official Blood on The Clocktower or Pandemonium Institute, please see the official website and patreon for official tools, information and to show support for the game\r[Blood on The Clocktower Website](https://bloodontheclocktower.com)\r[Blood on The Clocktower Patreon](https://www.patreon.com/botconline)";
            }
            catch (Exception exception)
            {
                response = "Something went wrong while showing Pandemonium Institute information!";
                info.Log(exception);
            }
            try
            {
                await context.RespondRegistered(info, response);
            }
            catch (Exception exception)
            {
                info.Log(exception);
            }
        }
        [Command("tokens")]
        public class TokenCommands
        {
            [Command("show")]
            [Description("Display all possible BOTC tokens")]
            public static async Task ShowTokensCommand(SlashCommandContext context)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log("Attempting to show BOTC tokens!");
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
                    info.Log(exception);
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
                    info.Log(exception);
                }

            }
            [Command("description")]
            [Description("Display the description of a specific BOTC token")]
            public static async Task TokenDescriptionCommand(SlashCommandContext context, [Description("Token to show description of")] string token)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Attempting to show the description of \"{token}\"!");
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
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while showing the description of \"{token}\"!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
        }
        [Command("scripts")]
        public class ScriptCommands
        {
            [Command("all")]
            [Description("Display all available BOTC scripts")]
            public static async Task BOTCScriptAllCommand(SlashCommandContext context)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log("Showing all scripts");
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
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while displaying available scripts!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("show")]
            [Description("Display the tokens available in a script")]
            public static async Task BOTCScriptShowCommand(SlashCommandContext context, [Description("The script to see the tokens of")] string script)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Showing all tokens in script of name: \"{script}\"");
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
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while displaying the tokens of script of name {script}!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("new")]
            [Description("Add a new BOTC script")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCScriptAddCommand(SlashCommandContext context, [Description("The name of the new script")] string script, [Description("Tokens to add to the script (Separated by \",\")")] string tokens)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Adding new script with name: \"{script}\"");
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
                        if (info.storytellerControls != null && info.gameStarted)
                        {
                            await info.EditMessage(info.storytellerControls.controls, await DeBOTCBot.UpdateControls(info, true));
                        }
                        await DeBOTCBot.SaveServerInfo(info);
                        split = response.Length > 2000;
                    }
                    else
                    {
                        response = $"Script: \"{correctedScript}\" already exists!";
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while adding script: \"{script}\"!";
                    info.Log(exception);
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
                    info.Log(exception);
                }
            }
            [Command("edit")]
            [Description("Edit the specified BOTC script")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCScriptEditCommand(SlashCommandContext context, [Description("The existing script to edit")] string script, [Description("Tokens to add to the script (Separated by \",\")")] string add = null, [Description("Tokens to remove from the script (Separated by \",\")")] string remove = null)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Editing script with name: \"{script}\"");
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
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while editing script: \"{script}\"!";
                    info.Log(exception);
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
                    info.Log(exception);
                }
            }
            [Command("remove")]
            [Description("Remove a specified BOTC script")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCScriptRemoveCommand(SlashCommandContext context, [Description("The existing script to remove")] string script)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Removing script with name: \"{script}\"");
                string response;
                await context.DeferResponseAsync(true);
                try
                {
                    if (info.botcGame.ScriptExists(script, out string correctedScript))
                    {
                        info.botcGame.scripts.Remove(correctedScript);
                        response = $"Removed script: \"{correctedScript}\"";
                        if (info.storytellerControls != null && info.storytellerControls.controls != 0 && info.gameStarted)
                        {
                            await info.EditMessage(info.storytellerControls.controls, await DeBOTCBot.UpdateControls(info, true));
                        }
                        await DeBOTCBot.SaveServerInfo(info);
                    }
                    else
                    {
                        response = $"Script: \"{script}\" could not be found!";
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while removing script: \"{script}\"!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("night")]
            [Description("Show the night order for a specific script")]
            public static async Task BOTCScriptOrderCommand(SlashCommandContext context, [Description("The script to see the night order of")] string script)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Displaying night order of script with name: \"{script}\"");
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
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while displaying the night order of script: \"{script}\"!";
                    info.Log(exception);
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
                    info.Log(exception);
                }
            }
            [Command("roll")]
            [Description("Roll a grimoire")]
            public static async Task BOTCScriptRollCommand(SlashCommandContext context, [Description("The script to use for this roll")] string script, [Description("The number of players to roll for (5 - 15 inclusive)")] int players)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Rolling a grimoire using script with name: \"{script}\", and player count of {players}");
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
                        response = DeBOTCBot.GenerateTokenMessage(info, scriptTokens, players);
                        responseNext = $"{DeBOTCBot.GenerateNightOrderMessage(scriptTokens, out split, out halfway)}";
                    }
                    else
                    {
                        response = $"Script with name: \"{script}\" does not exist!";
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while rolling a grimoire using script: \"{script}\" with player count: \"{players}\"! Are there enough of each token type for this number of players?";
                    info.Log(exception);
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
                    info.Log(exception);
                }
            }
            [Command("default")]
            [Description("Reset scripts to default")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCScriptDefaultCommand(SlashCommandContext context, [Description("Are you sure?")] bool confirm = false)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Setting scripts to default");
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
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
        }
        [Command("town")]
        public class ChannelCommands
        {
            [Command("add")]
            [Description("Add a town channel")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCChannelAddCommand(SlashCommandContext context, [Description("Name of channel to add")] string channel, [Description("User limit for new channel")] int voiceLimit)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Adding new town channel with name \"{channel}\" and voice limit {voiceLimit}");
                await context.DeferResponseAsync(true);
                bool dictAdded = false;
                int limitClamped = Math.Min(Math.Max(0, voiceLimit), 99);
                string response;
                try
                {
                    if (info.townChannels.TryAdd(channel, limitClamped))
                    {
                        response = $"Added new town channel named \"{channel}\" with voice limit {limitClamped}";
                        dictAdded = true;
                        DiscordChannel townChannel = await info.GetChannel(info.townChannel);
                        if (townChannel != null && townChannel.Parent != null)
                        {
                            await info.NewChannel(channel, DiscordChannelType.Voice, townChannel.Parent, userLimit: limitClamped);
                        }
                        await DeBOTCBot.SaveServerInfo(info);
                    }
                    else
                    {
                        response = $"Town channels already contained a channel named \"{channel}\"!";
                    }
                    info.Log(response);
                }
                catch (Exception exception)
                {
                    if (dictAdded)
                    {
                        response = $"Successfully added new channel to list with name \"{channel}\" and voice limit {limitClamped}, but could not create the channel!";
                        info.Log(response);
                    }
                    else
                    {
                        response = "Something went wrong while adding new town channel!";
                    }
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("remove")]
            [Description("Remove a town channel")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCChannelRemoveCommand(SlashCommandContext context, [Description("Name of channel to remove")] string channel)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Removing town channel with name \"{channel}\"");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (info.townChannels.Remove(channel))
                    {
                        DiscordChannel townChannel = await info.GetChannel(info.townChannel);
                        if (townChannel != null && townChannel.Parent != null)
                        {
                            DiscordChannel childChannel = townChannel.Parent.Children.Where((x) => x.Name == channel).SingleOrDefault();
                            if (childChannel != null)
                            {
                                await info.DeleteChannel(childChannel);
                            }
                        }
                        response = $"Removed town channel named \"{channel}\"";
                        await DeBOTCBot.SaveServerInfo(info);
                    }
                    else
                    {
                        response = $"Town channels did not contain a channel named \"{channel}\"!";
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while removing channel named \"{channel}\"";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("edit")]
            [Description("Edit an existing town channel")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCChannelEditCommand(SlashCommandContext context, [Description("Name of channel to edit")] string channel, [Description("New name for this channel")] string newName = "", [Description("New user limit for this new channel")] int voiceLimit = -1)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Editing town channel with name \"{channel}\", with new name \"{newName}\" and voice limit of {voiceLimit}");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (voiceLimit == -1 && info.townChannels.TryGetValue(channel, out int value))
                    {
                        voiceLimit = value;
                    }
                    if (info.townChannels.Remove(channel))
                    {
                        if (newName == "")
                        {
                            newName = channel;
                        }
                        int limitClamped = Math.Min(Math.Max(0, voiceLimit), 99);
                        info.townChannels.Add(newName, limitClamped);
                        DiscordChannel townChannel = await info.GetChannel(info.townChannel);
                        if (townChannel != null && townChannel.Parent != null)
                        {
                            DiscordChannel childChannel = townChannel.Parent.Children.Where((x) => x.Name == channel).SingleOrDefault();
                            if (childChannel != null)
                            {
                                await info.EditChannel(childChannel, delegate (ChannelEditModel model) { model.Name = newName; model.Userlimit = limitClamped; });
                            }
                        }
                        response = $"Changed name of channel \"{channel}\" to \"{newName}\" and the voice limit to {limitClamped}";
                        await DeBOTCBot.SaveServerInfo(info);
                    }
                    else
                    {
                        response = $"Town channels did not contain a channel named \"{channel}\"!";
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while editing channel with name {channel} and a voice limit of {voiceLimit}!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("show")]
            [Description("Show current town channels")]
            public static async Task BOTCChannelShowCommand(SlashCommandContext context)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Showing current town channels");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    response = "# Channels:\r";
                    List<string> channelNames = [..info.townChannels.Keys];
                    for (int i = 0; i < channelNames.Count; i++)
                    {
                        response += $"- **{channelNames[i]}**, {info.townChannels[channelNames[i]]}\r";
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while showing town channels!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("default")]
            [Description("Reset town to default")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCChannelDefaultCommand(SlashCommandContext context, [Description("Are you sure?")] bool confirm = false)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Setting possible home names to default");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (confirm)
                    {
                        Dictionary<string, int> defaultTown = DeBOTCBot.DefaultTownData();
                        if (!info.townChannels.SequenceEqual(defaultTown))
                        {
                            info.townChannels = defaultTown;
                            DiscordChannel townChannel = await info.GetChannel(info.townChannel);
                            if (townChannel != null && townChannel.Parent != null)
                            {
                                List<DiscordChannel> channels = [..townChannel.Parent.Children];
                                for (int i = 0; i < channels.Count; i++)
                                {
                                    await info.DeleteChannel(channels[i]);
                                }
                                string[] channelNames = [..info.townChannels.Keys];
                                for (int i = 0; i < channelNames.Length; i++)
                                {
                                    await info.NewChannel(channelNames[i], DiscordChannelType.Voice, townChannel.Parent, userLimit: info.townChannels[channelNames[i]]);
                                }
                            }
                            response = "Set town channels to default";
                        }
                        else
                        {
                            response = "Town channels are already default!";
                            info.Log(response);
                        }
                    }
                    else
                    {
                        response = "Confirm that you'd like to reset server info by setting \"confirm\" to \"true\"";
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while setting town channels to default!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
        }
        [Command("homes")]
        public class HomeCommands
        {
            [Command("show")]
            [Description("Show current home names")]
            public static async Task BOTCHomesShowCommand(SlashCommandContext context)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Showing current home names");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    response = "# Homes:\r";
                    List<string> homeNames = info.homeChannels;
                    for (int i = 0; i < homeNames.Count; i++)
                    {
                        response += $"- {homeNames[i]}\r";
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while showing home names!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("add")]
            [Description("Add a new possible home name")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCHomesAddCommand(SlashCommandContext context, [Description("Name of new home")] string name)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Adding new home with name {name}");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (!info.homeChannels.Contains(name))
                    {
                        info.homeChannels.Add(name);
                        response = $"Added new home with name {name}";
                    }
                    else
                    {
                        response = $"Home with name {name} already exists!";
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while adding home with name {name}!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("remove")]
            [Description("Remove a possible home name")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCHomesRemoveCommand(SlashCommandContext context, [Description("Name of home to remove")] string name)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Removing possible home with name {name}");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (info.homeChannels.Remove(name))
                    {
                        response = $"Removing home with name {name}";
                    }
                    else
                    {
                        response = $"Home with name {name} did not exist!";
                        info.Log(response);
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while removing home with name {name}!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("set")]
            [Description("Set possible home names")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCHomesSetCommand(SlashCommandContext context, [Description("List of homes to set (Separated by \", \")")] string names)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Setting possible home names");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    response = "# Homes:\r";
                    List<string> homeNamesList = [..names.Replace(" ", "").Split(',')];
                    for (int i = 0; i < homeNamesList.Count; i++)
                    {
                        response += $"- {homeNamesList[i]}\r";
                    }
                    info.homeChannels = homeNamesList;
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while setting home names!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
            [Command("default")]
            [Description("Reset home names to default")]
            [RequirePermissions(DiscordPermission.ManageChannels)]
            public static async Task BOTCHomesDefaultCommand(SlashCommandContext context, [Description("Are you sure?")] bool confirm = false)
            {
                ServerInfo info = DeBOTCBot.activeServers[context.Guild.Id];
                info.Log($"Setting possible home names to default");
                await context.DeferResponseAsync(true);
                string response;
                try
                {
                    if (confirm)
                    {
                        info.homeChannels = DeBOTCBot.DefaultHomeData();
                        response = "Set possible home names to default";
                    }
                    else
                    {
                        response = "Confirm that you'd like to reset server info by setting \"confirm\" to \"true\"";
                    }
                }
                catch (Exception exception)
                {
                    response = $"Something went wrong while setting home names to default!";
                    info.Log(exception);
                }
                try
                {
                    await context.RespondRegistered(info, response);
                }
                catch (Exception exception)
                {
                    info.Log(exception);
                }
            }
        }
    }
    public class DeBOTCBot
    {
        public const DiscordIntents Intents = DiscordIntents.Guilds | DiscordIntents.GuildMembers | DiscordIntents.GuildMessages | DiscordIntents.MessageContents | DiscordIntents.GuildVoiceStates;
        public static readonly Dictionary<ulong, ServerInfo> activeServers = [];
        public static async Task Main()
        {
            Console.WriteLine("deBOTCBot Started!");
            string token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("DISCORD_TOKEN was null!");
                Environment.Exit(1);
                return;
            }
            Serialization.ResetDebugLog();
            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(token, Intents);
            builder.ConfigureEventHandlers(b => b.HandleGuildMemberUpdated(RoleUpdated)
            .HandleGuildDownloadCompleted(BotReady)
            .HandleComponentInteractionCreated(ButtonPressed)
            .HandleMessageDeleted(MessageDeleted)
            .HandleChannelDeleted(ChannelDeleted)
            .HandleGuildRoleDeleted(RoleDeleted)
            .HandleGuildMemberRemoved(MemberLeft)
            .HandleGuildDeleted(LeftServer))
            .UseInteractivity(new InteractivityConfiguration() { ResponseBehavior = DSharpPlus.Interactivity.Enums.InteractionResponseBehavior.Ack, Timeout = TimeSpan.FromSeconds(30) })
            .UseCommands((IServiceProvider serviceProvider, CommandsExtension extension) => { extension.AddCommand(typeof(BOTCCommands), 1396921797160472576); extension.AddCommand(typeof(AppCommands), 1396921797160472576); })
            .DisableDefaultLogging();
            DiscordClient client = builder.Build();
            await client.ConnectAsync(new DiscordActivity("Blood on The Clocktower", DiscordActivityType.Playing), DiscordUserStatus.Online);
            await Task.Delay(-1);
        }
        public static async Task SaveServerInfo(ServerInfo info)
        {
            info.Log("Saving server info");
            ServerSaveInfo saveInfo = new(info);
            await Task.Run(() => Serialization.WriteToFile($"{Serialization.infoFilePath}\\{info.server.Id}.json", saveInfo));
        }
        public static async Task ResetServerInfo(ServerInfo info)
        {
            info.Log("Resetting server info");
            ServerInfo newInfo = new(info.server);
            ServerSaveInfo saveInfo = new(newInfo);
            await newInfo.FillSavedValues(saveInfo);
            activeServers[info.server.Id] = newInfo;
            await Task.Run(() => Serialization.WriteToFile($"{Serialization.infoFilePath}\\{info.server.Id}.json", saveInfo));
        }
        public static async Task BotReady(DiscordClient client, GuildDownloadCompletedEventArgs args)
        {
            Console.WriteLine("Populating servers with data");
            List<ulong> serverIDs = [.. args.Guilds.Keys];
            for (int i = 0; i < serverIDs.Count; i++)
            {
                ulong id = serverIDs[i];
                DiscordGuild guild = args.Guilds[id];
                Console.WriteLine($"Found server: \"{guild.Name}\"");
                ServerInfo info = new(guild);
                ServerSaveInfo saveInfo = Serialization.ReadFromFile<ServerSaveInfo>($"{Serialization.infoFilePath}\\{id}.json");
                if (saveInfo != null)
                {
                    await info.FillSavedValues(saveInfo);
                }
                activeServers.Add(id, info);
            }
        }
        public static async Task RoleUpdated(DiscordClient client, GuildMemberUpdatedEventArgs args)
        {
            ServerInfo info = activeServers[args.Guild.Id];
            DiscordRole storytellerRole = await info.GetRole(info.storytellerRole);
            if (storytellerRole == null)
            {
                return;
            }
            DiscordMember member = args.Member;
            if (args.RolesAfter.Contains(storytellerRole))
            {
                
                if (info.storytellerControls != null)
                {
                    await info.DeleteMessage(info.storytellerControls.controls);
                    info.storytellerControls = null;
                }
                info.currentStoryteller = member.Id;
                DiscordChannel storytellerChannel = await info.GetChannel(info.storytellerChannel);
                if (storytellerChannel != null)
                {
                    info.storytellerControls = new(await info.NewMessage(storytellerChannel, await UpdateControls(info, false)));
                    info.Log($"User: {member.DisplayName} is the current Storyteller");
                }
            }
            else if (info.currentStoryteller == member.Id && info.storytellerControls != null)
            {
                info.Log("Deleting Storyteller Controls");
                DiscordMessage controls = await info.GetMessage(info.storytellerControls.controls);
                if (controls != null)
                {
                    await info.DeleteMessage(controls);
                }
                info.storytellerControls = null;
                await EndGame(info, [..info.playerDictionary.Keys]);
            }
        }
        public static async Task ButtonPressed(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            if (!args.Id.StartsWith("deB_BOTC"))
            {
                return;
            }
            ServerInfo info = activeServers[args.Guild.Id];
            List<ulong> currentPlayers = [];
            if (info.playerDictionary != null)
            {
                currentPlayers = [..info.playerDictionary.Keys];
            }
            info.Log($"Button Pressed with ID: \"{args.Id}\"");
            DiscordMember presserMember = await info.server.GetMemberAsync(args.User.Id);
            if (presserMember.Id == info.currentStoryteller)
            {
                switch (args.Id)
                {
                    case "deB_BOTCBellButton":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            if (info.gameStarted)
                            {
                                info.Log("Bell Ring Started");
                                DiscordChannel townChannel = await info.GetChannel(info.townChannel);
                                await info.NewMessage(await info.GetChannel(info.announcementsChannel), $"{Formatter.Mention(await info.GetRole(info.genericPlayerRole))} PLEASE MAKE YOUR WAY TO {Formatter.Mention(townChannel)}, YOU WILL BE MOVED IN 10 SECONDS---");
                                await Task.Delay(10000);
                                for (int i = 0; i < currentPlayers.Count; i++)
                                {
                                    DiscordMember member = await info.server.GetMemberAsync(currentPlayers[i]);
                                    if (member.VoiceState != null && member.VoiceState.ChannelId != info.townChannel)
                                    {
                                        info.Log($"User: {member.DisplayName} is being moved to the townhall...");
                                        await member.PlaceInAsync(townChannel);
                                    }
                                }
                                info.Log("Bell Ring Completed");
                            }
                            break;
                        }
                    case "deB_BOTCHomeButton":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            if (info.gameStarted)
                            {
                                info.Log("Home Time Started");
                                for (int i = 0; i < currentPlayers.Count; i++)
                                {
                                    DiscordChannel home = await info.GetChannel(info.playerDictionary[currentPlayers[i]]);
                                    DiscordMember member = await info.server.GetMemberAsync(currentPlayers[i]);
                                    if (member.VoiceState != null && member.VoiceState.ChannelId != home.Id)
                                    {
                                        info.Log($"User: {member.DisplayName} is being moved to {home.Name}...");
                                        await member.PlaceInAsync(home);
                                    }
                                }
                                info.Log("Home Time Completed!");
                            }
                            break;
                        }
                    case "deB_BOTCUserSelect":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            if (!info.gameStarted)
                            {
                                info.Log($"Users Selected: \"{args.Interaction.Data.Resolved.Users.Count}\"");
                                await info.EditMessage(args.Message, new DiscordMessageBuilder(await UpdateControls(info, true, true)));
                                await StartGame(info, new(args.Interaction.Data.Resolved.Users.Keys));
                                info.gameStarted = true;
                            }
                            break;
                        }
                    case "deB_BOTCEndButton":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate, new(info.controlsMessageBuilder));
                            if (info.gameStarted)
                            {
                                await EndGame(info, currentPlayers);
                            }
                            break;
                        }
                    case "deB_BOTCScriptSelect":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new(info.controlsInGameMessageBuilder));
                            string script = args.Values.Single();
                            string tokensString;
                            string orderString = string.Empty;
                            bool split = false;
                            int halfway = -1;
                            for (int i = 0; i < info.storytellerControls.scriptMessages.Count; i++)
                            {
                                await info.DeleteMessage(info.storytellerControls.scriptMessages[i]);
                                info.storytellerControls.scriptMessages.RemoveAt(i);
                                i--;
                            }
                            try
                            {
                                string[] scriptTokens = info.botcGame.scripts[script];
                                tokensString = GenerateTokenMessage(info, scriptTokens, currentPlayers.Count);
                                orderString = $"\r{GenerateNightOrderMessage(scriptTokens, out split, out halfway)}";
                            }
                            catch (Exception exception)
                            {
                                tokensString = "Something went wrong while generating a grimoire! Are there enough of each token type for this number of players?";
                                info.Log(exception);
                            }
                            ulong message = await info.NewMessage(info.storytellerChannel, tokensString);
                            info.storytellerControls.scriptMessages.Add(message);
                            if (orderString != string.Empty)
                            {
                                if (split && halfway > -1)
                                {
                                    info.storytellerControls.scriptMessages.Add(await info.NewMessage(info.storytellerChannel, new DiscordMessageBuilder().WithContent(orderString[..halfway]).WithReply(message)));
                                    info.storytellerControls.scriptMessages.Add(await info.NewMessage(info.storytellerChannel, new DiscordMessageBuilder().WithContent(orderString[halfway..]).WithReply(message)));
                                }
                                else
                                {
                                    info.storytellerControls.scriptMessages.Add(await info.NewMessage(info.storytellerChannel, new DiscordMessageBuilder().WithContent(orderString).WithReply(message)));
                                }
                            }
                            break;
                        }
                    case "deB_BOTCNominateButton":
                        {
                            List<DiscordMember> players = [];
                            for (int i = 0; i < currentPlayers.Count; i++)
                            {
                                players.Add(await info.GetMember(currentPlayers[i]));
                            }
                            players.Add(await info.GetMember(info.currentStoryteller));
                            IEnumerable<DiscordSelectComponentOption> nominatables = players.Select((x) => new DiscordSelectComponentOption(x.DisplayName, x.Id.ToString()));
                            DiscordSelectComponent nominatorSelect = new("deB_BOTCNominatorSelect", "Nominator", nominatables, false);
                            DiscordSelectComponent nomineeSelect = new("deB_BOTCNomineeSelect", "Nominee", nominatables, false);
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Pick a player to nominate another player!").AddActionRowComponent(nominatorSelect).AddActionRowComponent(nomineeSelect).AsEphemeral());
                            break;
                        }
                    case "deB_BOTCNominatorSelect":
                        {
                            bool nomMade = false;
                            ulong selectedID = ulong.Parse(args.Values.Single());
                            Player player = info.botcGame.playerSeats.Where((x) => x.memberID == selectedID).Single();
                            if (info.botcGame.currentNomination != null)
                            {
                                info.botcGame.currentNomination.nominator = player;
                                if (info.botcGame.currentNomination.nominee != null)
                                {
                                    info.Log($"Nomination made from \"{player.name}\" to \"{info.botcGame.currentNomination.nominee.name}\"");
                                    await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, "deB_BOTCBeginVote", "Begin Vote")));
                                    nomMade = true;
                                }
                            }
                            else
                            {
                                info.botcGame.currentNomination = new(player, true);
                            }
                            if (!nomMade)
                            {
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            }
                            else
                            {
                            info.voteMessage = await info.NewMessage(info.townChannel, await UpdateVoteMessage(info, initial: true));
                        }
                            break;
                        }
                    case "deB_BOTCNomineeSelect":
                        {
                            bool nomMade = false;
                            ulong selectedID = ulong.Parse(args.Values.Single());
                            Player player = info.botcGame.playerSeats.Where((x) => x.memberID == selectedID).Single();
                            if (info.botcGame.currentNomination != null)
                            {
                                info.botcGame.currentNomination.nominee = player;
                                if (info.botcGame.currentNomination.nominator != null)
                                {
                                    info.Log($"Nomination made from \"{info.botcGame.currentNomination.nominator.name}\" to \"{player.name}\"");
                                    await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("").AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Primary, "deB_BOTCBeginVote", "Begin Vote")));
                                    nomMade = true;
                                }
                            }
                            else
                            {
                                info.botcGame.currentNomination = new(player, false);
                            }
                            if (!nomMade)
                            {
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            }
                            else
                            {
                                info.voteMessage = await info.NewMessage(info.townChannel, await UpdateVoteMessage(info, initial: true));
                            }
                            break;
                        }
                    case "deB_BOTCBeginVote":
                        {
                            try
                            {
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Vote Began").AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, "deB_BOTCCancelVote", "Cancel Vote")));
                                DiscordMessage voteMessage = await info.GetMessage(info.voteMessage);
                                await info.EditMessage(voteMessage, await UpdateVoteMessage(info));
                                info.voteCancelToken = new();
                                int votes = -1;
                                List<Player> toReverse = [];
                                votes = await BeginVote(info, info.voteCancelToken.Token);
                                if (!info.voteCancelToken.IsCancellationRequested)
                                {
                                    info.voteCancelToken.Cancel();
                                }
                                string result = "Vote Unsuccessful";
                                if (votes >= 0)
                                {
                                    result = $"Votes: {votes}";
                                }
                                await info.EditMessage(voteMessage, result);
                                info.botcGame.currentNomination = null;
                                for (int i = 0; i < info.botcGame.playerSeats.Count; i++)
                                {
                                    Player player = info.botcGame.playerSeats[i];
                                    player.handRaised = false;
                                    player.secondHandRaised = false;
                                }
                            }
                            catch (Exception exception)
                            {
                                info.Log(exception);
                            }
                            break;
                        }
                    case "deB_BOTCCancelVote":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Vote Cancelled"));
                            try
                            {
                                info.voteCancelToken.Cancel();
                            }
                            catch (Exception exception)
                            {
                                info.Log(exception);
                            }
                            break;
                        }
                    case "deB_BOTCLifeToggle":
                        {
                            IEnumerable<ulong> chosenIDs = args.Interaction.Data.Resolved.Users.Keys;
                            Player chosenPlayer = info.botcGame.playerSeats.Where((x) => chosenIDs.Contains(x.memberID)).SingleOrDefault();
                            if (info.gameStarted && chosenPlayer != null)
                            {
                                string response = $"{chosenPlayer.name} is now ";
                                chosenPlayer.dead = !chosenPlayer.dead;
                                if (chosenPlayer.dead)
                                {
                                    response += "dead!";
                                }
                                else
                                {
                                    response += "alive!";
                                }
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(response).AsEphemeral());
                            }
                            else
                            {
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            }
                            break;
                        }
                    case "deB_BOTCBansheeToggle":
                        {
                            IEnumerable<ulong> chosenIDs = args.Interaction.Data.Resolved.Users.Keys;
                            Player chosenPlayer = info.botcGame.playerSeats.Where((x) => chosenIDs.Contains(x.memberID)).SingleOrDefault();
                            if (info.gameStarted && chosenPlayer != null)
                            {
                                string response = $"{chosenPlayer.name} now has ";
                                chosenPlayer.banshee = !chosenPlayer.banshee;
                                if (chosenPlayer.banshee)
                                {
                                    response += "the activated banshee ability!";
                                }
                                else
                                {
                                    response += "no banshee ability!";
                                }
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(response).AsEphemeral());
                            }
                            else
                            {
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            }
                            break;
                        }
                    default:
                        {
                            info.Log("Invalid button press!");
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            break;
                        }
                }
            }
            else if (args.Id.StartsWith("deB_BOTCPlayerVote"))
            {
                int buttonID = int.Parse(args.Id["deB_BOTCPlayerVote".Length..]);
                Player pressingPlayer = info.botcGame.playerSeats.Where((x) => x.memberID == presserMember.Id).SingleOrDefault();
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                if ((pressingPlayer != null && buttonID == pressingPlayer.seat) || /*temp*/presserMember.Id == info.currentStoryteller)
                {
                    await info.EditMessage(args.Message, await UpdateVoteMessage(info, buttonID, info.botcGame.playerSeats.Where((x) => x.buttonDisabled).Select((x) => x.seat)));
                }
            }
            else
            {
                info.Log($"Button Pressed by non-storyteller!: \"{args.Id}\"");
            }
            if (args.Interaction.ResponseState == DiscordInteractionResponseState.Replied)
            {
                info.RegisterMessage(await args.Interaction.GetOriginalResponseAsync());
            }
            else if (args.Interaction.ResponseState == DiscordInteractionResponseState.Unacknowledged)
            {
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
            }
        }
        public static async Task LeftServer(DiscordClient client, GuildDeletedEventArgs args)
        {
            ulong id = args.Guild.Id;
            ServerInfo info = activeServers[id];
            await SaveServerInfo(info);
            activeServers.Remove(id);
        }
        public static async Task MessageDeleted(DiscordClient client, MessageDeletedEventArgs args)
        {
            ServerInfo info = activeServers[args.Guild.Id];
            ulong id = args.Message.Id;
            info.messages.Remove(id);
            if (info.storytellerControls.controls == id)
            {
                await EndGame(info, [..info.playerDictionary.Keys]);
            }
        }
        public static async Task ChannelDeleted(DiscordClient client, ChannelDeletedEventArgs args)
        {
            ServerInfo info = activeServers[args.Guild.Id];
            if (info.channels.Remove(args.Channel.Id))
            {
                await EndGame(info, [..info.playerDictionary.Keys]);
            }
        }
        public static async Task RoleDeleted(DiscordClient client, GuildRoleDeletedEventArgs args)
        {
            ServerInfo info = activeServers[args.Guild.Id];
            if (info.roles.Remove(args.Role.Id))
            {
                await EndGame(info, [..info.playerDictionary.Keys]);
            }
        }
        public static async Task MemberLeft(DiscordClient client, GuildMemberRemovedEventArgs args)
        {
            ServerInfo info = activeServers[args.Guild.Id];
            if (info.members.Remove(args.Member.Id))
            {
                await EndGame(info, [..info.playerDictionary.Keys]);
            }
        }
        public static async Task<DiscordMessageBuilder> UpdateControls(ServerInfo info, bool ingame, bool initial = false)
        {
            DiscordMessageBuilder builder;
            if (ingame)
            {
                DiscordUserSelectComponent userSelect = new("deB_BOTCUserSelect", "Players", false, 1, 15);
                builder = new DiscordMessageBuilder().WithContent($"{Formatter.Mention(await info.GetMember(info.currentStoryteller))}, select your players!").AddActionRowComponent(userSelect);
            }
            else
            {
                List<DiscordSelectComponentOption> options = [];
                Dictionary<string, string[]> scriptDict = info.botcGame.scripts;
                string[] scripts = [.. scriptDict.Keys];
                for (int i = 0; i < scripts.Length; i++)
                {
                    string label = scripts[i];
                    options.Add(new(label, label));
                }
                DiscordSelectComponent scriptSelect = new("deB_BOTCScriptSelect", "Script", options, false);
                DiscordButtonComponent bellButton = new(DiscordButtonStyle.Primary, "deB_BOTCBellButton", "Ring the Bell");
                DiscordButtonComponent homeButton = new(DiscordButtonStyle.Primary, "deB_BOTCHomeButton", "Home Time");
                DiscordButtonComponent nominateButton = new(DiscordButtonStyle.Primary, "deB_BOTCNominateButton", "Nomination");
                DiscordButtonComponent endButton = new(DiscordButtonStyle.Danger, "deB_BOTCEndButton", "End Game");
                DiscordUserSelectComponent lifeToggleSelect = new("deB_BOTCLifeToggle", "Player", false, 1, 1);
                DiscordUserSelectComponent bansheeToggleSelect = new("deB_BOTCBansheeToggle", "Banshee Toggle", false, 1, 1);
                if (initial)
                {
                    scriptSelect.Disable();
                    bellButton.Disable();
                    homeButton.Disable();
                    nominateButton.Disable();
                    endButton.Disable();
                    lifeToggleSelect.Disable();
                    bansheeToggleSelect.Disable();
                }
                builder = new DiscordMessageBuilder().WithContent($"{Formatter.Mention(await info.GetMember(info.currentStoryteller))}, this is the Storyteller's Interface!").AddActionRowComponent(scriptSelect).AddActionRowComponent(bellButton, homeButton, nominateButton, endButton).AddActionRowComponent(lifeToggleSelect).AddActionRowComponent(bansheeToggleSelect);
            }
            return builder;
        }
        public static async Task<DiscordMessageBuilder> UpdateVoteMessage(ServerInfo info, int votesChanged = -1, IEnumerable<int> disabled = null, bool initial = false)
        {
            disabled ??= [];
            List<DiscordButtonComponent> voteButtons = [];
            int playerCount = info.botcGame.playerSeats.Count;
            for (int i = 0; i < playerCount; i++)
            {
                Player player = info.botcGame.playerSeats[i];
                bool disable = disabled.Contains(i);
                info.Log($"Updating {player.name}'s button");
                string emoji = "👎";
                DiscordButtonStyle style = DiscordButtonStyle.Danger;
                if (votesChanged == i)
                {
                    info.Log($"Changing {player.name}'s vote");
                    if (!disable)
                    {
                        if (info.botcGame.playerSeats[votesChanged].banshee)
                        {
                            if (player.secondHandRaised)
                            {
                                player.handRaised = false;
                                player.secondHandRaised = false;
                            }
                            else if (player.handRaised)
                            {
                                player.secondHandRaised = true;
                            }
                            else
                            {
                                player.handRaised = true;
                            }
                            info.Log($"Banshee second hand raised: {player.secondHandRaised}");
                        }
                        else
                        {
                            player.handRaised = !player.handRaised;
                        }
                    }
                }
                if (player.handRaised)
                {
                    info.Log($"{player.name}'s hand is raised");
                    emoji = "🤚";
                    style = DiscordButtonStyle.Primary;
                    if (player.secondHandRaised)
                    {
                        emoji = "🙌";
                    }
                }
                if (disable)
                {
                    info.Log($"Locking {player.name}'s vote in");
                    style = DiscordButtonStyle.Secondary;
                }
                string buttonID = $"deB_BOTCPlayerVote{i}";
                DiscordButtonComponent newButton = new(style, buttonID, $"Seat {i}", emoji: new(emoji));
                if (disable || initial)
                {
                    info.Log($"Disabling {player.name}'s button");
                    newButton.Disable();
                }
                voteButtons.Add(newButton);
            }
            float rowNo = playerCount / 4f;
            float rowsLeft = rowNo;
            List<DiscordActionRowComponent> actionRows = [];
            for (int i = 0; i < (int)MathF.Ceiling(rowNo); i++)
            {
                int buttons = 4;
                if (rowsLeft < 1)
                {
                    buttons = (int)MathF.Round(rowsLeft * 4f);
                }
                List<DiscordButtonComponent> newButtons = [];
                for (int j = 0; j < buttons; j++)
                {
                    newButtons.Add(voteButtons[0]);
                    voteButtons.RemoveAt(0);
                }
                actionRows.Add(new(newButtons));
                rowsLeft -= 1;
            }
            DiscordMessageBuilder builder = new DiscordMessageBuilder().EnableV2Components().AddTextDisplayComponent($"{Formatter.Mention(await info.GetRole(info.genericPlayerRole))}, {info.botcGame.currentNomination.nominator.mentionString} is nominating {info.botcGame.currentNomination.nominee.mentionString}, cast your vote!");
            for (int i = 0; i < actionRows.Count; i++)
            {
                builder.AddActionRowComponent(actionRows[i]);
                if (i != actionRows.Count - 1)
                {
                    builder.AddSeparatorComponent(new(true, DiscordSeparatorSpacing.Small));
                }
            }
            return builder;
        }
        public static async Task NominationTimer(ServerInfo info, ulong id, string mention, CancellationToken cancel)
        {
            DiscordMessage message = await info.GetMessage(id);
            int per = int.Parse(message.Content.Replace($"{mention} - ", "").Replace("**", ""));
            int counter = per;
            int asyncDiff = 250;
            Stopwatch stopWatch = new();
            while (true)
            {
                await Task.Delay((int)MathF.Max(0, 1000 - asyncDiff), cancel);
                counter--;
                if (counter <= 0)
                {
                    break;
                }
                if (stopWatch.IsRunning)
                {
                    stopWatch.Restart();
                }
                else
                {
                    stopWatch.Start();
                }
                await info.EditMessage(id, $"**{mention} - {counter}**");
                stopWatch.Stop();
                asyncDiff = (int)stopWatch.Elapsed.TotalMilliseconds;
            }
            await info.DeleteMessage(id);
        }
        public static async Task<int> BeginVote(ServerInfo info, CancellationToken cancel)
        {
            int timePer = 4;
            List<Player> votesRemoved = [];
            int totalVotes = 0;
            List<int> disabled = [];
            try
            {
                int voter = info.botcGame.currentNomination.nominee.neighbours.Item2;
                for (int i = 0; i < info.botcGame.playerSeats.Count; i++)
                {
                    Player player = info.botcGame.playerSeats[i];
                    if (!player.dead || player.banshee)
                    {
                        player.hasVote = true;
                    }
                    if (!player.hasVote)
                    {
                        disabled.Add(i);
                    }
                }
                await Task.Delay(1000, cancel);
                for (int i = 0; i < info.botcGame.playerSeats.Count; i++)
                {
                    if (voter >= info.botcGame.playerSeats.Count)
                    {
                        voter = 0;
                    }
                    Player player = info.botcGame.playerSeats[voter];
                    await NominationTimer(info, await info.NewMessage(info.townChannel, $"**{player.mentionString} - {timePer}**"), player.mentionString, cancel);
                    disabled.Add(voter);
                    if (player.hasVote)
                    {
                        await info.EditMessage(info.voteMessage, await UpdateVoteMessage(info, voter, disabled));
                        if (player.handRaised)
                        {
                            info.Log($"{player.name} voted");
                            totalVotes++;
                            if (player.secondHandRaised)
                            {
                                info.Log($"{player.name}, as a Banshee, voted again");
                                totalVotes++;
                            }
                        }
                        if (player.dead && !player.banshee)
                        {
                            info.Log($"Removing dead player {player.name}'s vote");
                            player.hasVote = false;
                            votesRemoved.Add(player);
                        }
                    }
                    voter++;
                }
            }
            catch (Exception exception)
            {
                info.Log(exception);
                for (int i = 0; i < votesRemoved.Count; i++)
                {
                    votesRemoved[i].hasVote = true;
                }
                totalVotes = -1;
            }
            return totalVotes;
        }
        public static async Task StartGame(ServerInfo info, List<ulong> ids)
        {
            info.Log("Starting BOTC Game");
            info.playerDictionary = [];
            List<DiscordMember> currentPlayers = [];
            for (int i = 0; i < ids.Count; i++)
            {
                ulong id = ids[i];
                DiscordMember member = await info.server.GetMemberAsync(id);
                currentPlayers.Add(member);
                info.RegisterMember(member);
                info.botcGame.playerSeats.Add(new(id, member.DisplayName, Formatter.Mention(member)));
            }
            for (int i = 0; i < currentPlayers.Count - 1; ++i)
            {
                int newIndex = Random.Shared.Next(i, currentPlayers.Count);
                (currentPlayers[newIndex], currentPlayers[i]) = (currentPlayers[i], currentPlayers[newIndex]);
            }
            DiscordRole genRole = await info.GetRole(info.genericPlayerRole);
            List<string> channelNames = [..info.homeChannels];
            for (int i = 0; i < currentPlayers.Count; i++)
            {
                if (channelNames.Count == 0)
                {
                    channelNames.Add($"Home #{i + 1}");
                }
                int randomIndex = Random.Shared.Next(channelNames.Count);
                ulong newHome = await info.NewChannel(channelNames[randomIndex], DiscordChannelType.Voice, info.homesCategory, overwrites: [new DiscordOverwriteBuilder(currentPlayers[i]) { Allowed = DiscordPermission.ViewChannel }]);
                channelNames.RemoveAt(randomIndex);
                info.playerDictionary.Add(currentPlayers[i].Id, newHome);
                await info.GiveRole(currentPlayers[i], genRole);
            }
            await genRole.ModifyPositionAsync(0);
            DiscordChannel townChannel = await info.GetChannel(info.townChannel);
            await info.NewMessage(info.announcementsChannel, $"{Formatter.Mention(genRole)} PLEASE MAKE YOUR WAY TO {Formatter.Mention(townChannel)}---");
            List<ulong> childChannels = [];
            DiscordChannel homesCategory = await info.GetChannel(info.homesCategory);
            if (homesCategory != null)
            {
                childChannels.AddRange(homesCategory.Children.Select((x) => x.Id));
            }
            if (townChannel.Parent != null)
            {
                childChannels.AddRange(townChannel.Parent.Children.Select((x) => x.Id));
            }
            List<ulong> channels = [..childChannels, info.announcementsChannel, info.storytellerChannel];
            for (int i = 0; i < channels.Count; i++)
            {
                await info.NewMessage(channels[i], $"---STARTING GAME---");
            }
            if (info.storytellerControls != null)
            {
                await info.EditMessage(info.storytellerControls.controls, await UpdateControls(info, true));
            }
        }
        public static async Task EndGame(ServerInfo info, List<ulong> currentPlayers)
        {
            if (!info.gameStarted)
            {
                return;
            }
            info.Log("Ending BOTC Game");
            DiscordRole genRole = await info.GetRole(info.genericPlayerRole);
            for (int i = 0; i < currentPlayers.Count; i++)
            {
                DiscordMember member = await info.GetMember(currentPlayers[i]);
                await info.EditMember(member, delegate (MemberEditModel model) { model.VoiceChannel = null; });
                await info.TakeRole(member, genRole);
                await info.DeleteChannel(info.playerDictionary[currentPlayers[i]]);
                info.UnregisterMember(member);
            }
            currentPlayers.Clear();
            info.playerDictionary.Clear();
            info.botcGame.playerSeats.Clear();
            info.botcGame.currentNomination = null;
            DiscordMember storyteller = await info.GetMember(info.currentStoryteller);
            DiscordRole storytellerRole = await info.GetRole(info.storytellerRole);
            await info.TakeRole(storyteller, storytellerRole);
            await info.UnregisterMember(info.currentStoryteller);
            info.currentStoryteller = 0;
            info.gameStarted = false;
            List<ulong> townChannels = [];
            DiscordChannel townChannel = await info.GetChannel(info.townChannel);
            if (townChannel != null && townChannel.Parent != null)
            {
                townChannels.AddRange(townChannel.Parent.Children.Select((x) => x.Id));
            }
            List<ulong> channels = [info.announcementsChannel, info.storytellerChannel, ..townChannels];
            for (int i = 0; i < channels.Count; i++)
            {
                await info.NewMessage(channels[i], $"---END OF GAME---");
            }
        }
        public static Dictionary<string, int> DefaultTownData()
        {
            Dictionary<string, int> finalDict = [];
            finalDict.Add("Backalley", 2);
            finalDict.Add("Doctor's Office", 2);
            finalDict.Add("Recording Studio", 2);
            finalDict.Add("Public Garden", 3);
            finalDict.Add("Local Park", 3);
            finalDict.Add("Bathhouse", 4);
            finalDict.Add("Tavern", 5);
            return finalDict;
        }
        public static List<string> DefaultHomeData()
        {
            return ["Humble Abode", "Grand Mansion", "Utility Shack", "Cottage", "Hobbit Hole", "Treehouse", "Large Shoe", "Burrow", "Caravan", "Lighthouse", "Small Castle", "Dovecoat", "Jester's Quarters", "Wizard Tower", "Dungeon", "Docked Boat", "Campsite", "Local Inn", "Bungalow", "Terrace"];
        }
        public static bool AreSimilar(params string[] strings)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                string currentString = strings[i];
                currentString = currentString.Replace("'", "").Replace("&", "and").Replace(" ", "").ToLower();
                strings[i] = currentString;
            }
            return strings.Distinct().Count() == 1;
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
            if (info.botcGame.playerSeats.Count == 0)
            {
                string[] tokenNames = [..tokens.Select((x) => x.characterName)];
                for (int i = 0; i < tokenNames.Length; i++)
                {
                    finalString += $"**{BOTCCharacters.allTokens[tokenNames[i]].characterType}**: {tokenNames[i]}\r";
                }
            }
            else
            {
                for (int i = 0; i < info.botcGame.playerSeats.Count; i++)
                {
                    Player player = info.botcGame.playerSeats[i];
                    finalString += $"**Seat {i}, {player.name} | {player.token.characterType}**: {player.token.characterName}\r";
                }
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
            List<Token> scriptTokens = [..BOTCCharacters.allTokens.Values.Where((x) => scriptNames.Contains(x.characterName) && x.nightOrder != (-1, -1))];
            scriptTokens.AddRange([
                new("Minion Info", firstOrder: 8, firstDesc: "If the game has 7 or more players, Minions learn who other Minions are and who Demon is"),
                new("Demon Info", firstOrder: 12, firstDesc: "If the game has 7 or more players, Demon learns who the Minions are and learns 3 not-in-play characters from the script")
                ]);
            scriptTokens.Sort((x, y) => x.nightOrder.Item1.CompareTo(y.nightOrder.Item1));
            int tokenNo = 1;
            for (int i = 0; i < scriptTokens.Count; i++)
            {
                Token token = scriptTokens[i];
                if (token.nightOrder.Item1 == -1)
                {
                    continue;
                }
                finalString += $"**{tokenNo} - {token.characterName}**: {token.orderFirstDescription}\r";
                tokenNo++;
            }
            half = finalString.Length;
            finalString += "\r## Other Nights Order:\r";
            scriptTokens.Sort((x, y) => x.nightOrder.Item2.CompareTo(y.nightOrder.Item2));
            tokenNo = 1;
            for (int i = 0; i < scriptTokens.Count; i++)
            {
                Token token = scriptTokens[i];
                if (token.nightOrder.Item2 == -1)
                {
                    continue;
                }
                finalString += $"**{tokenNo} - {token.characterName}**: {token.orderOtherDescription}\r";
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