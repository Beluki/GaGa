
## About

GaGa is a very lightweight, simple online radio player that runs as an icon
on the Windows tray. It's similar to [RadioTray][] for Linux. Here are two
screenshot:

![Screenshot1](https://raw.github.com/Beluki/GaGa/master/Screenshot/Screenshot1.png)

Left click on the icon toggles play/stop. Middle click (with the mouse wheel)
toggles mute. Right click opens the menu with a list of streams. The icon
reflects the current state (e.g. an animation while buffering).

GaGa also implements global hotkeys using multimedia keys. Toggle them on
or off in the options submenu if your keyboard has them.

On a modern system, GaGa typically uses about 16 MB of ram and 1% CPU while
playing. It supports .m3u, .asx, .mp3 and basically everything that Windows
Media Player supports.

## The streams file

GaGa is designed to make it convenient to add, move or share radio stations
between users. Unlike most other software, in which the user adds streams
one by one in a GUI, GaGa uses an INI file. This file is automatically
reloaded on changes and clicking "Edit streams file" opens it in your
default editor for the .ini extension.

Here is an example. The "Radio Reddit" submenu in the screenshot above looks
like this in the streams file:

```ini
[Radio Reddit]
Main = http://cdn.audiopump.co/radioreddit/main_mp3_128k
Electronic = http://cdn.audiopump.co/radioreddit/electronic_mp3_128k
Indie = http://cdn.audiopump.co/radioreddit/indie_mp3_128k
Hiphop = http://cdn.audiopump.co/radioreddit/hiphop_mp3_128k
Metal = http://cdn.audiopump.co/radioreddit/metal_mp3_128k
Rock = http://cdn.audiopump.co/radioreddit/rock_mp3_128k
```

It is possible to create arbitrarily nested submenus using a separator
in the section name. Here is another example:

```ini
[Videogames/Oldies]
Kohina = http://kohina.radio.ethz.ch/kohina.mp3

[Videogames]
A.I. Radio = http://ai-radio.org/radio.mp3.m3u
NoLife = http://nolife-radio.com/radio/NoLife-radio.m3u
```

Which produces the following menu:

![Screenshot2](https://raw.github.com/Beluki/GaGa/master/Screenshot/Screenshot2.png)

Scared of mistakes? Don't worry, GaGa will load an alternate menu with
error details (including exact line) on any error. In the worst-case
scenario, if you delete your Streams.ini file, GaGa will recreate it
with a list of default radio streams.

Performance-wise, streams file reloading is usually instant or takes
milliseconds, even with hundreds of radio stations defined.

## Compiling and installation

Building GaGa is a matter of opening the included Visual Studio 2010
solution and clicking the build button (or using msbuild). The source code
has no dependencies other than the [.NET Framework][] 4.0+.

There are binaries for the latest version in the [Releases][] tab above.

GaGa doesn't need to be installed. It can run from any folder and doesn't
write to the Windows registry. It's possible to run it from an usb stick
provided the .NET Framework is available on the target machine.

## Where to find radio stations

GaGa only includes the Jamendo radio stations in the default ini. Here are
some places with tons of radio stations, classified by genre or by country:

* [Listen Live](http://www.listenlive.eu)
* [Radionomy](http://www.radionomy.com/en)
* [The Icecast Directory](http://dir.xiph.org)

## Portability

GaGa is tested on Windows 7 and 8, using the .NET Framework 4.0+. [Mono][]
is not supported, because it doesn't implement MediaPlayer (in PresentationCore)
which GaGa uses for playback.

The streams file encoding is UTF-8 with or without a BOM signature. Notepad
will work, although I suggest something better such as [Notepad2][] or
[Sublime Text][].

## Status

This program is finished!

GaGa is feature-complete and has no known bugs. Unless issues are reported
I plan no further development on it other than maintenance.

## License

Like all my hobby projects, this is Free Software. See the [Documentation][]
folder for more information. No warranty though.

[Documentation]: https://github.com/Beluki/GaGa/tree/master/Documentation
[Releases]: https://github.com/Beluki/GaGa/releases

[Mono]: http://mono-project.com
[.NET Framework]: http://www.microsoft.com/en-us/download/details.aspx?id=30653
[Notepad2]: http://www.flos-freeware.ch/notepad2.html
[RadioTray]: http://radiotray.sourceforge.net
[Sublime Text]: http://www.sublimetext.com/

