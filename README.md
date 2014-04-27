
### About

GaGa is a very lightweight, simple online radio player that runs as an icon
on the Windows tray. It's similar to [RadioTray][] for Linux. Here is a
screenshot:

![Screenshot1](https://raw.github.com/Beluki/GaGa/master/Screenshot/Screenshot1.png)

Left click on the icon toggles play/stop. Middle click (with the mouse wheel)
toggles mute. Right click opens the menu with a list of streams.

On a modern system, GaGa typically uses about 12 MB of ram and 1% CPU while
playing. It supports .m3u, .asx, .mp3 and basically everything that Windows
Media Player supports.

### The streams file

GaGa is designed to make it convenient to add, move or share radio stations
between users. Unlike most other software, in which the user adds streams
one by one in a GUI, GaGa uses an INI file. This file is automatically
reloaded on changes and clicking "Edit streams file" opens it in your
default editor for the .ini extension.

Here is an example. The "Jamendo" submenu in the screenshot above looks
like this in the default streams file:

```ini
[Jamendo]
Best Of Jamendo = http://streaming.radionomy.com/JamBestOf
Classical = http://streaming.radionomy.com/JamClassical
Electronic = http://streaming.radionomy.com/JamElectro
Hip Hop = http://streaming.radionomy.com/JamHipHop
Jazz = http://streaming.radionomy.com/JamJazz
Lounge = http://streaming.radionomy.com/JamLounge
Metal = http://streaming.radionomy.com/JamMetal
Pop = http://streaming.radionomy.com/JamPop
Relaxation = http://streaming.radionomy.com/JamRelaxation
Rock = http://streaming.radionomy.com/JamRock
Songwriting = http://streaming.radionomy.com/JamSongwriting
Soundtrack = http://streaming.radionomy.com/JamSoundtrack
World = http://streaming.radionomy.com/JamWorld
```

It is possible to create arbitrarily nested submenus using a separator
in the section name. Here is another example:

```ini
[Video Games/Oldies]
Kohina = http://kohina.radio.ethz.ch/kohina.mp3

[Video Games]
A.I. Radio = http://dir.xiph.org/listen/483984/listen.m3u
NoLife = http://nolife-radio.com/radio/NoLife-radio.m3u
```

Which produces the following menu:

![Screenshot2](https://raw.github.com/Beluki/GaGa/master/Screenshot/Screenshot2.png)

Scared of mistakes? Don't worry, GaGa will load an alternate menu with
error details (including exact line) on any error. In the worst-case
scenario, if you delete your streams.ini file, GaGa will recreate it
with a list of default radio streams.

Performance-wise, streams file reloading is usually instant or takes
milliseconds, even with hundreds of radio stations defined.

### Compiling and installation

Building GaGa is a matter of opening the included Visual Studio 2013
solution and clicking the build button (or using msbuild). The source code
has no dependencies other than the [.NET Framework][] 4.0+.

I'll provide prebuilt binaries if there are requests (use the [Issues][] tab).

GaGa doesn't need to be installed. It can run from any folder and doesn't
write to the Windows registry. It's possible to run it from an usb stick
provided the .NET Framework is available on the target machine.

### Where to find radio stations

GaGa only includes the Jamendo radio stations in the default ini. Here are
some places with tons of radio stations, classified by genre or by country:

* [Listen Live](http://www.listenlive.eu)
* [Radionomy](http://www.radionomy.com/en)
* [The Icecast Directory](http://dir.xiph.org)

### Portability

GaGa is tested on Windows 7 and 8, using the .NET Framework 4.0+. [Mono][]
is not supported, because it doesn't implement MediaPlayer (in PresentationCore)
which GaGa uses for playback.

The streams file encoding is UTF-8 with or without a BOM signature. Notepad
will work, although I suggest something better such as [Notepad2][] or
[Sublime Text][].

### Status

There are no known bugs in GaGa. I may still add new features. Nice things
to have would be .pls format support and global hotkeys (using multimedia
keys on keyboards that have them). Contributions and ideas are welcome, as
are bug reports.

### License

Like all my hobby projects, this is Free Software. See the [Documentation][]
folder for more information. No warranty though.

[Documentation]: https://github.com/Beluki/GaGa/tree/master/Documentation

[Issues]: https://github.com/Beluki/GaGa/issues
[Mono]: http://mono-project.com
[Notepad2]: http://www.flos-freeware.ch/notepad2.html
[.NET Framework]: http://www.microsoft.com/en-us/download/details.aspx?id=30653
[RadioTray]: http://radiotray.sourceforge.net
[Sublime Text]: http://www.sublimetext.com/

