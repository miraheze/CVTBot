# Install CVTBot

## Installation

1. Compile the code by running the following command:

```bash
dotnet build src/CVTBot/CVTBot.csproj --configuration Release
```

   This will create the compiled DLL and other files in `src/CVTBot/bin/Release/net8.0`.

2. Create a directory for your bot, and move the contents of `src/CVTBot/bin/Release/net8.0` to it.

3. Edit `CVTBot.ini`: Set at least `botnick`.

4. Set permissions and ownership correctly. This step is after copying files because group ownership is usually not preserved when copying files:
   * For personal use: `chmod 644 *`, `chmod 600 CVTBot.ini`, and `chmod 755 CVTBot.dll`.
   * For organisational use: `chmod 664 *`, `chmod 660 CVTBot.ini`, `chmod 755 CVTBot.dll`, and `chgrp cvt.cvtservice *`.

5. You can now start the bot by running:

```bash
dotnet CVTBot.dll
```

   The bot will join the specified `feedchannel`.

---

## Upgrade

1. Compile the code by running the following command:

```bash
dotnet build src/CVTBot/CVTBot.csproj --configuration Release
```

   This creates the updated DLL and other files in `src/CVTBot/bin/Release/net8.0`.

2. Enter `src/CVTBot/bin/Release/net8.0`.

3. Remove `Projects.xml` and `CVTBot.ini` (to avoid accidentally overwriting your existing ones).

4. Make sure the bot is not currently running (e.g., `Botname quit` on IRC, and check output of `ps aux`).

5. Copy all remaining files to your existing bot directory. For example:

```bash
cp * /srv/cvt/services/cvtbot/CVTBotXYZ/
```

6. Set permissions and ownership correctly:
   * For personal use: `chmod 644 *`, `chmod 600 CVTBot.ini`, and `chmod 755 CVTBot.dll`.
   * For organisational use: `chmod 664 *`, `chmod 660 CVTBot.ini`, `chmod 755 CVTBot.dll`, and `chgrp cvt.cvtservice *`.

7. Start the bot:

```bash
dotnet CVTBot.dll
```
