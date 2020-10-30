# SimHub SLI-Pro Plugin

This is a simple plugin for SimHub to add support for a single [SLI-Pro](https://www.leobodnar.com/products/SLI-PRO/) board.

The plugin is not massively configurable at the moment, as it was essentially written to do exactly what I wanted.
This may change over time; it would probably be nice to allow configuring the RPM LED array the same way SimHub can configure
LED strips, etc. along with the segment displays and status LEDs. Alternatively, the code is available so you can hack it to your
requirements!

Some ideas for improvements I might do are [here](https://github.com/simelation/simhub-plugins/issues/1).

## Installation from package

Release packages are available from [GitHub](https://github.com/orgs/simelation/packages?repo_name=simhub-plugins).
Packages are currently published as gzipped tar files, so you may need an appropriate tool to unzip them
(e.g. [7zip](https://www.7-zip.org/)).

Copy the `package\bin\Release\SimElation.SimHub.SliPro.dll` file from the package to your SimHub installation directory
(e.g. `C:\Program Files (x86)\SimHub`) and (re)start SimHub. SimHub should ask for confirmation to enable the new plugin.

## Segment displays

Data that can be displayed is as follows (in parentheses is the text that will be shown temporarily when changing mode):

### Left segment display

1. Current lap # (`Lap`)
1. Laps remaining (`togo`)
1. Position (`Posn`)
1. Fuel remaining, as `FxxLyy` where `xx` = units remaining; `yy` = laps remaining (`Fuel`)
1. Brake bias, front:rear (`bbias`)
1. Oil temperature (`Oil`)
1. Water temperature (`H2O`)

### Right segment display

1. Current laptime (`Currnt`)
1. Last laptime (`Last`)
1. Best lap time in this session (`BstSES`)
1. All-time best laptime (`BstAll`)
1. Delta to session best laptime (`dltSES`)
1. Delta to all-time best laptime (`dltAll`)
1. Gap to car ahead (`gApAhd`)
1. Gap to car behind (`gApbhd`)

Note that some of this data will come from the SimHub PersistantTrackerPlugin if not provided by the game
(e.g. PC2/AMS2 lap time deltas).

## Status LEDs

### Left bank

1. Blue flag.
1. Yellow flag.
1. Fuel low alert.

### Right bank

1. ABS active.
1. TC active.
1. DRS available.

## External LEDs

Currently not configurable.

## Configuration

Configuration is available in SimHub at `Additional Plugins` then the `SLI-Pro Plugin` tab. It's not very pretty yet, as this is my
first foray in to WPF...

### Segment display switching

Segment displays can be configured to be controlled by rotary switches connected to the SLI-Pro, or manually from the UI, or using
controller buttons.

#### Rotary control

For rotary switch control, the toggle button under `Rotary control` should be enabled for the left or right segment display
(as appropriate).

The plugin needs to learn which rotary to watch for changes of position.
Press one of the `Learn rotary control` buttons and change the position of the rotary you wish to use for that function.
If the rotary is detected, the button should change to `Forget rotary control`. Note these buttons are only enabled when the
`Rotary control` toggle is also enabled.

Once assigned, changing the position of a rotary assigned to the segment displays will show some text describing the new data that
will be displayed (e.g. `Currnt`, `Fuel`, etc.) for a short period (which is configurable; set to 0 to disable completely).

#### UI control

The `Rotary control` toggle should be disabled and then you can simply set the `Mode` drop down for the left or right
segment display to the one you require.

#### Button control

When `Rotary control` is disabled, you can also assign buttons to go to the next/previous display mode using the regular SimHub UI.
Note the displays cycle, so it's not necessary to assign both.

### Brightness control

Similarly, brightness can be control by a rotary switch or UI. Enable the `Rotary control` toggle under `Display Brightness`, then
press `Learn rotary` next to it and change the position of the rotary you wish to use for that function.
If the rotary is detected, the button should change to `Forget rotary`.

Brightness can also be set explicitly in the plugin's configuraion using a slider if the `Rotary control` toggle is disabled.
Any assigned rotary is ignored (and therefore it isn't mandatory to assign a rotary for the brightness function).

### Welcome message

This is just some text to display in the segments when SimHub is running but a game isn't. It will be centered across the
left/gear/right segments displays.

### Pit lane animation speed

How quickly (in milliseconds) to animate the RPM LEDs when in the pits. Set to 0 to disable the special in pits RPM LED display.

### Shift point blink on time / Shift point blink off time

The RPM LEDs will blink when the RPM % is at or above the threshold set in the SimHub main configuration
(`Car Settings` -> `Red line`). These settings control the blink on/off time (in milliseconds). Set the `off` value to 0 to
disable shift point blinking completely.

### Show new segment title for

When changing the left or right segment displays, some text can be shown for a small period of time showing the new mode
for the display. 0 will disable.

### Brightness level

Brightness for the LEDs. Any value set here will override the position of an assigned rotary control (i.e. the rotary is ignored).

## Stock SLI-Pro firmware vs. Roso firmware

The plugin was written using the custom Roso SLI-Pro firmware. I don't think there's any reason why it shouldn't work with the
stock firmware, however. HID report layouts appear the same.

## Building from source

In order to decouple the project files from the SimHub installation location, a symlink needs to be created in the same
directory as this file to your SimHub location:

`mklink "C:\Program Files (x86)\SimHub" SimHub`

(adjust as necessary if installed elsewhere)

`mklink` may require elevated priveleges if developer mode is not enabled.
See [here](https://www.ghacks.net/2016/12/04/windows-10-creators-update-symlinks-without-elevation/).

Alternatively, `yarn prepare` in this directory will attempt to create the symlink (though it does assume the path to SimHub).

Debug builds will install the dll via this symlink. You may need to delete an existing `SimElation.SimHub.SliPro.dll` from the
SimHub installation directory if previously installed before a debug build will succeed, since it will itself be a symlink to the
built dll in `bin\Debug`.

Release builds do not install the dll into the SimHub installation directory; the generated dll will only be in `bin\Release`.

### Visual Studio 2019

Open `SimElation.SimHub.SliProPlugin.sln` with Visual Studio 2019.

### Visual Studio Code

Building within Visual Studio Code is supported, as long as `msbuild.exe` is in the path.

If Visual Studio 2019 is not also installed, then you will probably need the
[build tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/) and
[.NET Framework](https://dotnet.microsoft.com/download/visual-studio-sdks) 4.7.2.
