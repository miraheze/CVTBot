# Install CVTBot

## Installation

1. Compile the code by running the following command:

   `msbuild src/CVTBot.sln /p:Configuration=Release`

   This command creates `CVTBot.exe` and other files in the output directory at `src/CVTBot/bin/Release`.
1. Create a directory for your bot, and move the contents of `src/CVTBot/bin/Release` to it.
1. Edit `CVTBot.ini`: Set at least `botnick`.
1. Set permissions and ownership correctly. This step is after the copying of files because group ownership is usually not preserved when copying files.
   * For personal use, `chmod 644 *`, `chmod 600 CVTBot.ini`, and `chmod 755 CVTBot.exe`.
   * For organisational use, `chmod 664 *`, `chmod 660 CVTBot.ini`, `chmod 755 CVTBot.exe`, and `chgrp cvt.cvtservice *`.
1. You can now start the start the bot by running `mono CVTBot.exe` from your bot directory.<br/>The bot will join the specified `feedchannel`.

## Upgrade

1. Compile the code by running the following command:

   `msbuild src/CVTBot.sln /p:Configuration=Release`

   This command creates `CVTBot.exe` and other files in the output directory at `src/CVTBot/bin/Release`.
1. Enter `src/CVTBot/bin/Release`.
1. Remove `Projects.xml` and `CVTBot.ini` (to avoid accidentally overwriting your existing ones, later)
1. Make sure the bot is not currently running (e.g. `Botname quit` on IRC, and check output of `ps aux`).
1. Copy all remaining files in `src/CVTBot/bin/Release` to your existing bot directory. For example: `src/CVTBot/bin/Release$ cp * /srv/cvt/services/cvtbot/CVTBotXYZ/`
1. Set permissions and ownership correctly. This step is after the copying of files because group ownership is usually not preserved when copying files.
   * For personal use, `chmod 644 *`, `chmod 600 CVTBot.ini`, and `chmod 755 CVTBot.exe`.
   * For organisational use, `chmod 664 *`, `chmod 660 CVTBot.ini`, `chmod 755 CVTBot.exe`, and `chgrp cvt.cvtservice *`.
1. Start the bot.
