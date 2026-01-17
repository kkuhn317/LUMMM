# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [2.2.1] - 2025-06-26

### Fixed

- Fixed Pointer_Stringify error
- Fixed HTML mouse events for downloading files

## [2.2.0] - 2024-10-14

### Added

- Opening a file with the same name now doesn't overwrite the old file but adds a number to it.

## [2.1.0] - 2024-09-11

### Added

- Headless mode - if a FileOpen script is not attached to a UI Canvas it is considered to be headless and you can call 
  the method "OpenFile" to start the file browser. This now works in-editor and in webgl builds

## [2.0.3] - 2024-07-25

### Fixed

- Now able to upload files with the same name repeatedly

## [2.0.2] - 2024-04-29

### Fixed

- 'DownloadFromIndexedDB' now properly downloads result.contents instead of the serialized parent result object

## [2.0.1] - 2023-12-11

### Changed

- Cached RectTransforms to reduce overhead in Update loops

## [2.0.0] - 2023-12-11

### Changed

- Canvas scalefactor is now used when the width and height is determined of the canvas in WebGL, adding support for DPI scaling canvases

## [1.3.1] - 2023-08-08

### Fixed

- File browser now works on Apple silicon (M1/M2)
- Default extension(s) for FileOpen component is now `csv` instead of `.csv`; the dot will be included in the extensions
  and cause the opening to fail.

## [1.3.0] - 2023-08-03

### Added

* Added functionality to button to let the user select files and save the files to application.persistentDatapath in 
  webgl and standalone.

## [1.2.0] - 2018-11-06

All releases prior to 1.3 are made in the original repository; to provide a modicum of continuity this fork will 
continue on from 1.3.

See https://github.com/gkngkc/UnityStandaloneFileBrowser/releases/tag/1.2
