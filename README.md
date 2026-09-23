# osmo-video-renamer
Renames DJI Osmo Pocket 3 videos so that they appear in the correct order when sorted alphanumerically.

## Usage

```text
~$ OsmoVideoRenamer --help

Usage: OsmoVideoRenamer [--file-location <String>] [--prefix <String>] [--suffix <String>] [--starting-number <Int32>] [--digit-count <Int32>] [--dry-run] [--help] [--version]

OsmoVideoRenamer

Options:
  --file-location <String>     Directory where the Osmo videos are stored (Required)
  --prefix <String>            Text that should appear before each file's number
  --suffix <String>            Text that should appear after each file's number
  --starting-number <Int32>    What number the renamed files should start at (default 1)
  --digit-count <Int32>        The number of digits to include in each file number
  --dry-run                    Print a list of the files to be renamed, but do not rename them
  -h, --help                   Show help message
  --version                    Show version
```

## What it does

The Osmo Pocket 3 names every capture `DJI_<timestamp>_<counter>_<letter>.<extension>`,
for example `DJI_20240315123456_0007_D.MP4`. The 14-digit timestamp is the camera clock at
the moment of capture and the 4-digit counter goes up by one with every capture.

Given a directory, the tool:

- finds every `.MP4` whose name follows that pattern;
- orders them by the counter. The camera clock can be wrong or change time zone in the
  middle of a trip, but the counter always reflects recording order;
- gives each one a new name of the form `<prefix><number><suffix>.MP4`, numbered from
  `--starting-number` (default 1) and zero-padded to `--digit-count` digits (default: just
  enough digits for the largest number);
- renames the matching `.LRF` proxy and `.WAV` audio files, when present, to the same base
  name, so `DJI_20240315123456_0007_D.LRF` becomes `Trip - 001.LRF` alongside `Trip - 001.MP4`;
- leaves photos (`.JPG`, `.DNG`) and every other file alone.

Before anything is renamed it checks that no new name is used twice or already exists in
the directory. If one does, nothing is renamed. If a counter value appears twice (files from
two cards mixed together, or the camera numbering was reset) it stops and asks you to split
the files into separate directories. If the timestamps disagree with the counter order it
logs a warning and keeps the counter order.

Run with `--dry-run` first to see the plan without changing anything.

## Example

```text
~$ OsmoVideoRenamer --file-location ~/Videos/trip --prefix "Trip - " --digit-count 3 --dry-run
1: DJI_20240315120000_0001_D.MP4 -> Trip - 001.MP4
    DJI_20240315120000_0001_D.LRF -> Trip - 001.LRF
    DJI_20240315120000_0001_D.WAV -> Trip - 001.WAV
2: DJI_20240315120500_0002_D.MP4 -> Trip - 002.MP4
    DJI_20240315120500_0002_D.LRF -> Trip - 002.LRF
```
