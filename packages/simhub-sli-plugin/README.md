# SimHub SLI Plugin

This is a simple plugin for SimHub to add support for [SLI-Pro](https://www.leobodnar.com/products/SLI-PRO/) and
[SLI-F1](http://www.leobodnar.com/shop/index.php?main_page=product_info&cPath=97&products_id=184/) boards.

Some ideas for improvements I might make are [here](https://github.com/simelation/simhub-plugins/issues/).

## Installation from package

Release packages are available from [GitHub](https://github.com/orgs/simelation/packages?repo_name=simhub-plugins).
Packages are currently published as gzipped tar files, so you may need an appropriate tool to unzip them
(e.g. [7zip](https://www.7-zip.org/)).

Copy the `package\bin\x86\Release\SimElation.SimHub.SliPlugin.dll` file from the package to your SimHub installation directory
(e.g. `C:\Program Files (x86)\SimHub`) and (re)start SimHub. SimHub should ask for confirmation to enable the new plugin.

## Stock SLI-Pro firmware vs. Roso firmware

For the SLI-Pro device, the plugin was tested using the custom Roso SLI-Pro firmware. I don't think there's any reason why
it shouldn't work with the stock firmware, however. HID report layouts appear the same.

## Features

### Segment displays

Data that can be displayed is as follows (in parentheses is the text that will be shown temporarily when changing mode on the
SLI-Pro; the SLI-F1 is further truncated due to a 4 character limit):

#### Left segment display

1. Current lap # (`Lap`)
1. Laps remaining (`togo`) or blank if not applicable
1. Position (`Posn`)
1. Fuel remaining, as `FxxLyy` SLI-Pro or `xx.yy` SlI-F1 where `xx` = units remaining; `yy` = laps remaining (`Fuel`).
   If laps remaining is not known, only the unit remaining is shown
1. Brake bias, front:rear (`bbias`)
1. Oil temperature (`Oil`)
1. Water temperature (`H2O`)
1. Current speed (`SPd`)
1. Current RPM (`rPM`)

#### Right segment display

1. Current laptime (`Currnt`)
1. Last laptime (`Last`)
1. Best lap time in this session (`BstSES`)
1. All-time best laptime (`BstAll`)
1. Delta to session best laptime (`dltSES`)
1. Delta to all-time best laptime (`dltAll`)
1. Gap to car ahead (`gApAhd`)
1. Gap to car behind (`gApbhd`)
1. Current speed (`SPd`)
1. Current RPM (`rPM`)

Note that some of this data will come from the SimHub PersistantTrackerPlugin if not provided by the game
(e.g. PC2/AMS2 lap time deltas).

### Status LEDs

By default, these are configured to show the following data:

#### Left bank

1. Blue: blue flag.
1. Yellow: yellow flag.
1. Red: fuel low alert (blinking).

#### Right bank

1. Yellow: ABS active.
1. Blue: TC active.
1. Red: DRS. Blinking = available; solid = active.

### External LEDs

No external LEDs are configured by default, but they are supported.

### Rotary switches

#### SLI-F1

The SLI-F1 has support for 8 rotary switches. Switch 8 will appear as individual controller buttons (so it's trivial to configure a
game for "digital" functions assigned to each switch position), but the other 7 switches will look like analog axes.
This can be OK if you wish to use some of those rotary switches for controlling the segment display mode or device brightness,
since games do not need to "see" those values (the plugin handles them internally). However, that potentially leaves 4 rotary
switches which aren't usable in-game (well, unless an analog axis that only has values from 0 to 11 is of some use somewhere...).

(AFAICT, this also appears to be the case with Fanaleds; at least I can't see a way to make use of the other rotary switches. Not
sure about SLIMaxManager.)

#### SLI-Pro

The same is true of the SLI-Pro, though the numbers are different. I think the Roso firmware supports (at least) 2 switches
that will appear as buttons and (at least) 3 as analog axes. The stock firmware may be different again.

#### Rotary switch to vJoy button mapping

As a generic solution to this that doesn't require game-specific plugins, the SLI plugin can watch the rotary switches which appear
as analog axes for changes of position and map those changes to `vJoy` button presses. Most games should just work (though may be
a PITA to configure) - possbily with the exception of ones that have a very low limit on the number supported controller devices.

## Plugin configuration

Configuration is available in SimHub at `Additional Plugins` then the `SLI Plugin` tab.

When a supported device is first plugged in, it should appear in the UI under the `DEVICES` list. At this point, the device
is not controlled by the plugin; it needs to be told to do that by hitting the toggle button next to `Unmanaged` in the header.

The status should now change to `Available` and the full configuration UI will appear.

Note that hitting the toggle button again will tell the plugin to stop managing the device (the status will go back to
`Unmanaged`) and settings for that device will be deleted.

If you have multiple devices, you can collapse the UI for a particular device that is being managed by clicking anywhere on
the header other than the toggle button.

If a managed device is unplugged, the UI for it will still be visible but you won't be able to edit any settings.

### Display brightness

Brightness can be controlled by a rotary switch or from the UI. Press `Learn rotary switch` under `Display Brightness`
and change the position of the rotary switch you wish to use for that function. If the rotary is detected,
the button should change to `Forget rotary N`. If the rotary switch isn't 12-position, you can set the correct number
with the `Number of positions` setting.

Brightness can also be set explicitly in the plugin's configuraion using a slider if no rotary switch is assigned.

### LEDs

The 6 on-device status LEDs and optional external LEDs can be configured here.

If a status or external LED doesn't have an expression assigned to it, it will be gray. Otherwise, it will be colored. Clicking on
an LED will bring up the familiar SimHub ncalc expression dialog. Please note that `Use javascript` mode is currently not
supported.

Documentation for ncalc is available [here](https://github.com/ncalc/ncalc/wiki).

The expression should evaluate to the following values to control the LED:

-   0: LED off
-   1: LED on (solid)
-   2: LED blinking (at a rate determined by the `LED blink interval` setting)

For example, the default DRS expression is:

`if (([IsInPitLane] || [IsInPit]), 0, if ([DRSEnabled], 1, if ([DRSAvailable], 2, 0)))`

To unassign an LED, click `None` at the top of the ncalc expression dialog then `OK`.

### Segment displays

Segment displays can be configured to be controlled by rotary switches connected to the SLI device, or manually from the UI,
or using controller buttons.

#### Welcome message

This is just some text to display in the segments when SimHub is running but a game isn't. It will be centered across the
left/gear/right segment displays.

#### Show name of new segment mode for

When changing the left or right segment displays, some text can be shown for a small period of time showing the new mode
for the display. 0 will disable.

Note that using the "peek" functionality to quickly look at another segment mode will not show the name of the mode.

#### Rotary switch control

For rotary switch control, the plugin needs to learn which rotary to watch for changes of position.
Press one of the `Learn rotary switch` buttons and change the position of the rotary you wish to use for that function.
If the rotary is detected, the button should change to `Forget rotary switch N` (where N is the rotary's number).

#### Manual control

No rotary switch should be assigned in order to control the display from the UI. You can then simply set the `Mode` drop down for
the left or right segment display to the one you require.

#### Button (or rotary encoder) control

No rotary switch should be assigned in order to control the display using buttons, which can be assigned by pressing the
`Click to configure` buttons for `Next` and `Previous` using the familiar SimHub UI.
Note the displays cycle, so it's not necessary to assign both (or you could assign short press to go to next,
long press for previous).

#### Peeking

##### Show current mode

When switching display modes, the name of the new mode will be shown for a while. It's also possible to assign a button that,
whilst pressed, will show the display mode's name. Helpful if you've forgotten what mode you're in!

To configure, press `Click to configure` for `Current` under `Peeking`. You must set the `Press type` to `During`.

##### Peeking another mode

Similarly, to peek the value of another display mode whilst a button is pressed, press `Click to configure` next to the mode under
`Peeking`. Again you must set the `Press type` to `During`.

Note if you've assigned a button for peek `Current`, you can press that at the same time as a peek mode button to see the name
of the mode that is being peeked.

### RPM LEDs

Note the plugin does not make use of the SimHub `Car Settings` -> `Shift light N offset` settings for the different sections
of the RPM display. It simply uses `Minimum displayed RPM` for the LED that has been configured as the first RPM LED
(see next section) and evenly distributes RPM% across the remaining LEDs.

##### Assignable LEDs

The RPM LEDs can be used as additional assignable status LEDs, rather than for showing revs, if you prefer.
The default is to use all RPM LEDs for the rev display, but increasing `Number of assignable LEDs` will un-gray LEDs
(from the left) in the UI such that they can be clicked in order to assign an ncalc or Javascript expression.

##### In pit-lane animation

Whilst in the pits, the RPM LEDs can animate between two different layouts. Simply click on an LED to toggle it on or off. Note
all RPM LEDs are used here, irrespective of the `Number of assignable LEDs` setting.

###### Animation speed

How quickly (in milliseconds) to animate the RPM LEDs when in the pits. Set to 0 to disable the special in pits RPM LEDs.

##### Shift point animation

These settings control the blink on/off time (in milliseconds) when the RPM % is at or above the threshold set in the SimHub main
configuration (`Car Settings` -> `Red line`). Set the `off` value to 0 to disable shift point blinking completely.

### Rotary switch to vJoy button mapping

First thing to say is if you don't require this functionality, there is nothing to do: the plugin doesn't require `vJoy` to be
installed.

#### vJoy installation

So some bad news: some versions of the vJoy library can cause a crash of SimHub. So, we don't want to use those versions.
Good news: the plugin won't allow it - you should see a warning banner like `vJoy unavailable: dll version 216 is known to cause crashses`.
The minimum version of vJoy that I have tested that works is 2.1.8. 2.1.6 is definitely bad and 2.1.7 I can't find it to try.
Some more info [here](https://github.com/SHWotever/SimHub/issues/696).

Also with vJoy, the driver version needs to match the library version in order for things to work. Again, the plugin will show a warning
banner if that's not the case (`vJoy unavailable: driver version 221 doesn't match dll version 216`).

Here's what I recommend doing:

-   Install either the latest vJoy version from http://vjoystick.sourceforge.net/site/index.php/download-a-install/download or
-   Install the (seemingly more active) fork from https://github.com/njz3/vJoy/releases.
-   Copy the following files from `C:\Program Files\vJoy\x86` (note `x86` NOT `x64`) to your SimHub installation directory:
    -   `vJoyInterface.dll`
    -   `vJoyInterfaceWrap.dll`
-   Start SimHub. The UI options for vJoy mapping should now appear on the `SLI Plugin` page under a managed SLI device section.

I won't cover the configuration of vJoy here, but suffice to say you need at least one device with as many buttons configured as
you have rotary switch positions you wish to map. The plugin doesn't do any validation that how you've configured a mapping
is valid.

Note I think there is an issue with the vJoy C# wrapper, though it doesn't seem to affect things.
More info [here](https://github.com/njz3/vJoy/pull/2).

#### Plugin configuration

##### vJoy button press time

This is how long the plugin will "press" the vJoy device's button when it sees a rotary switch has changed position.

##### Refresh vJoy devices

If you add/remove vJoy devices whilst SimHub is running, you can press this button to update the plugin's knowledge of those
devices.

##### Adding a new mapping

Hit the `New mapping` button to add a rotary switch. When the dialog appears, changes the position of the rotary you wish to map.
If the rotary is detected, a new panel should appear in the UI (`Configuration for rotary switch N`).

###### Number of rotary switch positions

Simply set this to the number of positions your rotary switch has (up to 12).

###### vJoy device id

The vJoy device you wish to map to (1 - 16, as seen tab title in the `vJoyConf` app).

###### First vJoy button

This is the button number on the vJoy device that should correspond to the first rotary switch position.
Successive rotary positions will use successive vJoy buttons (so if you set the first vJoy button as 9 and have a
12-position rotary, the first rotary position will use button 9 and the last will use button 20).

Note that the default first button for a new mapping is 3: this is because for some reason, AMS2 (so maybe PC1/2 also) appear to
treat buttons 1 and 2 on any vJoy device as left/right mouse button presses.

#### Configuring the game

You may wish to check using the `vJoy Monitor` application that the mapping is working once you've configured it, before attempting
to configure your games. It can be tricky!

You may be able to configure your games by simply assigning a control in-game and changing the position of the rotary switch you've
just mapped.

This will be somewhat dependent on how intelligent the game is: effectively it will "see" two different devices changing something
(the original SLI device moving an analog axis and the virtual vJoy device button press). It may complain that it can't
distinguish between the two inputs - AMS2 (so I guess PC1/2 also) is one where this can happen, even though if you're assigning a
digital function it could happily ignore the analog input. AC is smart enough to figure it out more reliably, it seems.

It's worth trying moving both up and down to select the rotary position you are trying to assign.
I.e. if you are trying to set position 3, try moving to there from position 2 and if that doesn't work, try from position 4.

If that doesn't work, there are a few more complicated things that might do the trick.

##### Using the plugin's UI to press a vJoy button

The plugin's UI can be used to "press" a vJoy button. But again it can be complicated: the game probably needs focus for it to
recognize a button press, and it may be looking for the button to be pressed _or_ released.

As a result, you can tell the plugin to `Pause before button press for` (in milliseconds) and how long it should `Press button for`
(milliseconds again) after hitting `Simulate button press`.

You can then alt-tab to the game and press its assign control button. All being well, the game should then recognize the
vJoy button either when the plugin presses it, or when it releases it.

(Running the game in windowed mode temporarily to configure this way may be easier, or if you have multiple monitors and can
keep the desktop on one monitor with SimHub.)

Simple!

##### Calibrating the SLI device axis

Another option to limit the chance of the game seeing an analog axis change on the SLI device may be to use the Windows game
controller calibration for the SLI device, without adjusting the relevant rotary switch to its min and max positions
(depends which input on the SLI device is being used for the rotary switch; hit enable `Display raw data` and try moving the
rotary on each analog axis page in the calibration UI to figure it out, then rerun the calibration but don't move the rotary
switch when it gets to that axis again).

##### HidGuardian

Another option may be to filter out the analog axis using [HidGuardian](https://github.com/ViGEm/HidGuardian). I haven't tried that
myself yet, however, so can't comment further.

## Building from source

In order to decouple the project files from the SimHub installation location, a symlink needs to be created in the same
directory as this file to your SimHub location:

`mklink "C:\Program Files (x86)\SimHub" SimHub`

(adjust as necessary if installed elsewhere)

`mklink` may require elevated priveleges if developer mode is not enabled.
See [here](https://www.ghacks.net/2016/12/04/windows-10-creators-update-symlinks-without-elevation/).

Alternatively, `yarn prepare` in this directory will attempt to create the symlink (though it does assume the path to SimHub).

Debug builds will install the dll via this symlink. You may need to delete an existing `SimElation.SimHub.SliPlugin.dll` from the
SimHub installation directory if previously installed before a debug build will succeed, since it will itself be a symlink to the
built dll in `bin\x86\Debug`.

Release builds do not install the dll into the SimHub installation directory; the generated dll will only be in `bin\x86\Release`.

### Visual Studio 2019

Open `SimElation.SimHub.SliPlugin.sln` with Visual Studio 2019.

### Visual Studio Code

Building within Visual Studio Code is supported, as long as `msbuild.exe` is in the path.

If Visual Studio 2019 is not also installed, then you will probably need the
[build tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/) and
[.NET Framework](https://dotnet.microsoft.com/download/visual-studio-sdks) 4.7.2.
