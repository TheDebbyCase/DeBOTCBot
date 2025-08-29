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
namespace DeBOTCBot
{
    public class ServerSaveInfo
    {
        public ServerSaveInfo() { }
        public ServerSaveInfo(ServerInfo info)
        {
            if (info.storytellerRole != null)
            {
                storytellerRoleID = info.storytellerRole.Id;
            }
            if (info.genericPlayerRole != null)
            {
                genericPlayerRoleID = info.genericPlayerRole.Id;
            }
            if (info.announcementsChannel != null)
            {
                announcementsChannelID = info.announcementsChannel.Id;
            }
            if (info.storytellerChannel != null)
            {
                storytellerChannelID = info.storytellerChannel.Id;
            }
            if (info.townChannel != null)
            {
                townChannelID = info.townChannel.Id;
            }
            if (info.homesCategory != null)
            {
                homesCategoryID = info.homesCategory.Id;
            }
            if (info.botcGame != null && info.botcGame.scripts != null)
            {
                botcScripts = info.botcGame.scripts;
            }
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
    public class InGameControls(DiscordMessage newControls)
    {
        public DiscordMessage controls = newControls;
        public List<DiscordMessage> scriptMessages = [];
    }
    public class ServerInfo(DiscordGuild guild)
    {
        public bool hasInfo = false;
        public DiscordGuild server = guild;
        public DiscordRole storytellerRole;
        public DiscordRole genericPlayerRole;
        public DiscordChannel announcementsChannel;
        public DiscordChannel storytellerChannel;
        public DiscordChannel townChannel;
        public Dictionary<string, int> townChannels = [];
        public List<string> homeChannels;
        public DiscordChannel homesCategory;
        public DiscordMember currentStoryteller;
        public DiscordMessageBuilder controlsMessageBuilder;
        public DiscordMessageBuilder controlsInGameMessageBuilder;
        public InGameControls storytellerControls;
        public Dictionary<ulong, DiscordChannel> playerDictionary;
        public BOTCCharacters botcGame = new();
        public bool gameStarted;
        public async Task Initialize()
        {
            await Destroy(true);
            DiscordPermissions storytellerPerms = new(DiscordPermission.ViewChannel, DiscordPermission.SendMessages, DiscordPermission.Connect, DiscordPermission.Speak, DiscordPermission.MuteMembers, DiscordPermission.DeafenMembers, DiscordPermission.MoveMembers, DiscordPermission.PrioritySpeaker);
            Log("Creating Storyteller role");
            storytellerRole = await server.CreateRoleAsync("Storyteller", color: DiscordColor.Goldenrod, hoist: true, mentionable: true);
            Log("Creating BOTC Player role");
            genericPlayerRole = await server.CreateRoleAsync("BOTC Player");
            Log("Creating Storyteller channels");
            DiscordChannel storytellerCategory = await server.CreateChannelAsync("Storyteller's Corner", DiscordChannelType.Category, overwrites: [new DiscordOverwriteBuilder(storytellerRole) { Allowed = storytellerPerms }]);
            announcementsChannel = await server.CreateChannelAsync("botc-announcements", DiscordChannelType.Text, storytellerCategory, overwrites: [new DiscordOverwriteBuilder(server.EveryoneRole) { Allowed = DiscordPermission.ViewChannel, Denied = DiscordPermission.SendMessages }]);
            await server.CreateChannelAsync("Watchtower", DiscordChannelType.Voice, storytellerCategory, overwrites: [new DiscordOverwriteBuilder(genericPlayerRole) { Denied = new DiscordPermissions(DiscordPermission.ViewChannel, DiscordPermission.Connect) }]);
            storytellerChannel = await server.CreateChannelAsync("Storyteller's Crypt", DiscordChannelType.Voice, storytellerCategory, overwrites: [new DiscordOverwriteBuilder(storytellerRole) { Allowed = storytellerPerms }, new DiscordOverwriteBuilder(genericPlayerRole) { Denied = new DiscordPermissions(DiscordPermission.ViewChannel, DiscordPermission.Connect) }, new DiscordOverwriteBuilder(server.EveryoneRole) { Denied = new DiscordPermissions(DiscordPermission.Connect, DiscordPermission.SendMessages) }]);
            Log("Creating Town channels");
            DiscordChannel townCategory = await server.CreateChannelAsync("Town", DiscordChannelType.Category, overwrites: [new DiscordOverwriteBuilder(storytellerRole) { Allowed = storytellerPerms }, new DiscordOverwriteBuilder(genericPlayerRole) { Allowed = new DiscordPermissions(DiscordPermission.ViewChannel, DiscordPermission.Connect) }, new DiscordOverwriteBuilder(server.EveryoneRole) { Allowed = DiscordPermission.ViewChannel, Denied = new DiscordPermissions(DiscordPermission.Connect, DiscordPermission.SendMessages) }]);
            townChannel = await server.CreateChannelAsync("Town Square", DiscordChannelType.Voice, townCategory, userLimit: 0);
            if (townChannels == null || (townChannels != null && townChannels.Count == 0))
            {
                townChannels = DeBOTCBot.DefaultTownData();
            }
            foreach (KeyValuePair<string, int> pair in townChannels)
            {
                await server.CreateChannelAsync(pair.Key, DiscordChannelType.Voice, townCategory, userLimit: pair.Value);
            }
            Log("Creating Homes category");
            homesCategory = await server.CreateChannelAsync("Homes", DiscordChannelType.Category, overwrites: [new DiscordOverwriteBuilder(storytellerRole) { Allowed = storytellerPerms }, new DiscordOverwriteBuilder(genericPlayerRole) { Allowed = new DiscordPermissions(DiscordPermission.SendMessages, DiscordPermission.Connect), Denied = new DiscordPermissions(DiscordPermission.ViewChannel) }, new DiscordOverwriteBuilder(server.EveryoneRole) { Allowed = DiscordPermission.ViewChannel, Denied = new DiscordPermissions(DiscordPermission.Connect, DiscordPermission.SendMessages) }]);
            await DeBOTCBot.SaveServerInfo(DeBOTCBot.activeServers[server.Id]);
            hasInfo = true;
        }
        public async Task FillSavedValues(ServerSaveInfo savedInfo)
        {
            if (server.Roles.ContainsKey(savedInfo.storytellerRoleID))
            {
                storytellerRole = await server.GetRoleAsync(savedInfo.storytellerRoleID);
            }
            else
            {
                storytellerRole = default;
                Log($"No storyteller role was found!");
            }
            if (server.Roles.ContainsKey(savedInfo.genericPlayerRoleID))
            {
                genericPlayerRole = await server.GetRoleAsync(savedInfo.genericPlayerRoleID);
            }
            else
            {
                genericPlayerRole = default;
                Log($"No generic player role was found!");
            }
            if (server.Channels.ContainsKey(savedInfo.announcementsChannelID))
            {
                announcementsChannel = await server.GetChannelAsync(savedInfo.announcementsChannelID);
            }
            else
            {
                announcementsChannel = default;
                Log($"No announcements channel was found!");
            }
            if (server.Channels.ContainsKey(savedInfo.storytellerChannelID))
            {
                storytellerChannel = await server.GetChannelAsync(savedInfo.storytellerChannelID);
            }
            else
            {
                storytellerChannel = default;
                Log($"No storyteller channel was found!");
            }
            if (server.Channels.ContainsKey(savedInfo.townChannelID))
            {
                townChannel = await server.GetChannelAsync(savedInfo.townChannelID);
            }
            else
            {
                townChannel = default;
                Log($"No town channel was found!");
            }
            if (server.Channels.ContainsKey(savedInfo.homesCategoryID))
            {
                homesCategory = await server.GetChannelAsync(savedInfo.homesCategoryID);
            }
            else
            {
                homesCategory = default;
                Log($"No homes category was found!");
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
            hasInfo = storytellerRole != null || genericPlayerRole != null || announcementsChannel != null || storytellerChannel != null || townChannel != null || homesCategory != null;
        }
        public async Task Destroy(bool refresh = false)
        {
            Log("Destroying BOTC channels");
            gameStarted = false;
            playerDictionary?.Clear();
            currentStoryteller = null;
            storytellerControls = null;
            controlsMessageBuilder = null;
            controlsInGameMessageBuilder = null;
            DiscordChannel storytellerCategory = storytellerChannel?.Parent;
            storytellerChannel = null;
            DiscordChannel townCategory = townChannel?.Parent;
            townChannel = null;
            List<DiscordChannel> childChannels = [];
            if (storytellerCategory != null)
            {
                childChannels.AddRange([..storytellerCategory.Children]);
            }
            if (townCategory != null)
            {
                childChannels.AddRange([..townCategory.Children]);
            }
            if (homesCategory != null)
            {
                childChannels.AddRange([..homesCategory.Children]);
            }
            for (int i = 0; i < childChannels.Count; i++)
            {
                DiscordChannel channel = childChannels[i];
                if (channel != null)
                {
                    await channel.DeleteAsync();
                }
            }
            if (homesCategory != null)
            {
                await homesCategory.DeleteAsync();
                homesCategory = null;
            }
            if (townCategory != null)
            {
                await townCategory.DeleteAsync();
            }
            if (storytellerCategory != null)
            {
                await storytellerCategory.DeleteAsync();
            }
            Log("Destroying BOTC roles");
            if (genericPlayerRole != null)
            {
                await genericPlayerRole.DeleteAsync();
                genericPlayerRole = null;
            }
            if (storytellerRole != null)
            {
                await storytellerRole.DeleteAsync();
                storytellerRole = null;
            }
            if (!refresh)
            {
                await DeBOTCBot.SaveServerInfo(this);
            }
            hasInfo = false;
        }
        public void Log(string contents, ConsoleColor overrideColour = ConsoleColor.Gray, bool isError = false)
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
            Serialization.WriteLog(server.Name, contents, isError);
        }
        public void Log(Exception exception)
        {
            Log($"{exception.Message}\n{exception.StackTrace}", ConsoleColor.Red, true);
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
                await context.EditResponseAsync(response);
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
                if (!info.hasInfo)
                {
                    response = "BOTC environment does not exist!";
                }
                else if (info.currentStoryteller != member)
                {
                    response = string.Empty;
                    if (info.currentStoryteller != null)
                    {
                        response += $"\"{info.currentStoryteller.DisplayName}\" is no longer the storyteller,\r";
                        info.Log("Deleting Storyteller Controls");
                        await info.storytellerControls.controls.DeleteAsync();
                        info.storytellerControls = null;
                    }
                    response += $"\"{member.DisplayName}\" is the new storyteller";
                    await member.GrantRoleAsync(info.storytellerRole);
                    info.currentStoryteller = member;
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
                await context.EditResponseAsync(response);
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
                await context.EditResponseAsync(response);
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
                await context.EditResponseAsync(response);
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
                await context.EditResponseAsync(response);
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
                await context.EditResponseAsync(response);
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
                        await context.EditResponseAsync(response[..halfMessage]);
                        await context.FollowupAsync(response[halfMessage..], true);
                    }
                    else
                    {
                        await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
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
                        if (info.storytellerControls != null && info.storytellerControls.controls != null && info.gameStarted)
                        {
                            DeBOTCBot.UpdateControls(info);
                            await info.storytellerControls.controls.ModifyAsync(info.controlsInGameMessageBuilder);
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
                        await context.EditResponseAsync(response[..halfway]);
                        await context.FollowupAsync(response[halfway..], true);
                    }
                    else
                    {
                        await context.EditResponseAsync(response);
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
                        await context.EditResponseAsync(response[..halfway]);
                        await context.FollowupAsync(response[halfway..], true);
                    }
                    else
                    {
                        await context.EditResponseAsync(response);
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
                        if (info.storytellerControls != null && info.storytellerControls.controls != null && info.gameStarted)
                        {
                            DeBOTCBot.UpdateControls(info);
                            await info.storytellerControls.controls.ModifyAsync(info.controlsInGameMessageBuilder);
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
                    await context.EditResponseAsync(response);
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
                        await context.EditResponseAsync(response[..halfway]);
                        await context.FollowupAsync(response[halfway..]);
                    }
                    else
                    {
                        await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
                    if (responseNext != string.Empty)
                    {
                        if (split && halfway > -1)
                        {
                            await context.FollowupAsync(responseNext[..halfway], true);
                            await context.FollowupAsync(responseNext[halfway..], true);
                        }
                        else
                        {
                            await context.FollowupAsync(responseNext, true);
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
                    await context.EditResponseAsync(response);
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
                        if (info.townChannel != null)
                        {
                            await info.server.CreateChannelAsync(channel, DiscordChannelType.Voice, info.townChannel.Parent, userLimit: limitClamped);
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
                    await context.EditResponseAsync(response);
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
                        if (info.townChannel != null)
                        {
                            DiscordChannel[] channels = [.. info.townChannel.Parent.Children.Where((x) => x.Name == channel)];
                            if (channels != null && channels.Length > 0)
                            {
                                await channels[0].DeleteAsync();
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
                    await context.EditResponseAsync(response);
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
                        if (info.townChannel != null)
                        {
                            DiscordChannel[] channels = [..info.townChannel.Parent.Children.Where((x) => x.Name == channel)];
                            if (channels != null && channels.Length > 0)
                            {
                                await channels[0].ModifyAsync(delegate (ChannelEditModel model) { model.Name = newName; model.Userlimit = limitClamped; });
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
                    await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
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
                            if (info.townChannel != null)
                            {
                                List<DiscordChannel> channels = [.. info.townChannel.Parent.Children];
                                for (int i = 0; i < channels.Count; i++)
                                {
                                    await channels[i].DeleteAsync();
                                }
                                foreach (KeyValuePair<string, int> pair in info.townChannels)
                                {
                                    await context.Guild.CreateChannelAsync(pair.Key, DiscordChannelType.Voice, info.townChannel.Parent, userLimit: pair.Value);
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
                    await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
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
                    await context.EditResponseAsync(response);
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
            builder.ConfigureEventHandlers(b => b.HandleGuildMemberUpdated(RoleUpdated).HandleGuildDownloadCompleted(BotReady).HandleComponentInteractionCreated(ButtonPressed)).UseInteractivity(new InteractivityConfiguration() { ResponseBehavior = DSharpPlus.Interactivity.Enums.InteractionResponseBehavior.Ack, Timeout = TimeSpan.FromSeconds(30) }).UseCommands((IServiceProvider serviceProvider, CommandsExtension extension) => { extension.AddCommand(typeof(BOTCCommands), 1396921797160472576); });
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
            foreach (KeyValuePair<ulong, DiscordGuild> pair in args.Guilds)
            {
                ulong serverID = pair.Key;
                DiscordGuild guild = pair.Value;
                Console.WriteLine($"Found server: \"{guild.Name}\"");
                ServerInfo info = new(guild);
                ServerSaveInfo saveInfo = Serialization.ReadFromFile<ServerSaveInfo>($"{Serialization.infoFilePath}\\{serverID}.json");
                if (saveInfo != null)
                {
                    await info.FillSavedValues(saveInfo);
                }
                activeServers.Add(serverID, info);
            }
        }
        public static async Task RoleUpdated(DiscordClient client, GuildMemberUpdatedEventArgs args)
        {
            ServerInfo info = activeServers[args.Guild.Id];
            if (info.storytellerRole == null)
            {
                return;
            }
            if (args.RolesAfter.Contains(info.storytellerRole))
            {
                if (info.storytellerControls != null && info.storytellerControls.controls != null)
                {
                    await info.storytellerControls.controls.DeleteAsync();
                    info.storytellerControls = null;
                }
                info.currentStoryteller = args.Member;
                UpdateControls(info);
                info.storytellerControls = new(await client.SendMessageAsync(info.storytellerChannel, info.controlsMessageBuilder));
                info.Log($"User: {info.currentStoryteller.DisplayName} is the current Storyteller");
            }
            else if (info.currentStoryteller == args.Member && info.storytellerControls != null && info.storytellerControls.controls != null)
            {
                info.Log("Deleting Storyteller Controls");
                await info.storytellerControls.controls.DeleteAsync();
                info.storytellerControls = null;
                if (info.gameStarted)
                {
                    await EndGame(info, client, [..info.playerDictionary.Keys]);
                }
            }
        }
        public static void UpdateControls(ServerInfo info)
        {
            List<DiscordSelectComponentOption> options = [];
            Dictionary<string, string[]> scriptDict = info.botcGame.scripts;
            string[] scripts = [.. scriptDict.Keys];
            for (int i = 0; i < scripts.Length; i++)
            {
                string label = scripts[i];
                info.Log($"Adding \"{scripts[i]}\" to available scripts!");
                options.Add(new(label, label));
            }
            DiscordSelectComponent scriptSelect = new("deB_BOTCScriptSelect", "Script", options, false);
            DiscordUserSelectComponent userSelect = new("deB_BOTCUserSelect", "Players", false, 1, 15);
            DiscordButtonComponent bellButton = new(DiscordButtonStyle.Primary, "deB_BOTCBellButton", "Ring the Bell");
            DiscordButtonComponent homeButton = new(DiscordButtonStyle.Primary, "deB_BOTCHomeButton", "Home Time");
            DiscordButtonComponent nominateButton = new(DiscordButtonStyle.Primary, "deB_BOTCNominateButton", "Nomination");
            DiscordButtonComponent endButton = new(DiscordButtonStyle.Danger, "deB_BOTCEndButton", "End Game");
            info.controlsMessageBuilder = new DiscordMessageBuilder().WithContent($"{Formatter.Mention(info.currentStoryteller)}, select your players!").AddActionRowComponent(userSelect);
            info.controlsInGameMessageBuilder = new DiscordMessageBuilder().WithContent($"{Formatter.Mention(info.currentStoryteller)}, this is the Storyteller's Interface!").AddActionRowComponent(scriptSelect).AddActionRowComponent(bellButton, homeButton, nominateButton).AddActionRowComponent(endButton);
        }
        public static async Task ButtonPressed(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            ServerInfo info = activeServers[args.Guild.Id];
            List<ulong> currentPlayers = [];
            if (info.playerDictionary != null)
            {
                currentPlayers = [..info.playerDictionary.Keys];
            }
            info.Log($"Button Pressed with ID: \"{args.Id}\"");
            DiscordMember presserMember = await info.server.GetMemberAsync(args.User.Id);
            if (presserMember == info.currentStoryteller)
            {
                switch (args.Id)
                {
                    case "deB_BOTCBellButton":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            if (info.gameStarted)
                            {
                                info.Log("Bell Ring Started");
                                await client.SendMessageAsync(info.announcementsChannel, $"{Formatter.Mention(info.genericPlayerRole)} PLEASE MAKE YOUR WAY TO {Formatter.Mention(info.townChannel)}, YOU WILL BE MOVED IN 10 SECONDS---");
                                await Task.Delay(TimeSpan.FromSeconds(10));
                                for (int i = 0; i < currentPlayers.Count; i++)
                                {
                                    DiscordMember member = await info.server.GetMemberAsync(currentPlayers[i]);
                                    if (member.VoiceState != null && member.VoiceState.ChannelId != info.townChannel.Id)
                                    {
                                        info.Log($"User: {member.DisplayName} is being moved to the townhall...");
                                        await member.PlaceInAsync(info.townChannel);
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
                                    DiscordChannel home = info.playerDictionary[currentPlayers[i]];
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
                            if (!info.gameStarted)
                            {
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                                info.Log($"Users Selected: \"{args.Interaction.Data.Resolved.Users.Count}\"");
                                await args.Message.ModifyAsync(new DiscordMessageBuilder(info.controlsInGameMessageBuilder));
                                await StartGame(info, client, new(args.Interaction.Data.Resolved.Users.Keys));
                                info.gameStarted = true;
                            }
                            else
                            {
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            }
                            break;
                        }
                    case "deB_BOTCEndButton":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate, new(info.controlsMessageBuilder));
                            if (info.gameStarted)
                            {
                                await EndGame(info, client, currentPlayers);
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
                                try
                                {
                                    await info.storytellerControls.scriptMessages[i].DeleteAsync();
                                }
                                catch (Exception exception)
                                {
                                    info.Log(exception);
                                }
                                finally
                                {
                                    info.storytellerControls.scriptMessages.RemoveAt(i);
                                    i--;
                                }
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
                            DiscordMessage message = await client.SendMessageAsync(info.storytellerChannel, tokensString);
                            info.storytellerControls.scriptMessages.Add(message);
                            if (orderString != string.Empty)
                            {
                                if (split && halfway > -1)
                                {
                                    message = await client.SendMessageAsync(info.storytellerChannel, new DiscordMessageBuilder().WithContent(orderString[..halfway]).WithReply(message.Id));
                                    info.storytellerControls.scriptMessages.Add(message);
                                    info.storytellerControls.scriptMessages.Add(await client.SendMessageAsync(info.storytellerChannel, new DiscordMessageBuilder().WithContent(orderString[halfway..]).WithReply(message.Id)));
                                }
                                else
                                {
                                    info.storytellerControls.scriptMessages.Add(await client.SendMessageAsync(info.storytellerChannel, new DiscordMessageBuilder().WithContent(orderString).WithReply(message.Id)));
                                }
                            }
                            break;
                        }
                    case "deB_BOTCNominateButton":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            IEnumerable<DiscordMember> players = currentPlayers.Select(async (x) => await info.server.GetMemberAsync(x)).Select((x) => x.Result).Append(info.currentStoryteller);
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
                                    await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, "deB_BOTCBeginVote", "Begin Vote")));
                                    nomMade = true;
                                }
                            }
                            else
                            {
                                info.botcGame.currentNomination = new(player, false);
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            }
                            if (!nomMade)
                            {
                                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            }
                            break;
                        }
                    case "deB_BOTCBeginVote":
                        {
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            await BeginVote(info, client);
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
            else
            {
                switch (args.Id)
                {
                    case "":
                        {
                            break;
                        }
                    default:
                        {
                            info.Log($"Button Pressed by non-storyteller!: \"{args.Id}\"");
                            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                            break;
                        }
                }
            }
        }
        public static async Task BeginVote(ServerInfo info, DiscordClient client)
        {

        }
        public static async Task StartGame(ServerInfo info, DiscordClient client, List<ulong> ids)
        {
            info.Log("Starting BOTC Game");
            info.playerDictionary = [];
            List<DiscordMember> currentPlayers = [];
            List<string> testNames = ["Bill", "Bob", "Jack", "Jill", "Harry", "Hannah", "xXxLeanBeefxXx", "Cocktower", "Harold", "Jessica", "Emma", "Willy", "Jaqueline", "Jason", "Sean"];
            for (int i = 0; i < 5; i++)
            {
                int randomIndex = Random.Shared.Next(0, testNames.Count);
                info.botcGame.playerSeats.Add(new((ulong)i, testNames[randomIndex]));
                testNames.RemoveAt(randomIndex);
            }
            for (int i = 0; i < ids.Count; i++)
            {
                ulong id = ids[i];
                DiscordMember member = await info.server.GetMemberAsync(id);
                currentPlayers.Add(member);
                //info.botcGame.playerSeats.Add(new(id, member.DisplayName));
            }
            for (int i = 0; i < currentPlayers.Count - 1; ++i)
            {
                int newIndex = Random.Shared.Next(i, currentPlayers.Count);
                (currentPlayers[newIndex], currentPlayers[i]) = (currentPlayers[i], currentPlayers[newIndex]);
            }
            List<string> channelNames = [..info.homeChannels];
            for (int i = 0; i < currentPlayers.Count; i++)
            {
                if (channelNames.Count == 0)
                {
                    channelNames.Add($"Home #{i + 1}");
                }
                int randomIndex = Random.Shared.Next(channelNames.Count);
                DiscordChannel newHome = await info.server.CreateChannelAsync(channelNames[randomIndex], DiscordChannelType.Voice, info.homesCategory, overwrites: [new DiscordOverwriteBuilder(currentPlayers[i]) { Allowed = DiscordPermission.ViewChannel }]);
                channelNames.RemoveAt(randomIndex);
                info.playerDictionary.Add(currentPlayers[i].Id, newHome);
                await currentPlayers[i].GrantRoleAsync(info.genericPlayerRole);
            }
            await info.genericPlayerRole.ModifyPositionAsync(0);
            await client.SendMessageAsync(info.announcementsChannel, $"{Formatter.Mention(info.genericPlayerRole)} PLEASE MAKE YOUR WAY TO {Formatter.Mention(info.townChannel)}---");
            List<DiscordChannel> channels = [..info.homesCategory.Children, info.announcementsChannel, info.storytellerChannel, ..info.townChannel.Parent.Children];
            for (int i = 0; i < channels.Count; i++)
            {
                await client.SendMessageAsync(channels[i], $"---STARTING GAME---");
            }
        }
        public static async Task EndGame(ServerInfo info, DiscordClient client, List<ulong> currentPlayers)
        {
            info.Log("Ending BOTC Game");
            for (int i = 0; i < currentPlayers.Count; i++)
            {
                DiscordMember member = await info.server.GetMemberAsync(currentPlayers[i]);
                await member.ModifyAsync(delegate (MemberEditModel model) { model.VoiceChannel = null; });
                await member.RevokeRoleAsync(info.genericPlayerRole);
                await info.playerDictionary[currentPlayers[i]].DeleteAsync();
            }
            currentPlayers.Clear();
            info.playerDictionary.Clear();
            info.botcGame.playerSeats.Clear();
            info.botcGame.currentNomination = null;
            await info.currentStoryteller.RevokeRoleAsync(info.storytellerRole);
            info.currentStoryteller = null;
            info.gameStarted = false;
            List<DiscordChannel> channels = [info.announcementsChannel, info.storytellerChannel, .. info.townChannel.Parent.Children];
            for (int i = 0; i < channels.Count; i++)
            {
                await client.SendMessageAsync(channels[i], $"---END OF GAME---");
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
        public static bool IsSimilar(string stringOne, string stringTwo)
        {
            stringOne = stringOne.Replace("'", "").Replace("&", "and").Replace(" ", "");
            stringTwo = stringTwo.Replace("'", "").Replace("&", "and").Replace(" ", "");
            return stringOne.Equals(stringTwo, StringComparison.OrdinalIgnoreCase);
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
            for (int i = 0; i < scriptTokens.Count; i++)
            {
                Token token = scriptTokens[i];
                if (token.nightOrder.Item1 == -1)
                {
                    continue;
                }
                finalString += $"- **{token.characterName}**: {token.orderFirstDescription}\r";
            }
            half = finalString.Length;
            finalString += "\r## Other Nights Order:\r";
            scriptTokens.Sort((x, y) => x.nightOrder.Item2.CompareTo(y.nightOrder.Item2));
            for (int i = 0; i < scriptTokens.Count; i++)
            {
                Token token = scriptTokens[i];
                if (token.nightOrder.Item2 == -1)
                {
                    continue;
                }
                finalString += $"- **{token.characterName}**: {token.orderOtherDescription}\r";
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