# Royale Brawl v29

A Brawl Stars private server for 29.258 based on royale Brawl

why did I create this server? because all public v29 servers suck

## Download the client [here](https://mega.nz/file/T3gTxSQY#xER8enz0SggjF9bSWnDyqFne2MXiJCSg9zBywOga9gM)
![Logo](https://github.com/Erder00/royale-brawl-v29/blob/main/docs/screenshots/lobby.png?raw=true)


## Features


- offline battles
- vip / premium system
- account system (supercell ID like system)
- report system
- discord integration
- online game rooms
- friends & clubs
- brawl pass and trophy road
- global, club and brawler leaderboards
- random events
- creator codes
- slash commands in clubs
- anti-ddos

#### discord bot commands

- !help - shows all available commands  
- !status - show server status  
- !ping - will respond with pong  
- !unlockall - will unlock EVERYTHING on the player's account (!unlockall [TAG])  
- !givepremium - gives premium to an account (!givepremium [TAG])
- !ban - ban an account (!ban [TAG])  
- !unban - unban an account (!unban [TAG])  
- !mute - mute a player (!mute [TAG])  
- !unmute - unmute a player (!unmute [TAG])  
- !resetseason - resets the season
- !changename - changes a players name (!changename [TAG] [newName])
- !reports - sends a link to all reported messages  
- !userinfo - show player info (!userinfo [TAG])  
- !changecredentials - change username/password of an account (!changecredentials [TAG] [newUsername] [newPassword])  
- !settrophies - set trophies for all brawlers (!settrophies [TAG] [Trophies])  
- !addgems - grant gems to a player (!addgems [TAG] [Gems])  
- !givepremium - give premium to an account for one month (!givepremium [TAG])

#### club commands

- /help - lists all available commands
- /status - shows the server status
## Installation

requirements:

[dotnet 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[mysql](https://dev.mysql.com/downloads/)

How to install:

[Android](https://github.com/Erder00/royale-brawl-v29/blob/main/docs/Android.md)
[Linux](https://github.com/Erder00/royale-brawl-v29/blob/main/docs/Linux.md)
[Windows](https://github.com/Erder00/royale-brawl-v29/blob/main/docs/Windows.md)

then connect to the server using the [pre-made client](https://mega.nz/file/T3gTxSQY#xER8enz0SggjF9bSWnDyqFne2MXiJCSg9zBywOga9gM)

decompile the apk and replace the ip in `lib/armeabi-v7a/liberder.script.so` with your own

## Acknowledgements

 - based on [royale brawl](https://github.com/Erder00/royale-brawl) from [xeon](https://git.xeondev.com/xeon)
 - using [netcord](https://netcord.dev) for the discord stuff (it's really cool, check it out)
 - [spz](https://github.com/spz2020) and [santer](https://github.com/SANS3R66) and [8Hacc](https://github.com/8-bitHacc) for the pull requests <3

## TODO

- better anti-cheat (check csv shas and apk integrity in script, detect if battles are very short)
- crypto
- add more club commands
- add creator codes
