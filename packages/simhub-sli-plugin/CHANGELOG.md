# Change Log

All notable changes to this project will be documented in this file.
See [Conventional Commits](https://conventionalcommits.org) for commit guidelines.

# [0.9.0](https://github.com/simelation/simhub-plugins/compare/@simelation/simhub-sli-plugin@0.8.0...@simelation/simhub-sli-plugin@0.9.0) (2021-07-11)

### Bug Fixes

-   **simhub-sli-plugin:** cope with no vjoy dlls in latest simhub. ([f718800](https://github.com/simelation/simhub-plugins/commit/f718800e61f7743e284f0c200e7abcab9f9d5ac0))

### Features

-   added speed and rpm segment displays. ([d2c6fe1](https://github.com/simelation/simhub-plugins/commit/d2c6fe1a3aed17080930e28a75a547261b710f77))

# [0.8.0](https://github.com/simelation/simhub-plugins/compare/@simelation/simhub-sli-plugin@0.7.0...@simelation/simhub-sli-plugin@0.8.0) (2020-12-21)

### Features

-   **simhub-sli-plugin:** added ability to assign expressions to a configurable number of RPM LEDs. ([fcf958b](https://github.com/simelation/simhub-plugins/commit/fcf958bde9dc70017c7ebf65529a4b30e049f799))

# [0.7.0](https://github.com/simelation/simhub-plugins/compare/@simelation/simhub-sli-plugin@0.6.0...@simelation/simhub-sli-plugin@0.7.0) (2020-12-14)

### Bug Fixes

-   **simhub-sli-plugin:** was always setting LED index 0 for status and external LEDs. ([2c54ccc](https://github.com/simelation/simhub-plugins/commit/2c54ccca80a6ae6db727db2e7e04dc00fdbe3acc))

### Features

-   **simhub-sli-plugin:** added support for mapping rotary switches to vJoy devices. ([a476b44](https://github.com/simelation/simhub-plugins/commit/a476b44ee2bca364404ef9d97590a229f04647c5))
-   **simhub-sli-plugin:** use FormulaPicker for LEDs. Javascript now works. ([d54318d](https://github.com/simelation/simhub-plugins/commit/d54318dc85aeeae5c1d0a299378704f75f0b914e))

# 0.6.0 (2020-12-09)

### Features

-   **simhub-sli-plugin:** all sorts. ([2ef9405](https://github.com/simelation/simhub-plugins/commit/2ef94052c09f10350139d4c666f97414ee5f2ce3))

# [0.5.0](https://github.com/simelation/simhub-plugins/compare/@simelation/simhub-slipro-plugin@0.4.0...@simelation/simhub-slipro-plugin@0.5.0) (2020-11-20)

### Bug Fixes

-   **simhub-slipro-plugin:** time & delta formatting. ([62c7a9c](https://github.com/simelation/simhub-plugins/commit/62c7a9c05e82ca720fc0311c0405a3a72fd191e6))

### Features

-   **simhub-slipro-plugin:** added a peek current segment display mode name function assignable to a button. ([724c34e](https://github.com/simelation/simhub-plugins/commit/724c34e0d5aa0780cc0abae5b9d17f148baf9b39))
-   **simhub-slipro-plugin:** added external LED support. ([626630c](https://github.com/simelation/simhub-plugins/commit/626630cdf5adb5a743ed24d531ae9e47ba81635a))
-   **simhub-slipro-plugin:** added feedback dialogs for rotary detection process. ([826628b](https://github.com/simelation/simhub-plugins/commit/826628bf1aa378a8ae45925e76bc1f2d0ad64a6f))
-   **simhub-slipro-plugin:** show device status in UI. ([8354147](https://github.com/simelation/simhub-plugins/commit/8354147eb7d208a8ab38767220af822e2c79c431))

# [0.4.0](https://github.com/simelation/simhub-plugins/compare/@simelation/simhub-slipro-plugin@0.3.0...@simelation/simhub-slipro-plugin@0.4.0) (2020-11-01)

### Bug Fixes

-   **simhub-slipro-plugin:** don't check if a property has actually changed value before invoking OnPropertyChanged(). ([ac9ee02](https://github.com/simelation/simhub-plugins/commit/ac9ee0271c6d797d341b0de12d9492b4606c2c4f))

### Features

-   **simhub-slipro-plugin:** added support for blinking status LEDs. ([8f1a3d1](https://github.com/simelation/simhub-plugins/commit/8f1a3d1fbc8fc78f72b672ffc43b1b68bdc63efb))
-   **simhub-slipro-plugin:** ui tidy ups. ([680be9d](https://github.com/simelation/simhub-plugins/commit/680be9d121c630e207e8009516b4d88e2bc00266))

# [0.3.0](https://github.com/simelation/simhub-plugins/compare/@simelation/simhub-slipro-plugin@0.2.0...@simelation/simhub-slipro-plugin@0.3.0) (2020-10-30)

### Bug Fixes

-   **simhub-slipro-plugin:** use absolute values for ahead/behind delta. ([88e1726](https://github.com/simelation/simhub-plugins/commit/88e17267568074aad41d38e13196990823bdbc9d))

### Features

-   **simhub-slipro-plugin:** added help link (to README) in UI. ([9bc6837](https://github.com/simelation/simhub-plugins/commit/9bc68374ee6bae7d0985ce5a0b049fac02dfd513))
-   **simhub-slipro-plugin:** added support for buttons to control segment displays. ([03a1971](https://github.com/simelation/simhub-plugins/commit/03a1971f21e49574a43e1ca483e8f13a8a776877))
-   **simhub-slipro-plugin:** show version in ui. ([e669799](https://github.com/simelation/simhub-plugins/commit/e669799ca311a84402d1fa2e8cda0e4ac68701b4))
-   **simhub-slipro-plugin:** use toggle for whether a rotary controls brightness, and slider for when not. ([7c16e3b](https://github.com/simelation/simhub-plugins/commit/7c16e3bf80382a8ac521e8d5aaa448d9558358b1))

# 0.2.0 (2020-10-26)

### Bug Fixes

-   **simhub-slipro-plugin:** long messages across weren't split across the segment displays correctly by SliPro.SetTextMessage(). ([ca24e61](https://github.com/simelation/simhub-plugins/commit/ca24e6109e32aa4d9d864edd1e34b017a9da6b1e))

### Features

-   **simhub-slipro-plugin:** added UI control of segment display modes for when a rotary isn't available. ([a31302a](https://github.com/simelation/simhub-plugins/commit/a31302ae18fdd330253cdfd2afa5efbaccff6698))

# 0.1.0 (2020-10-04)

### Features

-   **simhub-slipro-plugin:** initial commit of SLI-Pro SimHub plugin. ([78f45bc](https://github.com/simelation/simhub-plugins/commit/78f45bc959292a61fb4fdcc1d805ece3d0f25e92))
