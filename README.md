[![Build Status](https://github.com/Universal-Omega/MirahezeCVTBot/actions/workflows/CI.yml/badge.svg)](https://github.com/Universal-Omega/MirahezeCVTBot/actions/workflows/CI.yml)

MirahezeCVTBot
==================================================


Quick start
----------

Clone the repo, `git clone git://github.com/Universal-Omega/MirahezeCVTBot.git`


Build
----------
The software is written in C# and originally created as a Visual Studio Project.
We use `mono` to run the executable and `msbuild` to build the executable.

Recommended installation methods:

* For Linux, install [`mono-complete`](https://packages.debian.org/search?keywords=mono-complete) from Debian, or [latest from mono-project.com](https://www.mono-project.com/download/stable/#download-lin),
* For Mac, install [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/) (enable Mono and .NET during installation).
* For Windows, install [Visual Studio](https://visualstudio.microsoft.com/vs/) (enable Mono and .NET during installation).

For standalone command-line installations on Mac or Windows, see [monodevelop.com](https://www.monodevelop.com/download/).

Currently supported versions of Mono: **6.8**

Once mono is installed, build the project. The below uses Debug, for local development. (See [Installation](./docs/install.md) for how to install it in production):

```bash
Universal-Omega/MirahezeCVTBot:$ msbuild src/CVNBot.sln /p:Configuration=Debug
```

Once built, you can run it:
```bash
Universal-Omega/MirahezeCVTBot/src/CVNBot/bin/Debug:$ mono CVNBot.exe
```


Bug tracker
-----------

Found a bug? Please report it using our [issue
tracker](https://github.com/Universal-Omega/MirahezeCVTBot/issues)!


Documentation, support and contact
-----------
* [Documentation (wiki)](https://github.com/Universal-Omega/MirahezeCVTBot/wiki/Documentation)


Copyright and license
---------------------

See [LICENSE](https://raw.github.com/Universal-Omega/MirahezeCVTBot/main/LICENSE.txt).
