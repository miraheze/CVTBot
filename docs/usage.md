**CVTBot** is an IRC bot written in [C#](https://en.wikipedia.org/wiki/C_Sharp_(programming_language)) designed for use as a wiki feed CVT bot. Features include dynamic loading and unloading of wikis and channels, a central configuration file and database, global lists, and detection of page blanking and replacement using automated MediaWiki summaries.

It is a modified version of [CVNBot](https://github.com/wikimedia/countervandalism-CVNBot), modified to support Miraheze and other wiki farms formatted like the same feed Miraheze uses, and to provide more configuration, primarily for usage by a global IRC feed channel, rather than per-wiki feeds the original bot supported.

## Messages
* Copyvio?: "Copyvio?" is given for when a non-Admin user or IP creates a new large page.
* Possible gibberish?: "Possible gibberish?" is given for when a non-Admin user or IP makes a large edit to an existing page.

## Lists
All global lists are automatically synchronised across the bots in the network (through the broadcast channel). Local lists are stored in the local database only. There is a global article watchlist for all wikis, and one for each one; you can add/delete/show items on the global list by leaving out the "`p=`" parameter (see the command lists below for more information).

### Global
* Bad new usernames (BNU)
* Bad new article titles (BNA)
* Bad edit summaries (BES)
* User trustlist (TL)
* User blocklist (BL)
* User flaglist (FL)
* Global article watchlist (CVP)

### Local
* Lists of admins (AL)
* Lists of bots (BOTS)
* Article watchlists (CVP)

<span id="commands"></span>
## Commands
Only voiced users (aka `+`) can use these commands, with the exception of control commands which are restricted to operators only (aka `@`).

### Info commands

| Command | Description
|--|--
| status | When the last message was received from the source.<br/>`SampleBot status`
| config<br/>settings<br/>version | Version of the bot and the currently loaded configuration (2 lines).<br/>`SampleBot config`
| bleep <em>wikiname</em> | Finds out which bot monitors a particular project. You can issue this command to any networked bot to receive the same results.<br/>`SampleBot bleep metawiki`
| count | Finds out how many wikis each bot monitors, and each bot's version. You can issue this command to any networked bot to receive the same results.<br/>`SampleBot count`
| list | Returns a list of all currently monitored wikis.<br/>`SampleBot list`
| help | Link to this documentation page.<br/>`SampleBot help`

### List commands

| Command | Description
|--|--
| bl <em>action</em> <em>user</em> <em>[x=duration]</em> <em>[r=reason]</em> | Add (`add`), delete (`del`), or show (`show`) an item on the global blocklist.<br/>`SampleBot bl add 80.10.20.123`<br/>`SampleBot bl del MrVandal`<br/>`SampleBot bl add MrVandal r=Vandal on metawiki`
| fl del <em>user</em> | Delete an item from the (global) flaglist.<br/>`SampleBot fl del MrVandal`
| tl <em>action</em> <em>user</em> <em>[x=duration]</em> <em>[r=reason]</em> | Add, delete, or show an item on the global trustlist.<br/><em>Same as `bl`</em>
| al <em>action</em> <em>user</em> <em>p=wikiname</em> <em>[x=duration]</em> <em>[r=reason]</em> | Add, delete, or show an item on the admin list for a particular wiki.<br/>`SampleBot al add MrGood p=metawiki`
| bots <em>action</em> <em>user</em> <em>p=wikiname</em> <em>[x=duration]</em> <em>[r=reason]</em> | Add, delete, or show an item on the bot list for a particular wiki.<br/><em>Same as `al`</em>
| cvp <em>action</em> <em>page</em> <em>[p=wikiname]</em> <em>[x=duration]</em> <em>[r=reason]</em> | Add, delete, or check a page on the watchlist. Use without `p=` for the global watchlist over all wikis. The default duration is `x=0`, for indefinite.<br/>`SampleBot cvp add Main Page`<br/>`SampleBot cvp add Main Page x=24`<br/>`SampleBot cvp add Main Page p=metawiki`<br/>`SampleBot cvp add Main Page p=metawiki x=24`<br/>`SampleBot cvp show Main Page`<br/>`SampleBot cvp show Main Page p=metawiki`<br/>`SampleBot cvp del Main Page`<br/>`SampleBot cvp del Main Page p=metawiki`
| bnu <em>action</em> <em>pattern</em> <em>[x=duration]</em> <em>[r=reason]</em> | Add, delete, or check a "Bad New Username" pattern ([regex](https://en.wikipedia.org/wiki/Regular_expression)). These are **always global**. The default duration is `x=0`, for indefinite.<br/>`SampleBot bnu add Sh.t r=Watch account creation like Shat, or Shot.`<br/>`SampleBot bnu add Sh.?t r=Watch account creation like Sht, Shat, or Shot.`<br/>`SampleBot bnu add Sh.*t r=Watch account creation like Sht, Shalalit, or Shower Kit.`<br/>`SampleBot bnu show Sh.it`<br/>`SampleBot bnu del Sh.it`
| bna <em>action</em> <em>pattern</em> <em>[x=duration]</em> <em>[r=reason]</em> | Add, delete, or check a "Bad New Article" pattern ([regex](https://en.wikipedia.org/wiki/Regular_expression)). These are **always global**. The default duration is `x=0`, for indefinite.<br/>`SampleBot bna add Sh.t r=Watch page creation like Shat, or Shot.`<br/>`SampleBot bna add Sh.?t r=Watch page creation like Sht, Shat, or Shot.`<br/>`SampleBot bna add Sh.*t r=Watch page creation like Sht, Shalalit, or Shower Kit.`<br/>`SampleBot bna show Sh.it`<br/>`SampleBot bna del Sh.it`
| bes <em>action</em> <em>pattern</em> <em>[x=duration]</em> <em>[r=reason]</em> | Add, delete, or check a "Bad Edit Summary" pattern ([regex](https://en.wikipedia.org/wiki/Regular_expression)). These are **always global**. The default duration is `x=0`, for indefinite.<br/>`SampleBot bes add Sh.t r=Watch edits with summary like Shat, or Shot.`<br/>`SampleBot bes add Sh.?t r=Watch edits with summary like Sht, Shat, or Shot.`<br/>`SampleBot bes add Sh.*t r=Watch edits with summary like Sht, Shalalit, or Shower Kit.`<br/>`SampleBot bes show Sh.it`<br/>`SampleBot bes del Sh.it`

#### Duration ####
The duration is measured in number of hours. `x=24` will make the entry active for 24 hours (1 day). In all list commands a duration value of `0` is used to indicate that the entry should be active indefinitely (until it is changed or removed). Otherwise the entry will expire (removed from the database) after the set duration.

The default duration is `0` for all lists, except for blocklist where the entries expire after a duration of 744 hours by default (31 days).

### Control commands

| Command | Description
|--|--
| quit | Quits the bot.<br/>`SampleBot quit`
| restart | Restarts the bot.<br/>`SampleBot restart`
| drop <em>wikiname</em> | Stop monitoring a wiki.<br/>`SampleBot drop metawiki`
| purge <em>wikiname</em> | Removes all user list information in the database pertaining to this wiki.<br/>`SampleBot purge metawiki`
| msgs | Rebuilds message cache from the Console.msgs file. Only necessary when the file has changed. This automatically hapends on restart, so only use it if the messages file has changed while the bot is running without, and want the bot to start using the new version without restarting it.<br/>`SampleBot msgs`
| reload <em>wikiname</em> | Rebuilds wiki cache from the wiki over HTTP. Use this if any of the log entry message templates have changed (either by MediaWiki software update or due to a local override by the wiki administrators).<br/>`SampleBot reload metawiki`
| batchreload | <br/>`SampleBot batchreload`
| getadmins <em>wikiname</em> | <br/>`SampleBot getadmins metawiki`
| getbots <em>wikiname</em> | <br/>`SampleBot getbots metawiki`
| batchgetusers | <br/>`SampleBot batchgetusers`

<span id="configuration"></span>
## Configuration

<span id="install"></span><span id="upgrade"></span>
## Installation
See [docs/install: Install and Upgrade](https://github.com/miraheze/CVTBot/blob/main/docs/install.md).
