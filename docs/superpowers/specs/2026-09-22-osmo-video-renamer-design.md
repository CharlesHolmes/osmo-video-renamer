# OsmoVideoRenamer design

**Date:** 2026-09-22
**Status:** Design approved by Charles; spec reviewed and approved 2026-09-22

## 1. Purpose

Rename DJI Osmo Pocket 3 videos in bulk after a trip so that the files appear in
recording order when a directory is sorted alphanumerically. The tool is a sibling of
[gopro-video-renamer](https://github.com/CharlesHolmes/gopro-video-renamer): same
command-line contract, same architecture, same test style, adapted to the DJI file
naming convention.

## 2. Scope

In scope:

- Videos recorded by the Osmo Pocket 3 (`.MP4`) that follow the DJI naming convention.
- The companion files DJI writes next to each video: the `.LRF` low-resolution proxy
  (always written) and the `.WAV` audio backup (written when that setting is enabled).
  They are renamed to match their video so the set stays paired.
- A single, non-recursive directory.

Out of scope (non-goals):

- Photos (`.JPG`, `.DNG`) and any other file. They are never touched.
- Putting the capture date into the new name. The scheme is identical to the GoPro
  tool. A date option would be an easy later addition.
- Deleting companion files, joining split segments, or reading video metadata.
- Recursing into sub-directories.

## 3. DJI Osmo Pocket 3 naming convention

Every capture is named `DJI_<timestamp>_<counter>_<letter>.<ext>`:

| Part | Meaning | Example |
|------|---------|---------|
| `DJI_` | Fixed prefix | `DJI_` |
| `<timestamp>` | 14 digits, `yyyyMMddHHmmss`, camera local time at capture | `20240315123456` |
| `<counter>` | 4 digits, starts at `0001`, increments by one per capture | `0007` |
| `<letter>` | One or more upper-case letters; `D` on every Pocket 3 file seen so far | `D` |
| `<ext>` | `MP4` for video, `LRF` for the proxy, `WAV` for the audio backup, `JPG` and `DNG` for photos | `MP4` |

Example: `DJI_20240315123456_0007_D.MP4` with companions
`DJI_20240315123456_0007_D.LRF` and `DJI_20240315123456_0007_D.WAV`.

Facts and assumptions the design relies on:

- The counter is the camera's capture order within a `DCIM/DJI_00n` folder. It is
  shared by photos and videos, so two videos in one folder never share a counter value.
- The counter restarts only when the camera opens a new `DJI_00n` folder (after 9999
  captures), when numbering is reset in the camera's Naming Management setting, or when
  files from two cards are merged into one directory.
- Long recordings are split at about 4 GB on older firmware and about 16 GB on newer
  firmware. Each segment is assumed to be its own capture with the next counter value.
  Whether a later segment's timestamp is the split time or the original start time is
  unverified and does not matter under counter ordering.
- The camera clock can be wrong or can change time zone mid-trip. The counter is
  therefore the trustworthy order; the timestamp is not.

Sources: DJI support articles on the Osmo Action/Pocket video split logic and on `.LRF`
files; the Osmo Pocket 3 user manual (Naming Management setting); Paul Glagla,
"The files of the camcorder DJI Osmo Pocket 3" (2025-10-27).

## 4. Command-line contract

```text
OsmoVideoRenamer --file-location <String> [--prefix <String>] [--suffix <String>]
                 [--starting-number <Int32>] [--digit-count <Int32>] [--dry-run]
                 [--help] [--version]

Options:
  --file-location <String>     Directory where the Osmo videos are stored (Required)
  --prefix <String>            Text that should appear before each file's number
  --suffix <String>            Text that should appear after each file's number
  --starting-number <Int32>    What number the renamed files should start at (default 1)
  --digit-count <Int32>        The number of digits to include in each file number
  --dry-run                    Print a list of the files to be renamed, but do not rename them
```

Console output, one block per video, in the new order:

```text
1: DJI_20240315123456_0007_D.MP4 -> Trip - 001.MP4
    DJI_20240315123456_0007_D.LRF -> Trip - 001.LRF
    DJI_20240315123456_0007_D.WAV -> Trip - 001.WAV
2: DJI_20240315124501_0008_D.MP4 -> Trip - 002.MP4
    DJI_20240315124501_0008_D.LRF -> Trip - 002.LRF
```

Companion lines are indented four spaces. When no DJI videos are found the tool prints
`No DJI Osmo videos found in <directory>.` and exits successfully. All other failures
are thrown as exceptions and reach the console through Cocona, exactly as in the GoPro
tool, with a non-zero exit code.

## 5. Behaviour

### 5.1 Recognizing videos

A directory entry is a DJI video when its whole name matches, case-insensitively,

```text
^DJI_(\d{14})_(\d{4})_[A-Z]+\.MP4$
```

and the 14 digits parse as a valid `yyyyMMddHHmmss` timestamp. The pattern is anchored
(the GoPro tool's pattern is not). A name whose digits do not form a valid date is not a
DJI video and is skipped, never a crash.

### 5.2 Ordering

Videos are ordered by counter (sequence number), ascending. That is the sole sort key.

- If any counter value appears more than once, the run aborts with an
  `ArgumentException` explaining that the counter restarted or files from more than one
  card are mixed, and that they should be split into separate directories. This mirrors
  the GoPro tool's duplicate check and prevents silently interleaving two sessions.
- Walking the videos in counter order, if any video's timestamp is earlier than the
  previous video's timestamp, a warning is logged ("timestamps are not in sequence
  order; the camera clock may have changed or the time zone may have been adjusted").
  Equal timestamps, which split segments may share, do not trigger it. The run
  continues in counter order.

Each video receives a new index: `--starting-number` (default 1) for the first,
increasing by one. A negative `--starting-number` aborts with an
`ArgumentOutOfRangeException`; zero is allowed.

### 5.3 New names

New base name: `{prefix}{index formatted to digit count}{suffix}`. The video keeps its
original extension (`.MP4`, in whatever case it was found); each companion keeps its own.

Digit count: `--digit-count` when given, otherwise the number of decimal digits in the
largest new index, computed as the length of its decimal string so that an index of 0
yields one digit (the GoPro tool's base-10 logarithm is not used because it fails for
0). If `--digit-count` is smaller than that, the run aborts with an
`ArgumentOutOfRangeException` (same rule and message shape as the GoPro tool).

The prefix and suffix must produce a plain file name. If the computed base name contains a
path separator (so that `Path.GetFileName` would change it) or any character that the
platform does not allow in file names, the run aborts with an `ArgumentException` after a
Critical log, before any name is planned. Without this check a suffix such as `/../out`
would escape the directory and defeat the collision check in 5.5, which compares names.

### 5.4 Companion files

For each video, every file in the same directory whose base name (name without
extension) equals the video's base name, compared case-insensitively, and whose
extension is `.LRF` or `.WAV` (case-insensitive) is a companion. It is renamed to the
video's new base name plus its own extension. The extension allow-list is deliberate:
a stray `.JPG` that somehow shares a base name is left alone.

### 5.5 Collision check

Before any file is touched, the set of planned new names (videos and companions) is
checked:

- no planned name may occur twice (case-insensitive), and
- no planned name may equal the name of any file already in the directory
  (case-insensitive).

Any violation aborts the run with an `IOException` that lists the offending names.
Because the check runs before the first rename, the directory is either fully renamed
or untouched. A dry run performs the check too, so it reports exactly what a real run
would do.

### 5.6 Renaming

Each rename is `IFileInfo.MoveTo(Path.Combine(directory.FullName, newName))`. The path
is joined with the platform separator (the GoPro tool hard-codes a backslash, which
breaks on macOS). The video is moved first, then its companions, in output order.

## 6. Architecture

.NET 8 console application. Cocona provides the command line, dependency injection,
and logging. `System.IO.Abstractions` (`TestableIO.System.IO.Abstractions.Wrappers`)
wraps the file system so every class is unit-testable with mocks. Every collaborator
is an interface with a factory, exactly as in the GoPro project, because that
scaffolding is what lets the test suite reach near-100 % coverage.

### 6.1 Pipeline

```text
RenameCommand.Rename(fileLocation, prefix, suffix, startingNumber, digitCount, dryRun)
  1. IVideoDirectoryFactory.Create(fileLocation)           -> IVideoDirectory   (throws DirectoryNotFoundException if missing)
  2. IVideoDirectory.GetFilesInDirectory()                  -> IReadOnlyList<IDirectoryFile>  (every entry, materialized once)
  3. IFileFilter.GetMatchingVideos(allFiles)                -> IEnumerable<IVideoFile>        (5.1)
  4. IFileSort.GetOrderedFiles(videos, startingNumber)      -> List<INumberedVideoFile>       (5.2)
     if empty: print "No DJI Osmo videos found in <dir>." and return
  5. IFileRename.GetRenamedFiles(numbered, allFiles, prefix, suffix, digitCount)
                                                            -> IList<IRenamedVideoFile>       (5.3, 5.4)
  6. IRenameCollisionChecker.VerifyNoCollisions(renamed, allFiles)                            (5.5)
  7. for each renamed video: print lines; unless dry run, CommitRenameToDisk() on the video, then on each companion (5.6)
```

### 6.2 Components

| Component | Responsibility | Depends on |
|-----------|----------------|------------|
| `Program` | Builds the Cocona app, registers services, filters, commands. Excluded from coverage. | Configuration classes |
| `Configuration.ServiceConfiguration` | Registers every interface/implementation pair as transient. | all below |
| `Configuration.CommandConfiguration` | Registers `RenameCommand`. | Cocona |
| `Configuration.FilterConfiguration` | Registers the parameter-logging filter. | `ParameterLogging` |
| `ParameterLogging.*` | Logs each received option at Information level (copied from GoPro). | `ILogger` |
| `ConsoleWrapping.IConsoleWrapper` / `ConsoleWrapper` | `WriteLine`, `WriteErrorLine` over `Console`. | none |
| `Directory.IVideoDirectory` / `VideoDirectory` | Verifies the directory exists; wraps each entry as `IDirectoryFile`. | `IFileSystem`, `IDirectoryFileFactory`, `ILogger` |
| `Directory.IVideoDirectoryFactory` / `VideoDirectoryFactory` | Creates a `VideoDirectory` for a path, resolving dependencies from `IServiceProvider`. | `IServiceProvider` |
| `File.Naming.DjiVideoFileName` | Immutable record with static `TryParse(name, out DjiVideoFileName?)` and `Parse(name)`; instances expose `CaptureTimestamp` and `SequenceNumber`. The timestamp is parsed with format `yyyyMMddHHmmss` and `CultureInfo.InvariantCulture`. Single source of truth for the pattern. | none |
| `File.DirectoryFiles.IDirectoryFile` / `DirectoryFile` | Any directory entry: `FileInfo`, `Name`, `BaseName`, `FileExtension`. | `IFileInfo` |
| `File.DirectoryFiles.IDirectoryFileFactory` / `DirectoryFileFactory` | `Create(IFileInfo)`. | none |
| `File.VideoFiles.IVideoFile` / `VideoFile` | A recognized DJI video: `IDirectoryFile` plus `SequenceNumber`, `CaptureTimestamp`, parsed eagerly in the constructor (throws `ArgumentException` for a non-DJI name). Copy constructor from `IVideoFile`. | `DjiVideoFileName` |
| `File.VideoFiles.IVideoFileFactory` / `VideoFileFactory` | `Create(IDirectoryFile)`. | none |
| `File.VideoFiles.Numbered.INumberedVideoFile` / `NumberedVideoFile` | `IVideoFile` plus `NewIndex`. Copy constructors as in GoPro. | none |
| `File.VideoFiles.Numbered.INumberedVideoFileFactory` / factory | `Create(newIndex, IVideoFile)`. | none |
| `File.VideoFiles.Renamed.IRenamedVideoFile` / `RenamedVideoFile` | `INumberedVideoFile` plus `NewName`, `Companions` (`IReadOnlyList<IRenamedCompanionFile>`), `CommitRenameToDisk()` (video only). | `IFileInfo` |
| `File.VideoFiles.Renamed.IRenamedVideoFileFactory` / factory | `Create(newName, companions, INumberedVideoFile)`. | none |
| `File.CompanionFiles.IRenamedCompanionFile` / `RenamedCompanionFile` | `IDirectoryFile` plus `NewName`, `CommitRenameToDisk()`. | `IFileInfo` |
| `File.CompanionFiles.IRenamedCompanionFileFactory` / factory | `Create(newName, IDirectoryFile)`. | none |
| `File.ICompanionFileFinder` / `CompanionFileFinder` | `GetCompanions(IVideoFile, IEnumerable<IDirectoryFile>)` per 5.4. | none |
| `File.IFileFilter` / `FileFilter` | `GetMatchingVideos(IEnumerable<IDirectoryFile>)` per 5.1, creating videos through `IVideoFileFactory`. | `IVideoFileFactory`, `DjiVideoFileName` |
| `File.IFileSort` / `FileSort` | `GetOrderedFiles(IEnumerable<IVideoFile>, int?)` per 5.2. | `INumberedVideoFileFactory`, `ILogger` |
| `File.IFileRename` / `FileRename` | `GetRenamedFiles(IList<INumberedVideoFile>, IEnumerable<IDirectoryFile>, prefix, suffix, digitCount)` per 5.3 and 5.4. Returns an empty list for empty input. | `IRenamedVideoFileFactory`, `IRenamedCompanionFileFactory`, `ICompanionFileFinder`, `ILogger` |
| `File.IRenameCollisionChecker` / `RenameCollisionChecker` | `VerifyNoCollisions(IEnumerable<IRenamedVideoFile>, IEnumerable<IDirectoryFile>)` per 5.5. | `ILogger` |
| `RenameCommand` | The Cocona command; orchestrates 6.1 and writes console output. | everything above through interfaces |

Namespaces mirror the GoPro project (`OsmoVideoRenamer.Configuration`,
`OsmoVideoRenamer.Directory`, `OsmoVideoRenamer.File.VideoFiles.Numbered`, and so on),
with three additions: `File.Naming`, `File.DirectoryFiles`, `File.CompanionFiles`.

### 6.3 Error handling summary

| Situation | Result |
|-----------|--------|
| Directory does not exist | `DirectoryNotFoundException` from `VideoDirectory` (logged Critical) |
| No DJI videos | Message on stdout, exit 0 |
| Repeated counter value | `ArgumentException` from `FileSort` (logged Critical) |
| Negative `--starting-number` | `ArgumentOutOfRangeException` from `FileSort` (logged Critical) |
| Timestamp earlier than the previous one in counter order | Warning logged, run continues |
| `--digit-count` too small | `ArgumentOutOfRangeException` from `FileRename` (logged Critical) |
| Prefix or suffix produces a path or an invalid file name | `ArgumentException` from `FileRename` (logged Critical) |
| Planned name duplicated or already present | `IOException` from `RenameCollisionChecker` (logged Critical), nothing renamed |
| `MoveTo` fails mid-run | Exception propagates; files already moved stay moved (same as GoPro) |

## 7. Testing

- `OsmoVideoRenamer.UnitTests`: MSTest, Moq, FluentAssertions, coverlet, same package
  set and versions as the GoPro test project. One test class per production class, in
  the same folder layout. Every collaborator is mocked through its interface; factories
  are tested for the type they produce; `ServiceConfiguration` is tested by resolving
  each interface from a real `ServiceCollection`; `ConsoleWrapper` tests redirect
  `Console.Out`/`Console.Error` with `[DoNotParallelize]`.
- `DjiVideoFileName` tests cover: valid names; lower-case extension; multi-letter
  suffix; wrong prefix; 13 or 15 timestamp digits; invalid date such as month 13;
  `.LRF`/`.WAV`/`.JPG` extensions rejected; unanchored junk around a valid name rejected.
- `FileSort` tests cover: counter order regardless of input order; counter order wins
  over timestamp order and logs the warning; equal timestamps do not warn; duplicate
  counter aborts; custom starting number; zero starting number allowed; negative
  starting number aborts.
- `FileRename` tests cover the GoPro cases (prefix, suffix, both, neither, digit count
  given, auto-padded, too small) plus an index of 0 padding to one digit, companions
  receiving the same base name and their own extension, empty input, and a prefix or
  suffix that contains a path separator or an invalid file-name character.
- `CompanionFileFinder` tests cover: `.LRF` and `.WAV` found; case-insensitive base name
  and extension; `.JPG` and unrelated names ignored; the video itself not returned.
- `RenameCollisionChecker` tests cover: clean set passes; planned name equal to an
  existing file aborts; two planned names equal aborts; case-insensitivity.
- `RenameCommand` tests cover: preview output including companion lines; dry run commits
  nothing; real run commits video then companions; empty set prints the message and
  calls nothing further; collision checker runs before any commit.
- `Program` is `[ExcludeFromCodeCoverage]` as in GoPro. Target is the same near-100 %
  line coverage the GoPro project reports.
- End-to-end check, run by hand before finishing: create a scratch directory with fake
  DJI-named `.MP4`, `.LRF`, `.WAV`, `.JPG` files in shuffled counter order plus a
  `notes.txt`, run `--dry-run` and confirm nothing changed, run for real and confirm the
  resulting names, then run again and confirm the "no videos" message.

## 8. Repository

- Folder `osmo-video-renamer` is already a git repository on `main` (first commit: this
  spec) with the local commit identity set to match the GoPro commits, so the plan needs
  no repository or identity setup. Creating the GitHub repository is left to Charles.
- Boilerplate marked "copied from GoPro" comes from a read-only clone of
  gopro-video-renamer at commit 145a44f:
  `/private/tmp/claude-501/-Users-charlie-repos-osmo-video-renamer/c39be8f0-77d6-4811-8735-caf9e1ad1ae9/scratchpad/gopro-video-renamer`.
- Files: `OsmoVideoRenamer.sln`, `OsmoVideoRenamer/`, `OsmoVideoRenamer.UnitTests/`,
  `README.md` (usage block in the GoPro README style plus a short description of the DJI
  naming convention and the companion-file behaviour), `.gitignore` (the Visual Studio
  template used by GoPro), `.github/dependabot.yml`, `.github/workflows/pr-checks.yml`,
  `.github/workflows/auto-merge-dependabot.yml` (copied from GoPro).
- Packages: Cocona 2.2.0, TestableIO.System.IO.Abstractions.Wrappers 22.2.0;
  tests: FluentAssertions 8.11.0, Microsoft.NET.Test.Sdk 18.10.1, Moq 4.20.72,
  MSTest.TestAdapter and MSTest.TestFramework 3.11.0, coverlet.collector 10.0.1.
- Target framework `net8.0`; the machine has SDK 8.0.420.
