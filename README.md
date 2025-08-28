# DeBOTCBot

FANMADE, TO USE OFFICIAL TOOLS FOR RUNNING BLOOD ON THE CLOCKTOWER GAMES GO TO https://bloodontheclocktower.com

Repository for my first Discord bot, for making and running [Blood on The Clocktower](https://bloodontheclocktower.com) games within a Discord server, as well as managing and generating "scripts" and grimoires!

(No link to add yet, I have no means of hosting it at the moment, this is purely for version tracking and keeping up with development for the time being)

You can use the [Issues](https://github.com/TheDebbyCase/DeBOTCBot/issues) tab to submit any problems with the bot, or any suggestions you may have.
Suggestions can also be made directly to me if you can find me on Discord, or on [Bluesky](https://bsky.app/profile/thedebbycase.bsky.social)!

If you wish you can also donate to me over on [Ko-Fi](https://ko-fi.com/thedebbycase)!

- ## Limitations
 
	<details>
	<summary>Seating</summary>
 
	Currently no game implementation for the seating arrangement of players, nothing planned at the moment  
 
	This must be done using the main website  
	
	</details>
 
	<details>
	<summary>Nomination Phase</summary>
 
	Currently no game implementation for the nomination phase, nothing planned at the moment  
	
	This must be done using the main website  
	
	</details>

	<details>
	<summary>Travellers</summary>
	
	Currently no helper commands or info about Traveller tokens, nothing planned at the moment  
	
	</details>

	<details>
	<summary>Fabled</summary>
	
	Currently no helper commands or info about Fabled tokens, nothing planned at the moment  
	
	</details>

- ## Commands
	<details>
	<summary>/botc create</summary>

	### Required Permission: Manage Channels  

	### Creates roles:  

	Storyteller, upon giving someone this role a special set of game controls are generated within the "Storyteller's Crypt" channel  
 
	BOTC Player, gives access to join and message in game channels (given automatically by the storyteller, no need to manually give to players)  

	### Creates storyteller channels:  

	(Text) "botc-announcements" for ingame public announcements/information through text (Message permissions for Storyteller only)  

	(Voice) "Watchtower" for spectators to watch a stream (where possible) and discuss amongst themselves (Members with the BOTC Player role cannot see this channel)  

	(Voice) "Storyteller's Crypt" for private conversations between players and the storyteller, the voice text channel is also where the storyteller's controls are (Members with the BOTC Player role cannot see this channel, everyone else except the storyteller cannot join unless moved by the storyteller)  
 
	### Creates town channels:  
 
	(Voice) "Town Square" no voice limit, storyteller can drag players to this channel with their controls  
	
	(Voice) Town Channels, configurable amount, names and voice limits using commands (There are a few by default but they are replaceable also using commands)  

	### Creates homes category:  

	Only the category is made, individual voice channels are added later. By default, members with the BOTC Player role cannot see these channels, but each get assigned one channel that they can see, join and message in upon game start. Storyteller has full access to these channels.  

	</details>

	<details>
	<summary>/botc destroy</summary>
	
	### Required Permission: Manage Channels  
 
	Removes all of the above channels and roles, including ones added at game start  
	
	</details>

	<details>
	<summary>/botc save</summary>
	
	### Required Permission: Administrator  
 
	Forces relevant server information to save to the bot's database (Currently my PC, all info saved is listed in the ServerSaveInfo class)  
	
	</details>

	<details>
	<summary>/botc reset</summary>
	
	### Required Permission: Administrator  
 
	Forces bot to remove server information, resetting to default values  
	
	</details>
 
	<details>
	<summary>/botc storyteller</summary>
	
	### Required Permission: Manage Channels  
 
	Lets you select a member to become a storyteller. Only one storyteller at a time, will end the game if ongoing  
	
	</details>

	<details>
	<summary>/botc pandemonium</summary>
	
	Sends an ephemeral message with links to the official BOTC website and Patreon  
	
	</details>

	<details>
	<summary>/botc tokens show</summary>
	
	Sends an ephemeral message with a list of all character tokens, organised by type  
	
	</details>

	<details>
	<summary>/botc tokens description</summary>
	
	Sends an ephemeral message with the token description of a specified character token  
	
	</details>

	<details>
	<summary>/botc scripts all</summary>
	
	Sends an ephemeral message with a list of all available scripts, with characters, organised by type  
	
	</details>
 
	<details>
	<summary>/botc scripts show</summary>
	
	Sends an ephemeral message with a list of all characters in a specified script, organised by type  
	
	</details>

	<details>
	<summary>/botc scripts new</summary>
	
	### Required Permission: Manage Channels  
 
	Adds a new available script, specifying name and tokens to use, sends ephemeral message with the script and its tokens, organised by type  
	
	</details>

	<details>
	<summary>/botc scripts remove</summary>
	
	### Required Permission: Manage Channels  
 
	Removes an available script by name  
	
	</details>

	<details>
	<summary>/botc scripts edit</summary>
	
	### Required Permission: Manage Channels  
 
	Adds and removes specified tokens from an available script, sends ephemeral message with successfully added and removed tokens  
	
	</details>

	<details>
	<summary>/botc scripts night</summary>
	
	Creates a night order from a specified, available script, sending an ephemeral message with the tokens, organised by the order they wake at night  
	
	</details>

	<details>
	<summary>/botc scripts roll</summary>
	
	Creates a grimoire from a specified, available script and number of players, sending an ephemeral message with the tokens, organised by type, and a night order  
	
	</details>

	<details>
	<summary>/botc scripts default</summary>
	 
	### Required Permission: Manage Channels  

	Resets available scripts to the default 3 main scripts  
	
	</details>

	<details>
	<summary>/botc town show</summary>
	
	Sends an ephemeral message with a list of all available town channel names and voice limits  
	
	</details>

	<details>
	<summary>/botc town add</summary>
	
	### Required Permission: Manage Channels  
 
	Adds a new available town channel, specifying name and voice limit. If town channels currently exist the channel is created  
	
	</details>

	<details>
	<summary>/botc town remove</summary>
	
	### Required Permission: Manage Channels  
 
	Removes an available town channel, specifying name. If town channels currently exist the channel is deleted  
	
	</details>

	<details>
	<summary>/botc town edit</summary>
	
	### Required Permission: Manage Channels  
 
	Edits an existing available town channel, specifying name, new name and new voice limit. New name and voice limit can be left blank to remain unchanged. If town channels currently exist, the specified channel is edited  
	
	</details>

	<details>
	<summary>/botc town default</summary>
	
	### Required Permission: Manage Channels  
 
	Resets available town channels to the default values  

	</details>

	<details>
	<summary>/botc homes show</summary>
	
	Sends an ephemeral message with a list of all available home channel names  

	</details>

	<details>
	<summary>/botc homes add</summary>
	
	### Required Permission: Manage Channels  
 
	Adds a new available home channel name  

	</details>

	<details>
	<summary>/botc homes remove</summary>
	
	### Required Permission: Manage Channels  
	
	Removes an available home channel name  

	</details>

	<details>
	<summary>/botc homes set</summary>
	
	### Required Permission: Manage Channels  
 
	Overwrites all available home channel names with a specified list of names  

	</details>

	<details>
	<summary>/botc homes default</summary>
	
	### Required Permission: Manage Channels  
	
	Resets available home channel names to the default values  

	</details>

- ## Storyteller Controls  

	Storyteller controls are only created once a member has been given the storyteller role  

	<details>
	<summary>Pre-game</summary>
	
	Has access to a selection menu where the storyteller can select any number of members between 5 and 15 inclusive, each of these members will be given the BOTC Player role  
	
	Upon selection, the game will "start", creating a number of home channels equal to the number of players, each player being assigned a home they have access to  
	
	A message will be sent in every BOTC created channel to timestamp the start of the game  
	
	The storyteller controls will change upon the game starting  

	</details>

	<details>
	<summary>In-game</summary>
							
	Has access to a selection menu where the storyteller can select any available script with which to generate a grimoire using the number of players selected previously, and a night order  
	
	Has access to a button which the storyteller can press to notify players they have 10 seconds to go back to the Town Square channel. After the elapsed time, players will be forced into the channel  
	
	Has access to a button which the storyteller can press to send each player to their assigned home channel  
	
	Has access to a button which the storyteller can press to "end" the game, removing all home channels, removing the storyteller controls, removing the Storyteller and the BOTC Player roles from all members, and sending a message in every BOTC created channel to timestamp the end of the game  

	</details>