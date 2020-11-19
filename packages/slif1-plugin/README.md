# SimHub SLI-F1 Plugin

This is a simple plugin for SimHub to add support for a single
[SLI-F1](http://www.leobodnar.com/shop/index.php?main_page=product_info&cPath=97&products_id=184/) board.

The plugin is not massively configurable at the moment, as it was essentially written to do exactly what I wanted.
This may change over time; it would probably be nice to allow configuring the RPM LED array the same way SimHub can configure
LED strips, etc. along with the segment displays and status LEDs. Alternatively, the code is available so you can hack it to your
requirements!

Some ideas for improvements I might do are [here](https://github.com/simelation/simhub-plugins/issues/1).

## Installation from package

Release packages are available from [GitHub](https://github.com/orgs/simelation/packages?repo_name=simhub-plugins).
Packages are currently published as gzipped tar files, so you may need an appropriate tool to unzip them
(e.g. [7zip](https://www.7-zip.org/)).

Copy the `package\bin\Release\SimElation.SimHub.SliF1.dll` file from the package to your SimHub installation directory
(e.g. `C:\Program Files (x86)\SimHub`) and (re)start SimHub. SimHub should ask for confirmation to enable the new plugin.

## Stock SLI-F1 firmware vs. Roso firmware

The plugin was written using the custom Roso SLI-F1 firmware. I don't think there's any reason why it shouldn't work with the
stock firmware, however. HID report layouts appear the same.

## Segment displays

Data that can be displayed is as follows (in parentheses is the text that will be shown temporarily when changing mode):

### Left segment display

1. Current lap # (`Lap`)
1. Laps remaining (`togo`)
1. Position (`Posn`)
1. Fuel remaining, as `xx.yy` where `xx` = units remaining; `yy` = laps remaining (`Fuel`)
1. Brake bias, front:rear (`bias`)
1. Oil temperature (`Oil`)
1. Water temperature (`H2O`)

### Right segment display

1. Current laptime (`Curr`)
1. Last laptime (`Last`)
1. Best lap time in this session (`BstS`)
1. All-time best laptime (`BstA`)
1. Delta to session best laptime (`dltS`)
1. Delta to all-time best laptime (`dltA`)
1. Gap to car ahead (`gApA`)
1. Gap to car behind (`gApb`)

Note that some of this data will come from the SimHub PersistantTrackerPlugin if not provided by the game
(e.g. PC2/AMS2 lap time deltas).

## Status LEDs

These are currently hard-coded to show the following data:

### Left bank

1. Blue flag.
1. Yellow flag.
1. Fuel low alert.

### Right bank

1. ABS active.
1. TC active.
1. DRS: blinking = available; solid = active.

## Configuration

Configuration is available in SimHub at `Additional Plugins` then the `SLI-F1 Plugin` tab.

### External LEDs

External LEDs can currently be configured to display a SimHub property, e.g. `DataCorePlugin.GameData.Flag_Yellow`.
There's no support yet for ncalc formulas, blinking, etc., though this may follow.

Click on an external LED in the UI and use the property picker to choose the property. It should be one that makes
sense to be interpreted as either "on" or "off".

An assigned external LED will turn red in the UI. You hover over an LED to see what its assignment is and right click on
it to clear the assignment.

### Segment displays

Segment displays can be configured to be controlled by rotary switches connected to the SLI-F1, or manually from the UI, or using
controller buttons.

#### Rotary switch control

For rotary switch control, the plugin needs to learn which rotary to watch for changes of position.
Press one of the `Learn rotary switch` buttons and change the position of the rotary you wish to use for that function.
If the rotary is detected, the button should change to `Forget rotary switch N` (where N is the rotary's number).

Once assigned, changing the position of a rotary assigned to the segment displays will show some text describing the new data that
will be displayed (e.g. `Currnt`, `Fuel`, etc.) for a short period (which is configurable; set to 0 to disable completely).

#### UI control

No rotary switch should be assigned in order to control the display from the UI. You can then simply set the `Mode` drop down for
the left or right segment display to the one you require.

#### Button (or rotary encoder) control

No rotary switch should be assigned in order to control the display using buttons, which can be assigned by pressing the
`Click to configure` buttons for `Next` and `Previous` (which use the regular SimHub UI).
Note the displays cycle, so it's not necessary to assign both (or you could assign short press to go to next,
long press for previous).

#### Peeking

##### Showing the name of the current mode

When switching display modes, the name of the new mode will be shown for a while. It's also possible to assign a button that,
whilst pressed, will show the display mode's name. Helpful if you've forgotten what mode you're in!

To configure, press `Click to configure` for `Current` under `Peeking`. You must set the `Press type` to `During`
and then press the button you wish to use.

Peeking the value of another mode (without changing the current mode) will follow in a future release.

### Brightness control

Similarly, brightness can be control by a rotary switch or UI. Press `Learn rotary` next to it and change the position of
the rotary you wish to use for that function. If the rotary is detected, the button should change to `Forget rotary N`. If
the rotary switch isn't 12-position, you can set the correct number with the `Number of positions` setting.

Brightness can also be set explicitly in the plugin's configuraion using a slider if no rotary switch is assigned.

### Welcome message

This is just some text to display in the segments when SimHub is running but a game isn't. It will be centered across the
left/gear/right segments displays.

### Pit lane animation speed

How quickly (in milliseconds) to animate the RPM LEDs when in the pits. Set to 0 to disable the special in pits RPM LED display.

### Shift point blink on time / Shift point blink off time

The RPM LEDs will blink when the RPM % is at or above the threshold set in the SimHub main configuration
(`Car Settings` -> `Red line`). These settings control the blink on/off time (in milliseconds). Set the `off` value to 0 to
disable shift point blinking completely.

### Show name of new segment mode for

When changing the left or right segment displays, some text can be shown for a small period of time showing the new mode
for the display. 0 will disable.

### Brightness level

Brightness for the LEDs. Any value set here will override the position of an assigned rotary switch (i.e. the rotary is ignored).

## Building from source

In order to decouple the project files from the SimHub installation location, a symlink needs to be created in the same
directory as this file to your SimHub location:

`mklink "C:\Program Files (x86)\SimHub" SimHub`

(adjust as necessary if installed elsewhere)

`mklink` may require elevated priveleges if developer mode is not enabled.
See [here](https://www.ghacks.net/2016/12/04/windows-10-creators-update-symlinks-without-elevation/).

Alternatively, `yarn prepare` in this directory will attempt to create the symlink (though it does assume the path to SimHub).

Debug builds will install the dll via this symlink. You may need to delete an existing `SimElation.SimHub.SliF1.dll` from the
SimHub installation directory if previously installed before a debug build will succeed, since it will itself be a symlink to the
built dll in `bin\Debug`.

Release builds do not install the dll into the SimHub installation directory; the generated dll will only be in `bin\Release`.

### Visual Studio 2019

Open `SimElation.SimHub.SliF1Plugin.sln` with Visual Studio 2019.

### Visual Studio Code

Building within Visual Studio Code is supported, as long as `msbuild.exe` is in the path.

If Visual Studio 2019 is not also installed, then you will probably need the
[build tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/) and
[.NET Framework](https://dotnet.microsoft.com/download/visual-studio-sdks) 4.7.2.
