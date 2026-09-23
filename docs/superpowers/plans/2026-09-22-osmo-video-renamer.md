# OsmoVideoRenamer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `OsmoVideoRenamer`, a .NET 8 command-line tool that renames DJI Osmo Pocket 3 videos (and their `.LRF`/`.WAV` companion files) into a numbered, alphanumerically sortable sequence, mirroring the existing `gopro-video-renamer` tool.

**Architecture:** A Cocona command (`RenameCommand`) runs a fixed pipeline: list directory entries → recognise DJI videos → order by the camera's capture counter → compute new names (plus companions) → check for collisions → print and rename. Every collaborator is an interface with a factory so all logic is unit-tested through Moq mocks; `System.IO.Abstractions` stands in for the real file system.

**Tech Stack:** .NET 8 (`net8.0`, SDK 8.0.420 installed), Cocona 2.2.0, TestableIO.System.IO.Abstractions.Wrappers 22.2.0; tests: MSTest 3.11.0, Moq 4.20.72, FluentAssertions 8.11.0, coverlet.collector 10.0.1, Microsoft.NET.Test.Sdk 18.10.1.

**Spec:** `docs/superpowers/specs/2026-09-22-osmo-video-renamer-design.md` (read it first; section numbers below refer to it).

**Reference checkout of the GoPro tool** (copy boilerplate from here; do not modify it):
`/private/tmp/claude-501/-Users-charlie-repos-osmo-video-renamer/c39be8f0-77d6-4811-8735-caf9e1ad1ae9/scratchpad/gopro-video-renamer`

**Ground rules for every task**

- Repository root is `/Users/charlie/repos/osmo-video-renamer`; every command below assumes `cd /Users/charlie/repos/osmo-video-renamer` first (the shell's working directory does not persist between commands).
- Follow @superpowers:test-driven-development: write the test, watch it fail (a compile error because the type does not exist yet counts as the failing run), write the minimal production code, watch it pass, commit.
- Python one-offs on this machine must be run as `uv run python3 ...`; plain `python3` is blocked by a hook.
- Git identity is already configured locally. End every commit message with the `Co-Authored-By:` attribution trailer your environment specifies for the model doing the work (the commit commands below show the Fable trailer; substitute your own model's name if your instructions give a different one).
- Namespaces mirror the GoPro project. Note that `OsmoVideoRenamer.File` and `OsmoVideoRenamer.Directory` shadow `System.IO.File` / `System.IO.Directory` inside those namespaces; the code never uses those static classes, only `Path`, which is not shadowed.
- Test files use three global usings (`FluentAssertions`, `Microsoft.VisualStudio.TestTools.UnitTesting`, `Moq`) declared once in `GlobalUsings.cs`; do not repeat them per file.
- Expected test output lines look like `Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7`. "Expected: PASS" below means `Failed: 0` for the filtered run.

---

## File structure

Production project `OsmoVideoRenamer/` (one responsibility per file):

| Path | Responsibility |
|------|----------------|
| `OsmoVideoRenamer.csproj` | Exe, net8.0, Cocona + System.IO.Abstractions |
| `Program.cs` | Cocona bootstrap only (excluded from coverage) |
| `RenameCommand.cs` | The command; orchestrates the pipeline and prints output |
| `Configuration/ServiceConfiguration.cs` | DI registrations |
| `Configuration/CommandConfiguration.cs` | Registers `RenameCommand` with Cocona |
| `Configuration/FilterConfiguration.cs` | Registers the parameter-logging filter |
| `ConsoleWrapping/IConsoleWrapper.cs`, `ConsoleWrapper.cs` | Mockable console |
| `ParameterLogging/ParameterLoggingCommandFilter.cs`, `...Factory.cs` | Logs received options (copied from GoPro) |
| `Directory/Interfaces/IVideoDirectory.cs`, `IVideoDirectoryFactory.cs`, `Directory/VideoDirectory.cs`, `VideoDirectoryFactory.cs` | Directory existence check and listing |
| `File/Naming/DjiVideoFileName.cs` | The DJI name pattern and parser (single source of truth) |
| `File/DirectoryFiles/Interfaces/IDirectoryFile.cs`, `IDirectoryFileFactory.cs`, `File/DirectoryFiles/DirectoryFile.cs`, `DirectoryFileFactory.cs` | Any directory entry |
| `File/VideoFiles/Interfaces/IVideoFile.cs`, `IVideoFileFactory.cs`, `File/VideoFiles/VideoFile.cs`, `VideoFileFactory.cs` | A recognised DJI video (counter + timestamp) |
| `File/VideoFiles/Numbered/...` | Video plus its new index |
| `File/VideoFiles/Renamed/...` | Video plus new name, companions, and the rename action |
| `File/CompanionFiles/Interfaces/IRenamedCompanionFile.cs`, `IRenamedCompanionFileFactory.cs`, `File/CompanionFiles/RenamedCompanionFile.cs`, `RenamedCompanionFileFactory.cs` | `.LRF`/`.WAV` companion plus new name and rename action |
| `File/Interfaces/ICompanionFileFinder.cs`, `File/CompanionFileFinder.cs` | Finds a video's companions (spec 5.4) |
| `File/Interfaces/IFileFilter.cs`, `File/FileFilter.cs` | Recognises DJI videos (spec 5.1) |
| `File/Interfaces/IFileSort.cs`, `File/FileSort.cs` | Orders by counter, validates (spec 5.2) |
| `File/Interfaces/IFileRename.cs`, `File/FileRename.cs` | Computes new names (spec 5.3, 5.4) |
| `File/Interfaces/IRenameCollisionChecker.cs`, `File/RenameCollisionChecker.cs` | Pre-flight collision check (spec 5.5) |

Test project `OsmoVideoRenamer.UnitTests/`: one test class per production class in the same folder layout, plus `GlobalUsings.cs`, `File/DirectoryFileMocking.cs` (mock builders shared by file tests) and `Logging/LoggerMockExtensions.cs` (ILogger verification helpers).

Repository extras: `OsmoVideoRenamer.sln`, `.gitignore`, `README.md`, `.github/dependabot.yml`, `.github/workflows/pr-checks.yml`, `.github/workflows/auto-merge-dependabot.yml`.

---

### Task 1: Scaffold the solution, projects, and repository boilerplate

**Files:**
- Create: `OsmoVideoRenamer.sln` (generated)
- Create: `OsmoVideoRenamer/OsmoVideoRenamer.csproj`
- Create: `OsmoVideoRenamer/Program.cs` (placeholder, replaced in Task 17)
- Create: `OsmoVideoRenamer.UnitTests/OsmoVideoRenamer.UnitTests.csproj`
- Create: `OsmoVideoRenamer.UnitTests/GlobalUsings.cs`
- Create: `.gitignore`, `.github/dependabot.yml`, `.github/workflows/pr-checks.yml`, `.github/workflows/auto-merge-dependabot.yml` (copied from the GoPro checkout)

- [x] **Step 1: Create the production project file**

`OsmoVideoRenamer/OsmoVideoRenamer.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Cocona" Version="2.2.0" />
    <PackageReference Include="TestableIO.System.IO.Abstractions.Wrappers" Version="22.2.0" />
  </ItemGroup>

</Project>
```

- [x] **Step 2: Create the placeholder entry point**

`OsmoVideoRenamer/Program.cs`:

```csharp
namespace OsmoVideoRenamer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            // Placeholder so the project builds; replaced in Task 17 once the Cocona configuration exists.
        }
    }
}
```

- [x] **Step 3: Create the test project file and global usings**

`OsmoVideoRenamer.UnitTests/OsmoVideoRenamer.UnitTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="8.11.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.11.0" />
    <PackageReference Include="coverlet.collector" Version="10.0.1">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="MSTest.TestFramework" Version="3.11.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\OsmoVideoRenamer\OsmoVideoRenamer.csproj" />
  </ItemGroup>

</Project>
```

`OsmoVideoRenamer.UnitTests/GlobalUsings.cs`:

```csharp
global using FluentAssertions;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using Moq;
```

- [x] **Step 4: Generate the solution and copy boilerplate**

Run:

```bash
cd /Users/charlie/repos/osmo-video-renamer
dotnet new sln -n OsmoVideoRenamer
dotnet sln OsmoVideoRenamer.sln add OsmoVideoRenamer/OsmoVideoRenamer.csproj OsmoVideoRenamer.UnitTests/OsmoVideoRenamer.UnitTests.csproj
GOPRO=/private/tmp/claude-501/-Users-charlie-repos-osmo-video-renamer/c39be8f0-77d6-4811-8735-caf9e1ad1ae9/scratchpad/gopro-video-renamer
cp "$GOPRO/.gitignore" .gitignore
mkdir -p .github/workflows
cp "$GOPRO/.github/dependabot.yml" .github/dependabot.yml
cp "$GOPRO/.github/workflows/pr-checks.yml" .github/workflows/pr-checks.yml
cp "$GOPRO/.github/workflows/auto-merge-dependabot.yml" .github/workflows/auto-merge-dependabot.yml
```

Expected: `dotnet sln add` prints two "added to the solution" lines. The copied workflow files need no edits (they run `dotnet restore/build/test` on the whole solution).

- [x] **Step 5: Build and verify**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet build`
Expected: `Build succeeded.` with `0 Error(s)`.

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test`
Expected: build succeeds; the test runner reports no tests discovered (a warning, not an error).

- [x] **Step 6: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Scaffold OsmoVideoRenamer solution, test project, and CI boilerplate

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 2: ConsoleWrapper

**Files:**
- Create: `OsmoVideoRenamer/ConsoleWrapping/IConsoleWrapper.cs`
- Create: `OsmoVideoRenamer/ConsoleWrapping/ConsoleWrapper.cs`
- Test: `OsmoVideoRenamer.UnitTests/ConsoleWrapping/ConsoleWrapperTests.cs`

- [x] **Step 1: Write the failing tests**

```csharp
using OsmoVideoRenamer.ConsoleWrapping;

namespace OsmoVideoRenamer.UnitTests.ConsoleWrapping
{
    [TestClass]
    public class ConsoleWrapperTests
    {
        [TestMethod]
        [DoNotParallelize]
        public void ConsoleWrapper_ShouldWriteToConsoleOut()
        {
            string expected = "hello";
            TextWriter original = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);
            try
            {
                var consoleWrapper = new ConsoleWrapper();

                consoleWrapper.WriteLine(expected);

                Assert.AreEqual(expected + Environment.NewLine, writer.ToString());
            }
            finally
            {
                Console.SetOut(original);
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void ConsoleWrapper_ShouldWriteToConsoleError()
        {
            string expected = "hello";
            TextWriter original = Console.Error;
            var writer = new StringWriter();
            Console.SetError(writer);
            try
            {
                var consoleWrapper = new ConsoleWrapper();

                consoleWrapper.WriteErrorLine(expected);

                Assert.AreEqual(expected + Environment.NewLine, writer.ToString());
            }
            finally
            {
                Console.SetError(original);
            }
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~ConsoleWrapperTests"`
Expected: build error `CS0246: The type or namespace name 'ConsoleWrapper' could not be found`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/ConsoleWrapping/IConsoleWrapper.cs`:

```csharp
namespace OsmoVideoRenamer.ConsoleWrapping
{
    public interface IConsoleWrapper
    {
        void WriteErrorLine(string line);
        void WriteLine(string line);
    }
}
```

`OsmoVideoRenamer/ConsoleWrapping/ConsoleWrapper.cs`:

```csharp
namespace OsmoVideoRenamer.ConsoleWrapping
{
    public class ConsoleWrapper : IConsoleWrapper
    {
        public void WriteLine(string line)
        {
            Console.Out.WriteLine(line);
        }

        public void WriteErrorLine(string line)
        {
            Console.Error.WriteLine(line);
        }
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~ConsoleWrapperTests"`
Expected: PASS, 2 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add ConsoleWrapper

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 3: Parameter-logging command filter (copied from GoPro)

**Files:**
- Create: `OsmoVideoRenamer/ParameterLogging/ParameterLoggingCommandFilter.cs`
- Create: `OsmoVideoRenamer/ParameterLogging/ParameterLoggingCommandFilterFactory.cs`
- Test: `OsmoVideoRenamer.UnitTests/ParameterLogging/ParameterLoggingCommandFilterTests.cs`
- Test: `OsmoVideoRenamer.UnitTests/ParameterLogging/ParameterLoggingCommandFilterFactoryTests.cs`

- [x] **Step 1: Write the failing tests**

`OsmoVideoRenamer.UnitTests/ParameterLogging/ParameterLoggingCommandFilterTests.cs`:

```csharp
using Cocona.Command;
using Cocona.CommandLine;
using Cocona.Filters;
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.ParameterLogging;
using System.Collections.Immutable;
using System.Reflection;

namespace OsmoVideoRenamer.UnitTests.ParameterLogging
{
    [TestClass]
    public class ParameterLoggingCommandFilterTests
    {
        private readonly Mock<ILogger<ParameterLoggingCommandFilter>> _logger = new Mock<ILogger<ParameterLoggingCommandFilter>>();

        [TestInitialize]
        public void Setup()
        {
            _logger.Reset();
        }

        [TestMethod]
        public async Task Filter_ShouldLogOptionsAsInformational()
        {
            var filter = new ParameterLoggingCommandFilter(_logger.Object);
            CoconaCommandExecutingContext ctx = GetExecutingContext([
                new Tuple<string, string>("someopt", "25"),
                new Tuple<string, string>("food", "steak")]);
            var next = new Mock<CommandExecutionDelegate>();
            next.Setup(m => m.Invoke(ctx)).ReturnsAsync(1);

            await filter.OnCommandExecutionAsync(ctx, next.Object);

            next.Verify(m => m.Invoke(ctx));
            next.VerifyNoOtherCalls();
            VerifyMessageLoggedInformational("Received option someopt with value 25.");
            VerifyMessageLoggedInformational("Received option food with value steak.");
            _logger.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Filter_ShouldLogNothingIfNoOptionsProvided()
        {
            var filter = new ParameterLoggingCommandFilter(_logger.Object);
            CoconaCommandExecutingContext ctx = GetExecutingContext([]);
            var next = new Mock<CommandExecutionDelegate>();
            next.Setup(m => m.Invoke(ctx)).ReturnsAsync(1);

            await filter.OnCommandExecutionAsync(ctx, next.Object);

            next.Verify(m => m.Invoke(ctx));
            next.VerifyNoOtherCalls();
            _logger.VerifyNoOtherCalls();
        }

        private void VerifyMessageLoggedInformational(string message)
        {
            // https://stackoverflow.com/questions/66307477/how-to-verify-iloggert-log-extension-method-has-been-called-using-moq
            _logger.Verify(
                m => m.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == message && @type.Name == "FormattedLogValues"),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static CoconaCommandExecutingContext GetExecutingContext(Tuple<string, string>[] options) =>
            new CoconaCommandExecutingContext(
                new CommandDescriptor(
                    new Mock<MethodInfo>().Object,
                    null,
                    "descriptor",
                    ImmutableList.Create<string>(),
                    "descriptor_description",
                    ImmutableList.Create<object>(),
                    ImmutableList.Create<ICommandParameterDescriptor>(),
                    ImmutableList.Create<CommandOptionDescriptor>(),
                    ImmutableList.Create<CommandArgumentDescriptor>(),
                    ImmutableList.Create<CommandOverloadDescriptor>(),
                    ImmutableList.Create<CommandOptionLikeCommandDescriptor>(),
                    CommandFlags.None,
                    null),
                new ParsedCommandLine(
                    ImmutableList.CreateRange<CommandOption>(
                            options.Select((Tuple<string, string> o, int i) =>
                                new CommandOption(
                                new CommandOptionDescriptor(
                                    typeof(string),
                                    o.Item1,
                                    o.Item1.ToArray(),
                                    $"the description of the option {o.Item1}",
                                    CoconaDefaultValue.None,
                                    null,
                                    CommandOptionFlags.None,
                                    ImmutableList.Create<Attribute>()),
                                o.Item2,
                                i + 1))),
                    ImmutableList.Create<CommandArgument>(),
                    ImmutableList.Create<string>()),
                null);
    }
}
```

`OsmoVideoRenamer.UnitTests/ParameterLogging/ParameterLoggingCommandFilterFactoryTests.cs`:

```csharp
using Cocona.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.ParameterLogging;

namespace OsmoVideoRenamer.UnitTests.ParameterLogging
{
    [TestClass]
    public class ParameterLoggingCommandFilterFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldCreateFilterOfCorrectType()
        {
            var factory = new ParameterLoggingCommandFilterFactory();
            var logger = new Mock<ILogger<ParameterLoggingCommandFilter>>();
            var services = new ServiceCollection()
                .AddSingleton<ILogger<ParameterLoggingCommandFilter>>(logger.Object)
                .BuildServiceProvider();

            var instance = factory.CreateInstance(services);

            instance.Should().BeAssignableTo<IFilterMetadata>();
            instance.Should().BeAssignableTo<ICommandFilter>();
            instance.Should().BeAssignableTo<ParameterLoggingCommandFilter>();
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~ParameterLogging"`
Expected: build error `CS0246` for `ParameterLoggingCommandFilter`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/ParameterLogging/ParameterLoggingCommandFilter.cs`:

```csharp
using Cocona.Command;
using Cocona.CommandLine;
using Cocona.Filters;
using Microsoft.Extensions.Logging;

namespace OsmoVideoRenamer.ParameterLogging
{
    public class ParameterLoggingCommandFilter : ICommandFilter
    {
        private readonly ILogger<ParameterLoggingCommandFilter> _logger;

        public ParameterLoggingCommandFilter(ILogger<ParameterLoggingCommandFilter> logger)
        {
            _logger = logger;
        }

        public async ValueTask<int> OnCommandExecutionAsync(CoconaCommandExecutingContext ctx, CommandExecutionDelegate next)
        {
            LogCommandArguments(ctx);
            return await next(ctx);
        }

        private void LogCommandArguments(CoconaCommandExecutingContext ctx)
        {
            foreach (CommandOption option in ctx.ParsedCommandLine.Options)
            {
                _logger.LogInformation("Received option {name} with value {value}.",
                    option.Option.Name,
                    option.Value);
            }
        }
    }
}
```

`OsmoVideoRenamer/ParameterLogging/ParameterLoggingCommandFilterFactory.cs`:

```csharp
using Cocona.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OsmoVideoRenamer.ParameterLogging
{
    public class ParameterLoggingCommandFilterFactory : IFilterFactory
    {
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new ParameterLoggingCommandFilter(
                serviceProvider.GetRequiredService<ILogger<ParameterLoggingCommandFilter>>());
        }
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~ParameterLogging"`
Expected: PASS, 3 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add parameter-logging command filter

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 4: DjiVideoFileName parser (spec 5.1)

**Files:**
- Create: `OsmoVideoRenamer/File/Naming/DjiVideoFileName.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/Naming/DjiVideoFileNameTests.cs`

- [x] **Step 1: Write the failing tests**

```csharp
using OsmoVideoRenamer.File.Naming;

namespace OsmoVideoRenamer.UnitTests.File.Naming
{
    [TestClass]
    public class DjiVideoFileNameTests
    {
        [TestMethod]
        public void TryParse_ShouldParseStandardVideoName()
        {
            bool parsed = DjiVideoFileName.TryParse("DJI_20240315123456_0007_D.MP4", out DjiVideoFileName? result);

            parsed.Should().BeTrue();
            result.Should().NotBeNull();
            result!.CaptureTimestamp.Should().Be(new DateTime(2024, 3, 15, 12, 34, 56));
            result.SequenceNumber.Should().Be(7);
        }

        [TestMethod]
        public void TryParse_ShouldAcceptLowerCaseNames()
        {
            bool parsed = DjiVideoFileName.TryParse("dji_20240315123456_0007_d.mp4", out DjiVideoFileName? result);

            parsed.Should().BeTrue();
            result!.SequenceNumber.Should().Be(7);
        }

        [TestMethod]
        public void TryParse_ShouldAcceptMultiLetterSuffix()
        {
            bool parsed = DjiVideoFileName.TryParse("DJI_20240315123456_9999_DX.MP4", out DjiVideoFileName? result);

            parsed.Should().BeTrue();
            result!.SequenceNumber.Should().Be(9999);
        }

        [TestMethod]
        public void TryParse_ShouldAcceptLeapDay()
        {
            bool parsed = DjiVideoFileName.TryParse("DJI_20240229123456_0007_D.MP4", out DjiVideoFileName? result);

            parsed.Should().BeTrue();
            result!.CaptureTimestamp.Should().Be(new DateTime(2024, 2, 29, 12, 34, 56));
        }

        [TestMethod]
        public void TryParse_ShouldReturnFalseForNull()
        {
            bool parsed = DjiVideoFileName.TryParse(null, out DjiVideoFileName? result);

            parsed.Should().BeFalse();
            result.Should().BeNull();
        }

        [TestMethod]
        [DataRow("GH010001.mp4")]
        [DataRow("DJX_20240315123456_0007_D.MP4")]
        [DataRow("DJI_2024031512345_0007_D.MP4")]
        [DataRow("DJI_202403151234567_0007_D.MP4")]
        [DataRow("DJI_20240315123456_007_D.MP4")]
        [DataRow("DJI_20240315123456_00007_D.MP4")]
        [DataRow("DJI_20240315123456_0007_.MP4")]
        [DataRow("DJI_20240315123456_0007_D.LRF")]
        [DataRow("DJI_20240315123456_0007_D.WAV")]
        [DataRow("DJI_20240315123456_0007_D.JPG")]
        [DataRow("DJI_20240315123456_0007_D.DNG")]
        [DataRow("xDJI_20240315123456_0007_D.MP4")]
        [DataRow("DJI_20240315123456_0007_D.MP4.bak")]
        [DataRow("DJI_20241315123456_0007_D.MP4")]
        [DataRow("DJI_20240230123456_0007_D.MP4")]
        [DataRow("DJI_20240315243456_0007_D.MP4")]
        [DataRow("notes.txt")]
        [DataRow("")]
        [DataRow("DJI_20240315123456_0007_D.MP4\n")]
        public void TryParse_ShouldRejectNonVideoNames(string fileName)
        {
            bool parsed = DjiVideoFileName.TryParse(fileName, out DjiVideoFileName? result);

            parsed.Should().BeFalse();
            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_ShouldReturnParsedName()
        {
            DjiVideoFileName result = DjiVideoFileName.Parse("DJI_20240315123456_0007_D.MP4");

            result.CaptureTimestamp.Should().Be(new DateTime(2024, 3, 15, 12, 34, 56));
            result.SequenceNumber.Should().Be(7);
        }

        [TestMethod]
        public void Parse_ShouldThrowForNonVideoName()
        {
            Action act = () => DjiVideoFileName.Parse("notes.txt");

            act.Should().ThrowExactly<ArgumentException>().WithMessage("*notes.txt*");
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~DjiVideoFileNameTests"`
Expected: build error `CS0246` for `DjiVideoFileName`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/Naming/DjiVideoFileName.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OsmoVideoRenamer.File.Naming
{
    /// <summary>
    /// The parsed name of a video recorded by a DJI Osmo Pocket 3, for example
    /// DJI_20240315123456_0007_D.MP4: a 14-digit capture timestamp (yyyyMMddHHmmss),
    /// a 4-digit capture counter, and a letter suffix. This type is the single source
    /// of truth for the pattern.
    /// </summary>
    public sealed record DjiVideoFileName(DateTime CaptureTimestamp, int SequenceNumber)
    {
        public const string TimestampFormat = "yyyyMMddHHmmss";

        private const string MATCHING_FILE_PATTERN = @"^DJI_(?<timestamp>[0-9]{14})_(?<sequence>[0-9]{4})_[A-Z]+\.MP4\z";

        private static readonly Regex _matchingFileRegex = new Regex(
            MATCHING_FILE_PATTERN,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static bool TryParse(string? fileName, [NotNullWhen(true)] out DjiVideoFileName? result)
        {
            result = null;
            if (fileName is null)
            {
                return false;
            }

            Match match = _matchingFileRegex.Match(fileName);
            if (!match.Success)
            {
                return false;
            }

            if (!DateTime.TryParseExact(
                    match.Groups["timestamp"].Value,
                    TimestampFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime captureTimestamp))
            {
                return false;
            }

            int sequenceNumber = int.Parse(match.Groups["sequence"].Value, CultureInfo.InvariantCulture);
            result = new DjiVideoFileName(captureTimestamp, sequenceNumber);
            return true;
        }

        public static DjiVideoFileName Parse(string fileName)
        {
            if (TryParse(fileName, out DjiVideoFileName? result))
            {
                return result;
            }

            throw new ArgumentException($"'{fileName}' is not a DJI Osmo video file name.", nameof(fileName));
        }
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~DjiVideoFileNameTests"`
Expected: PASS, 26 tests (5 + 19 data rows + 2).

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add DjiVideoFileName parser for the Osmo Pocket 3 naming convention

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 5: DirectoryFile and its factory

**Files:**
- Create: `OsmoVideoRenamer/File/DirectoryFiles/Interfaces/IDirectoryFile.cs`
- Create: `OsmoVideoRenamer/File/DirectoryFiles/Interfaces/IDirectoryFileFactory.cs`
- Create: `OsmoVideoRenamer/File/DirectoryFiles/DirectoryFile.cs`
- Create: `OsmoVideoRenamer/File/DirectoryFiles/DirectoryFileFactory.cs`
- Create: `OsmoVideoRenamer.UnitTests/File/DirectoryFileMocking.cs` (shared mock builders; extended in Task 6)
- Test: `OsmoVideoRenamer.UnitTests/File/DirectoryFiles/DirectoryFileTests.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/DirectoryFiles/DirectoryFileFactoryTests.cs`

- [x] **Step 1: Write the mock helper and the failing tests**

`OsmoVideoRenamer.UnitTests/File/DirectoryFileMocking.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.File
{
    internal static class DirectoryFileMocking
    {
        public static Mock<IFileInfo> GetMockedIFileInfo(string fileName)
        {
            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(m => m.Name).Returns(fileName);
            fileInfo.Setup(m => m.Extension).Returns(Path.GetExtension(fileName));
            return fileInfo;
        }

        public static IDirectoryFile GetMockedIDirectoryFile(string fileName)
        {
            var result = new Mock<IDirectoryFile>();
            result.Setup(m => m.Name).Returns(fileName);
            result.Setup(m => m.BaseName).Returns(Path.GetFileNameWithoutExtension(fileName));
            result.Setup(m => m.FileExtension).Returns(Path.GetExtension(fileName));
            result.Setup(m => m.FileInfo).Returns(GetMockedIFileInfo(fileName).Object);
            return result.Object;
        }
    }
}
```

`OsmoVideoRenamer.UnitTests/File/DirectoryFiles/DirectoryFileTests.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.DirectoryFiles
{
    [TestClass]
    public class DirectoryFileTests
    {
        [TestMethod]
        public void DirectoryFile_GivenIFileInfo_HasCorrectProperties()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("DJI_20240315123456_0007_D.LRF");

            var file = new DirectoryFile(fileInfoMock.Object);

            file.FileInfo.Should().BeSameAs(fileInfoMock.Object);
            file.Name.Should().Be("DJI_20240315123456_0007_D.LRF");
            file.BaseName.Should().Be("DJI_20240315123456_0007_D");
            file.FileExtension.Should().Be(".LRF");
        }

        [TestMethod]
        public void DirectoryFile_GivenIDirectoryFile_CopiesFileInfo()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("holiday.MP4");
            var otherMock = new Mock<IDirectoryFile>();
            otherMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);

            var file = new DirectoryFile(otherMock.Object);

            file.FileInfo.Should().BeSameAs(fileInfoMock.Object);
            file.Name.Should().Be("holiday.MP4");
            file.BaseName.Should().Be("holiday");
            file.FileExtension.Should().Be(".MP4");
        }

        [TestMethod]
        public void DirectoryFile_WithoutExtension_HasEmptyExtension()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("system32");

            var file = new DirectoryFile(fileInfoMock.Object);

            file.BaseName.Should().Be("system32");
            file.FileExtension.Should().Be("");
        }
    }
}
```

`OsmoVideoRenamer.UnitTests/File/DirectoryFiles/DirectoryFileFactoryTests.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.DirectoryFiles
{
    [TestClass]
    public class DirectoryFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldInstantiateCorrectType()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("DJI_20240315123456_0007_D.MP4");
            var factory = new DirectoryFileFactory();

            var result = factory.Create(fileInfoMock.Object);

            result.Should().BeAssignableTo<IDirectoryFile>();
            result.FileInfo.Should().BeSameAs(fileInfoMock.Object);
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~DirectoryFile"`
Expected: build error `CS0246` for `IDirectoryFile` / `DirectoryFile`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/DirectoryFiles/Interfaces/IDirectoryFile.cs`:

```csharp
using System.IO.Abstractions;

namespace OsmoVideoRenamer.File.DirectoryFiles.Interfaces
{
    public interface IDirectoryFile
    {
        IFileInfo FileInfo { get; }
        string Name { get; }
        string BaseName { get; }
        string FileExtension { get; }
    }
}
```

`OsmoVideoRenamer/File/DirectoryFiles/Interfaces/IDirectoryFileFactory.cs`:

```csharp
using System.IO.Abstractions;

namespace OsmoVideoRenamer.File.DirectoryFiles.Interfaces
{
    public interface IDirectoryFileFactory
    {
        IDirectoryFile Create(IFileInfo fileInfo);
    }
}
```

`OsmoVideoRenamer/File/DirectoryFiles/DirectoryFile.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.File.DirectoryFiles
{
    public class DirectoryFile : IDirectoryFile
    {
        public IFileInfo FileInfo { get; private init; }

        public string Name => FileInfo.Name;

        public string BaseName => Path.GetFileNameWithoutExtension(FileInfo.Name);

        public string FileExtension => FileInfo.Extension;

        public DirectoryFile(IFileInfo fileInfo)
        {
            FileInfo = fileInfo;
        }

        public DirectoryFile(IDirectoryFile other)
        {
            FileInfo = other.FileInfo;
        }
    }
}
```

`OsmoVideoRenamer/File/DirectoryFiles/DirectoryFileFactory.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.File.DirectoryFiles
{
    public class DirectoryFileFactory : IDirectoryFileFactory
    {
        public IDirectoryFile Create(IFileInfo fileInfo) => new DirectoryFile(fileInfo);
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~DirectoryFile"`
Expected: PASS, 4 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add DirectoryFile wrapper and factory

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 6: VideoFile and its factory

**Files:**
- Create: `OsmoVideoRenamer/File/VideoFiles/Interfaces/IVideoFile.cs`
- Create: `OsmoVideoRenamer/File/VideoFiles/Interfaces/IVideoFileFactory.cs`
- Create: `OsmoVideoRenamer/File/VideoFiles/VideoFile.cs`
- Create: `OsmoVideoRenamer/File/VideoFiles/VideoFileFactory.cs`
- Modify: `OsmoVideoRenamer.UnitTests/File/DirectoryFileMocking.cs` (add `GetMockedIVideoFile`)
- Test: `OsmoVideoRenamer.UnitTests/File/VideoFiles/VideoFileTests.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/VideoFiles/VideoFileFactoryTests.cs`

- [x] **Step 1: Extend the mock helper and write the failing tests**

Add to `DirectoryFileMocking` (new `using OsmoVideoRenamer.File.VideoFiles.Interfaces;` at the top, new method inside the class):

```csharp
        public static IVideoFile GetMockedIVideoFile(string fileName, int sequenceNumber, DateTime captureTimestamp)
        {
            var result = new Mock<IVideoFile>();
            result.Setup(m => m.Name).Returns(fileName);
            result.Setup(m => m.BaseName).Returns(Path.GetFileNameWithoutExtension(fileName));
            result.Setup(m => m.FileExtension).Returns(Path.GetExtension(fileName));
            result.Setup(m => m.SequenceNumber).Returns(sequenceNumber);
            result.Setup(m => m.CaptureTimestamp).Returns(captureTimestamp);
            result.Setup(m => m.FileInfo).Returns(GetMockedIFileInfo(fileName).Object);
            return result.Object;
        }
```

`OsmoVideoRenamer.UnitTests/File/VideoFiles/VideoFileTests.cs`:

```csharp
using OsmoVideoRenamer.File.VideoFiles;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles
{
    [TestClass]
    public class VideoFileTests
    {
        [TestMethod]
        public void VideoFile_GivenIDirectoryFile_ParsesNameAndCopiesFileInfo()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4");

            var file = new VideoFile(directoryFile);

            file.FileInfo.Should().BeSameAs(directoryFile.FileInfo);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.BaseName.Should().Be("DJI_20240315123456_0007_D");
            file.FileExtension.Should().Be(".MP4");
            file.CaptureTimestamp.Should().Be(new DateTime(2024, 3, 15, 12, 34, 56));
            file.SequenceNumber.Should().Be(7);
        }

        [TestMethod]
        public void VideoFile_GivenNonDjiName_Throws()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("notes.txt");

            Action act = () => new VideoFile(directoryFile);

            act.Should().ThrowExactly<ArgumentException>();
        }

        [TestMethod]
        public void VideoFile_GivenIVideoFile_CopiesParsedValuesWithoutReparsing()
        {
            var other = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 42, new DateTime(2020, 1, 2, 3, 4, 5));

            var file = new VideoFile(other);

            file.FileInfo.Should().BeSameAs(other.FileInfo);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.SequenceNumber.Should().Be(42);
            file.CaptureTimestamp.Should().Be(new DateTime(2020, 1, 2, 3, 4, 5));
        }
    }
}
```

`OsmoVideoRenamer.UnitTests/File/VideoFiles/VideoFileFactoryTests.cs`:

```csharp
using OsmoVideoRenamer.File.VideoFiles;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles
{
    [TestClass]
    public class VideoFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldInstantiateCorrectType()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4");
            var factory = new VideoFileFactory();

            var result = factory.Create(directoryFile);

            result.Should().BeAssignableTo<IVideoFile>();
            result.SequenceNumber.Should().Be(7);
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~VideoFileTests|FullyQualifiedName~VideoFileFactoryTests"`
Expected: build error `CS0246` for `IVideoFile` / `VideoFile`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/VideoFiles/Interfaces/IVideoFile.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Interfaces
{
    public interface IVideoFile : IDirectoryFile
    {
        DateTime CaptureTimestamp { get; }
        int SequenceNumber { get; }
    }
}
```

`OsmoVideoRenamer/File/VideoFiles/Interfaces/IVideoFileFactory.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Interfaces
{
    public interface IVideoFileFactory
    {
        IVideoFile Create(IDirectoryFile file);
    }
}
```

`OsmoVideoRenamer/File/VideoFiles/VideoFile.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Naming;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles
{
    public class VideoFile : DirectoryFile, IVideoFile
    {
        public DateTime CaptureTimestamp { get; private init; }

        public int SequenceNumber { get; private init; }

        public VideoFile(IDirectoryFile file) : base(file)
        {
            DjiVideoFileName parsedName = DjiVideoFileName.Parse(file.Name);
            CaptureTimestamp = parsedName.CaptureTimestamp;
            SequenceNumber = parsedName.SequenceNumber;
        }

        public VideoFile(IVideoFile other) : base(other)
        {
            CaptureTimestamp = other.CaptureTimestamp;
            SequenceNumber = other.SequenceNumber;
        }
    }
}
```

`OsmoVideoRenamer/File/VideoFiles/VideoFileFactory.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles
{
    public class VideoFileFactory : IVideoFileFactory
    {
        public IVideoFile Create(IDirectoryFile file) => new VideoFile(file);
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~VideoFileTests|FullyQualifiedName~VideoFileFactoryTests"`
Expected: PASS, 4 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add VideoFile with parsed DJI counter and timestamp

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 7: NumberedVideoFile and its factory

**Files:**
- Create: `OsmoVideoRenamer/File/VideoFiles/Numbered/Interfaces/INumberedVideoFile.cs`
- Create: `OsmoVideoRenamer/File/VideoFiles/Numbered/Interfaces/INumberedVideoFileFactory.cs`
- Create: `OsmoVideoRenamer/File/VideoFiles/Numbered/NumberedVideoFile.cs`
- Create: `OsmoVideoRenamer/File/VideoFiles/Numbered/NumberedVideoFileFactory.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/VideoFiles/Numbered/NumberedVideoFileTests.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/VideoFiles/Numbered/NumberedVideoFileFactoryTests.cs`

- [x] **Step 1: Write the failing tests**

`OsmoVideoRenamer.UnitTests/File/VideoFiles/Numbered/NumberedVideoFileTests.cs`:

```csharp
using OsmoVideoRenamer.File.VideoFiles.Numbered;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles.Numbered
{
    [TestClass]
    public class NumberedVideoFileTests
    {
        private static readonly DateTime _timestamp = new DateTime(2020, 1, 2, 3, 4, 5);

        [TestMethod]
        public void NumberedVideoFile_GivenIVideoFile_HasCorrectProperties()
        {
            var videoFile = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 42, _timestamp);

            var file = new NumberedVideoFile(9, videoFile);

            file.NewIndex.Should().Be(9);
            file.FileInfo.Should().BeSameAs(videoFile.FileInfo);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.BaseName.Should().Be("DJI_20240315123456_0007_D");
            file.FileExtension.Should().Be(".MP4");
            file.SequenceNumber.Should().Be(42);
            file.CaptureTimestamp.Should().Be(_timestamp);
        }

        [TestMethod]
        public void NumberedVideoFile_GivenINumberedVideoFile_HasCorrectProperties()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("DJI_20240315123456_0007_D.MP4");
            var numberedMock = new Mock<INumberedVideoFile>();
            numberedMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);
            numberedMock.Setup(m => m.SequenceNumber).Returns(42);
            numberedMock.Setup(m => m.CaptureTimestamp).Returns(_timestamp);
            numberedMock.Setup(m => m.NewIndex).Returns(10);

            var file = new NumberedVideoFile(numberedMock.Object);

            file.NewIndex.Should().Be(10);
            file.FileInfo.Should().BeSameAs(fileInfoMock.Object);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.FileExtension.Should().Be(".MP4");
            file.SequenceNumber.Should().Be(42);
            file.CaptureTimestamp.Should().Be(_timestamp);
        }
    }
}
```

`OsmoVideoRenamer.UnitTests/File/VideoFiles/Numbered/NumberedVideoFileFactoryTests.cs`:

```csharp
using OsmoVideoRenamer.File.VideoFiles.Numbered;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles.Numbered
{
    [TestClass]
    public class NumberedVideoFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldCreateCorrectType()
        {
            var videoFile = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, new DateTime(2024, 3, 15, 12, 34, 56));
            var factory = new NumberedVideoFileFactory();

            var result = factory.Create(1, videoFile);

            result.Should().BeAssignableTo<INumberedVideoFile>();
            result.NewIndex.Should().Be(1);
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~NumberedVideoFile"`
Expected: build error `CS0246` for `INumberedVideoFile` / `NumberedVideoFile`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/VideoFiles/Numbered/Interfaces/INumberedVideoFile.cs`:

```csharp
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces
{
    public interface INumberedVideoFile : IVideoFile
    {
        int NewIndex { get; }
    }
}
```

`OsmoVideoRenamer/File/VideoFiles/Numbered/Interfaces/INumberedVideoFileFactory.cs`:

```csharp
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces
{
    public interface INumberedVideoFileFactory
    {
        INumberedVideoFile Create(int newIndex, IVideoFile file);
    }
}
```

`OsmoVideoRenamer/File/VideoFiles/Numbered/NumberedVideoFile.cs`:

```csharp
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Numbered
{
    public class NumberedVideoFile : VideoFile, INumberedVideoFile
    {
        public int NewIndex { get; private init; }

        public NumberedVideoFile(int newIndex, IVideoFile file) : base(file)
        {
            NewIndex = newIndex;
        }

        public NumberedVideoFile(INumberedVideoFile other) : this(other.NewIndex, other) { }
    }
}
```

`OsmoVideoRenamer/File/VideoFiles/Numbered/NumberedVideoFileFactory.cs`:

```csharp
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Numbered
{
    public class NumberedVideoFileFactory : INumberedVideoFileFactory
    {
        public INumberedVideoFile Create(int newIndex, IVideoFile file) => new NumberedVideoFile(newIndex, file);
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~NumberedVideoFile"`
Expected: PASS, 3 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add NumberedVideoFile and factory

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 8: RenamedCompanionFile and its factory

**Files:**
- Create: `OsmoVideoRenamer/File/CompanionFiles/Interfaces/IRenamedCompanionFile.cs`
- Create: `OsmoVideoRenamer/File/CompanionFiles/Interfaces/IRenamedCompanionFileFactory.cs`
- Create: `OsmoVideoRenamer/File/CompanionFiles/RenamedCompanionFile.cs`
- Create: `OsmoVideoRenamer/File/CompanionFiles/RenamedCompanionFileFactory.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/CompanionFiles/RenamedCompanionFileTests.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/CompanionFiles/RenamedCompanionFileFactoryTests.cs`

- [x] **Step 1: Write the failing tests**

`OsmoVideoRenamer.UnitTests/File/CompanionFiles/RenamedCompanionFileTests.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.File.CompanionFiles
{
    [TestClass]
    public class RenamedCompanionFileTests
    {
        [TestMethod]
        public void RenamedCompanionFile_GivenIDirectoryFile_HasCorrectProperties()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.LRF");

            var file = new RenamedCompanionFile("Trip - 001.LRF", directoryFile);

            file.FileInfo.Should().BeSameAs(directoryFile.FileInfo);
            file.Name.Should().Be("DJI_20240315123456_0007_D.LRF");
            file.BaseName.Should().Be("DJI_20240315123456_0007_D");
            file.FileExtension.Should().Be(".LRF");
            file.NewName.Should().Be("Trip - 001.LRF");
        }

        [TestMethod]
        public void RenamedCompanionFile_CommitRenameToDisk_MovesFileWithinItsDirectory()
        {
            var directoryInfoMock = new Mock<IDirectoryInfo>();
            directoryInfoMock.Setup(m => m.FullName).Returns("Some directory full name");
            var fileInfoMock = new Mock<IFileInfo>();
            fileInfoMock.Setup(m => m.Directory).Returns(directoryInfoMock.Object);
            var directoryFileMock = new Mock<IDirectoryFile>();
            directoryFileMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);
            var file = new RenamedCompanionFile("Trip - 001.LRF", directoryFileMock.Object);

            file.CommitRenameToDisk();

            directoryInfoMock.Verify(m => m.FullName, Times.Once());
            fileInfoMock.Verify(m => m.MoveTo(Path.Combine("Some directory full name", "Trip - 001.LRF")), Times.Once());
            directoryInfoMock.VerifyNoOtherCalls();
            fileInfoMock.VerifyNoOtherCalls();
        }
    }
}
```

Note: the last two `VerifyNoOtherCalls()` lines mirror the GoPro test; Moq treats the `Directory` getter (which returns the verified inner mock) as covered. If Moq ever reports `IFileInfo.Directory` as an unverified invocation, add `fileInfoMock.Verify(m => m.Directory, Times.Once());` before `fileInfoMock.VerifyNoOtherCalls();`.

`OsmoVideoRenamer.UnitTests/File/CompanionFiles/RenamedCompanionFileFactoryTests.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.CompanionFiles
{
    [TestClass]
    public class RenamedCompanionFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldProduceCorrectType()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.WAV");
            var factory = new RenamedCompanionFileFactory();

            var result = factory.Create("Trip - 001.WAV", directoryFile);

            result.Should().BeAssignableTo<IRenamedCompanionFile>();
            result.NewName.Should().Be("Trip - 001.WAV");
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~RenamedCompanionFile"`
Expected: build error `CS0246` for `RenamedCompanionFile`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/CompanionFiles/Interfaces/IRenamedCompanionFile.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.CompanionFiles.Interfaces
{
    public interface IRenamedCompanionFile : IDirectoryFile
    {
        string NewName { get; }

        void CommitRenameToDisk();
    }
}
```

`OsmoVideoRenamer/File/CompanionFiles/Interfaces/IRenamedCompanionFileFactory.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.CompanionFiles.Interfaces
{
    public interface IRenamedCompanionFileFactory
    {
        IRenamedCompanionFile Create(string newName, IDirectoryFile file);
    }
}
```

`OsmoVideoRenamer/File/CompanionFiles/RenamedCompanionFile.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.CompanionFiles
{
    public class RenamedCompanionFile : DirectoryFile, IRenamedCompanionFile
    {
        public string NewName { get; private init; }

        public RenamedCompanionFile(string newName, IDirectoryFile file) : base(file)
        {
            NewName = newName;
        }

        public void CommitRenameToDisk()
        {
            FileInfo.MoveTo(Path.Combine(FileInfo.Directory!.FullName, NewName));
        }
    }
}
```

`OsmoVideoRenamer/File/CompanionFiles/RenamedCompanionFileFactory.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.CompanionFiles
{
    public class RenamedCompanionFileFactory : IRenamedCompanionFileFactory
    {
        public IRenamedCompanionFile Create(string newName, IDirectoryFile file) => new RenamedCompanionFile(newName, file);
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~RenamedCompanionFile"`
Expected: PASS, 3 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add RenamedCompanionFile for LRF and WAV companions

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 9: RenamedVideoFile and its factory

**Files:**
- Create: `OsmoVideoRenamer/File/VideoFiles/Renamed/Interfaces/IRenamedVideoFile.cs`
- Create: `OsmoVideoRenamer/File/VideoFiles/Renamed/Interfaces/IRenamedVideoFileFactory.cs`
- Create: `OsmoVideoRenamer/File/VideoFiles/Renamed/RenamedVideoFile.cs`
- Create: `OsmoVideoRenamer/File/VideoFiles/Renamed/RenamedVideoFileFactory.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/VideoFiles/Renamed/RenamedVideoFileTests.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/VideoFiles/Renamed/RenamedVideoFileFactoryTests.cs`

- [x] **Step 1: Write the failing tests**

`OsmoVideoRenamer.UnitTests/File/VideoFiles/Renamed/RenamedVideoFileTests.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles.Renamed
{
    [TestClass]
    public class RenamedVideoFileTests
    {
        private static readonly DateTime _timestamp = new DateTime(2020, 1, 2, 3, 4, 5);

        [TestMethod]
        public void RenamedVideoFile_GivenINumberedVideoFile_HasCorrectProperties()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("DJI_20240315123456_0007_D.MP4");
            var numberedMock = new Mock<INumberedVideoFile>();
            numberedMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);
            numberedMock.Setup(m => m.SequenceNumber).Returns(42);
            numberedMock.Setup(m => m.CaptureTimestamp).Returns(_timestamp);
            numberedMock.Setup(m => m.NewIndex).Returns(10);
            var companion = new Mock<IRenamedCompanionFile>().Object;
            var companions = new List<IRenamedCompanionFile> { companion };

            var file = new RenamedVideoFile("new file 5.MP4", companions, numberedMock.Object);

            file.NewIndex.Should().Be(10);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.FileExtension.Should().Be(".MP4");
            file.SequenceNumber.Should().Be(42);
            file.CaptureTimestamp.Should().Be(_timestamp);
            file.NewName.Should().Be("new file 5.MP4");
            file.Companions.Should().Equal(companion);
        }

        [TestMethod]
        public void RenamedVideoFile_CommitRenameToDisk_MovesOnlyTheVideoWithinItsDirectory()
        {
            var directoryInfoMock = new Mock<IDirectoryInfo>();
            directoryInfoMock.Setup(m => m.FullName).Returns("Some directory full name");
            var fileInfoMock = new Mock<IFileInfo>();
            fileInfoMock.Setup(m => m.Directory).Returns(directoryInfoMock.Object);
            var numberedMock = new Mock<INumberedVideoFile>();
            numberedMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);
            var companionMock = new Mock<IRenamedCompanionFile>();
            var file = new RenamedVideoFile("new file 5.MP4", new List<IRenamedCompanionFile> { companionMock.Object }, numberedMock.Object);

            file.CommitRenameToDisk();

            directoryInfoMock.Verify(m => m.FullName, Times.Once());
            fileInfoMock.Verify(m => m.MoveTo(Path.Combine("Some directory full name", "new file 5.MP4")), Times.Once());
            directoryInfoMock.VerifyNoOtherCalls();
            fileInfoMock.VerifyNoOtherCalls();
            companionMock.VerifyNoOtherCalls();
        }
    }
}
```

(Same Moq note as Task 8 applies to `fileInfoMock.VerifyNoOtherCalls()`.)

`OsmoVideoRenamer.UnitTests/File/VideoFiles/Renamed/RenamedVideoFileFactoryTests.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles.Renamed
{
    [TestClass]
    public class RenamedVideoFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldProduceCorrectType()
        {
            var numberedMock = new Mock<INumberedVideoFile>();
            var companions = new List<IRenamedCompanionFile>();
            var factory = new RenamedVideoFileFactory();

            var result = factory.Create("some new name", companions, numberedMock.Object);

            result.Should().BeAssignableTo<IRenamedVideoFile>();
            result.NewName.Should().Be("some new name");
            result.Companions.Should().BeSameAs(companions);
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~RenamedVideoFile"`
Expected: build error `CS0246` for `RenamedVideoFile`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/VideoFiles/Renamed/Interfaces/IRenamedVideoFile.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces
{
    public interface IRenamedVideoFile : INumberedVideoFile
    {
        string NewName { get; }

        IReadOnlyList<IRenamedCompanionFile> Companions { get; }

        void CommitRenameToDisk();
    }
}
```

`OsmoVideoRenamer/File/VideoFiles/Renamed/Interfaces/IRenamedVideoFileFactory.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces
{
    public interface IRenamedVideoFileFactory
    {
        IRenamedVideoFile Create(string newName, IReadOnlyList<IRenamedCompanionFile> companions, INumberedVideoFile file);
    }
}
```

`OsmoVideoRenamer/File/VideoFiles/Renamed/RenamedVideoFile.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Renamed
{
    public class RenamedVideoFile : NumberedVideoFile, IRenamedVideoFile
    {
        public string NewName { get; private init; }

        public IReadOnlyList<IRenamedCompanionFile> Companions { get; private init; }

        public RenamedVideoFile(string newName, IReadOnlyList<IRenamedCompanionFile> companions, INumberedVideoFile file) : base(file)
        {
            NewName = newName;
            Companions = companions;
        }

        public void CommitRenameToDisk()
        {
            FileInfo.MoveTo(Path.Combine(FileInfo.Directory!.FullName, NewName));
        }
    }
}
```

`OsmoVideoRenamer/File/VideoFiles/Renamed/RenamedVideoFileFactory.cs`:

```csharp
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Renamed
{
    public class RenamedVideoFileFactory : IRenamedVideoFileFactory
    {
        public IRenamedVideoFile Create(string newName, IReadOnlyList<IRenamedCompanionFile> companions, INumberedVideoFile file) =>
            new RenamedVideoFile(newName, companions, file);
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~RenamedVideoFile"`
Expected: PASS, 3 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add RenamedVideoFile with companions and rename action

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 10: CompanionFileFinder (spec 5.4)

**Files:**
- Create: `OsmoVideoRenamer/File/Interfaces/ICompanionFileFinder.cs`
- Create: `OsmoVideoRenamer/File/CompanionFileFinder.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/CompanionFileFinderTests.cs`

- [x] **Step 1: Write the failing tests**

```csharp
using OsmoVideoRenamer.File;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class CompanionFileFinderTests
    {
        private static readonly DateTime _timestamp = new DateTime(2024, 3, 15, 12, 34, 56);

        [TestMethod]
        public void Finder_ShouldReturnLrfAndWavWithSameBaseName_InDirectoryOrder()
        {
            var video = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, _timestamp);
            var videoEntry = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4");
            var lrf = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.LRF");
            var wav = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.WAV");
            var otherLrf = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.LRF");
            var finder = new CompanionFileFinder();

            var result = finder.GetCompanions(video, [videoEntry, wav, otherLrf, lrf]);

            result.Should().Equal(wav, lrf);
        }

        [TestMethod]
        public void Finder_ShouldMatchBaseNameAndExtensionCaseInsensitively()
        {
            var video = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, _timestamp);
            var lrf = DirectoryFileMocking.GetMockedIDirectoryFile("dji_20240315123456_0007_d.lrf");
            var wav = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.wav");
            var finder = new CompanionFileFinder();

            var result = finder.GetCompanions(video, [lrf, wav]);

            result.Should().Equal(lrf, wav);
        }

        [TestMethod]
        public void Finder_ShouldIgnoreOtherExtensionsAndOtherBaseNames()
        {
            var video = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, _timestamp);
            var entries = new[]
            {
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.JPG"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.DNG"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.SRT"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.LRF"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.WAV"),
                DirectoryFileMocking.GetMockedIDirectoryFile("notes.txt"),
            };
            var finder = new CompanionFileFinder();

            var result = finder.GetCompanions(video, entries);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Finder_ShouldReturnEmptyForEmptyDirectory()
        {
            var video = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, _timestamp);
            var finder = new CompanionFileFinder();

            var result = finder.GetCompanions(video, []);

            result.Should().BeEmpty();
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~CompanionFileFinderTests"`
Expected: build error `CS0246` for `CompanionFileFinder`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/Interfaces/ICompanionFileFinder.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.Interfaces
{
    public interface ICompanionFileFinder
    {
        IEnumerable<IDirectoryFile> GetCompanions(IVideoFile video, IEnumerable<IDirectoryFile> allFiles);
    }
}
```

`OsmoVideoRenamer/File/CompanionFileFinder.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File
{
    /// <summary>
    /// Finds the files DJI writes next to a video: the .LRF low-resolution proxy and the
    /// optional .WAV audio backup. Both share the video's base name.
    /// </summary>
    public class CompanionFileFinder : ICompanionFileFinder
    {
        private static readonly string[] COMPANION_EXTENSIONS = [".LRF", ".WAV"];

        public IEnumerable<IDirectoryFile> GetCompanions(IVideoFile video, IEnumerable<IDirectoryFile> allFiles) =>
            allFiles.Where(file =>
                string.Equals(file.BaseName, video.BaseName, StringComparison.OrdinalIgnoreCase)
                && COMPANION_EXTENSIONS.Contains(file.FileExtension, StringComparer.OrdinalIgnoreCase));
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~CompanionFileFinderTests"`
Expected: PASS, 4 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add CompanionFileFinder for LRF and WAV files

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 11: FileFilter (spec 5.1)

**Files:**
- Create: `OsmoVideoRenamer/File/Interfaces/IFileFilter.cs`
- Create: `OsmoVideoRenamer/File/FileFilter.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/FileFilterTests.cs`

- [x] **Step 1: Write the failing tests**

```csharp
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class FileFilterTests
    {
        private readonly Mock<IVideoFileFactory> _videoFileFactoryMock = new Mock<IVideoFileFactory>();

        [TestInitialize]
        public void Setup()
        {
            _videoFileFactoryMock.Reset();
        }

        [TestMethod]
        public void FileFilter_ShouldReturnDjiVideosCreatedByFactory_InDirectoryOrder()
        {
            var entry1 = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4");
            var entry2 = DirectoryFileMocking.GetMockedIDirectoryFile("dji_20240315123500_0008_d.mp4");
            var video1 = new Mock<IVideoFile>().Object;
            var video2 = new Mock<IVideoFile>().Object;
            _videoFileFactoryMock.Setup(m => m.Create(entry1)).Returns(video1);
            _videoFileFactoryMock.Setup(m => m.Create(entry2)).Returns(video2);
            var filter = new FileFilter(_videoFileFactoryMock.Object);

            var result = filter.GetMatchingVideos([entry1, entry2]).ToList();

            result.Should().Equal(video1, video2);
            _videoFileFactoryMock.Verify(m => m.Create(entry1), Times.Once());
            _videoFileFactoryMock.Verify(m => m.Create(entry2), Times.Once());
            _videoFileFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileFilter_ShouldIgnoreCompanionsPhotosAndOtherFiles()
        {
            var entries = new[]
            {
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.LRF"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.WAV"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.JPG"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.DNG"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20241315123456_0009_D.MP4"),
                DirectoryFileMocking.GetMockedIDirectoryFile("GH010001.mp4"),
                DirectoryFileMocking.GetMockedIDirectoryFile("other_video.mp4"),
                DirectoryFileMocking.GetMockedIDirectoryFile("never gonna give you up.mp3"),
                DirectoryFileMocking.GetMockedIDirectoryFile("notes.txt"),
                DirectoryFileMocking.GetMockedIDirectoryFile("system32"),
            };
            var filter = new FileFilter(_videoFileFactoryMock.Object);

            var result = filter.GetMatchingVideos(entries).ToList();

            result.Should().BeEmpty();
            _videoFileFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileFilter_ShouldReturnEmptyForEmptyInput()
        {
            var filter = new FileFilter(_videoFileFactoryMock.Object);

            var result = filter.GetMatchingVideos([]).ToList();

            result.Should().BeEmpty();
            _videoFileFactoryMock.VerifyNoOtherCalls();
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~FileFilterTests"`
Expected: build error `CS0246` for `FileFilter`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/Interfaces/IFileFilter.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.Interfaces
{
    public interface IFileFilter
    {
        IEnumerable<IVideoFile> GetMatchingVideos(IEnumerable<IDirectoryFile> allFiles);
    }
}
```

`OsmoVideoRenamer/File/FileFilter.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.Naming;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File
{
    public class FileFilter : IFileFilter
    {
        private readonly IVideoFileFactory _videoFileFactory;

        public FileFilter(IVideoFileFactory videoFileFactory)
        {
            _videoFileFactory = videoFileFactory;
        }

        public IEnumerable<IVideoFile> GetMatchingVideos(IEnumerable<IDirectoryFile> allFiles) =>
            allFiles
                .Where(file => DjiVideoFileName.TryParse(file.Name, out _))
                .Select(file => _videoFileFactory.Create(file));
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~FileFilterTests"`
Expected: PASS, 3 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add FileFilter that recognises DJI Osmo videos

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 12: Logger test helper and FileSort (spec 5.2)

**Files:**
- Create: `OsmoVideoRenamer.UnitTests/Logging/LoggerMockExtensions.cs`
- Create: `OsmoVideoRenamer/File/Interfaces/IFileSort.cs`
- Create: `OsmoVideoRenamer/File/FileSort.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/FileSortTests.cs`

- [x] **Step 1: Write the logger helper and the failing tests**

`OsmoVideoRenamer.UnitTests/Logging/LoggerMockExtensions.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace OsmoVideoRenamer.UnitTests.Logging
{
    /// <summary>
    /// ILogger extension methods (LogInformation etc.) all funnel into ILogger.Log with a
    /// FormattedLogValues state, so that is what gets verified.
    /// https://stackoverflow.com/questions/66307477/how-to-verify-iloggert-log-extension-method-has-been-called-using-moq
    /// </summary>
    internal static class LoggerMockExtensions
    {
        public static void VerifyLogged<T>(this Mock<ILogger<T>> logger, LogLevel level, Times times)
        {
            logger.Verify(
                m => m.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((@object, @type) => @type.Name == "FormattedLogValues"),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }
    }
}
```

`OsmoVideoRenamer.UnitTests/File/FileSortTests.cs`:

```csharp
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.UnitTests.Logging;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class FileSortTests
    {
        private readonly Mock<ILogger<FileSort>> _loggerMock = new Mock<ILogger<FileSort>>();
        private readonly Mock<INumberedVideoFileFactory> _numberedFactoryMock = new Mock<INumberedVideoFileFactory>();

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
            _numberedFactoryMock.Reset();
        }

        [TestMethod]
        public void FileSort_ShouldOrderBySequenceNumber()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var file2 = Video("DJI_20240315120100_0002_D.MP4", 2, Time(12, 1));
            var file3 = Video("DJI_20240315120200_0003_D.MP4", 3, Time(12, 2));
            var file4 = Video("DJI_20240315120300_0010_D.MP4", 10, Time(12, 3));
            var numbered1 = SetupNumbered(1, file1);
            var numbered2 = SetupNumbered(2, file2);
            var numbered3 = SetupNumbered(3, file3);
            var numbered4 = SetupNumbered(4, file4);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file3, file1, file4, file2], null);

            result.Should().Equal(numbered1, numbered2, numbered3, numbered4);
            _loggerMock.VerifyLogged(LogLevel.Warning, Times.Never());
        }

        [TestMethod]
        public void FileSort_ShouldUseStartingNumber()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var file2 = Video("DJI_20240315120100_0002_D.MP4", 2, Time(12, 1));
            var numbered5 = SetupNumbered(5, file1);
            var numbered6 = SetupNumbered(6, file2);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file2, file1], 5);

            result.Should().Equal(numbered5, numbered6);
        }

        [TestMethod]
        public void FileSort_ShouldAllowStartingNumberZero()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var file2 = Video("DJI_20240315120100_0002_D.MP4", 2, Time(12, 1));
            var numbered0 = SetupNumbered(0, file1);
            var numbered1 = SetupNumbered(1, file2);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file1, file2], 0);

            result.Should().Equal(numbered0, numbered1);
        }

        [TestMethod]
        public void FileSort_ShouldThrowForNegativeStartingNumber()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            Action act = () => sort.GetOrderedFiles([file1], -1);

            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("startingNumber");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
            _numberedFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileSort_ShouldOrderBySequenceAndWarn_WhenTimestampGoesBackwards()
        {
            var file1 = Video("DJI_20240315150000_0001_D.MP4", 1, Time(15, 0));
            var file2 = Video("DJI_20240315090000_0002_D.MP4", 2, Time(9, 0));
            var numbered1 = SetupNumbered(1, file1);
            var numbered2 = SetupNumbered(2, file2);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file2, file1], null);

            result.Should().Equal(numbered1, numbered2);
            _loggerMock.VerifyLogged(LogLevel.Warning, Times.Once());
        }

        [TestMethod]
        public void FileSort_ShouldNotWarn_WhenTimestampsAreEqual()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var file2 = Video("DJI_20240315120000_0002_D.MP4", 2, Time(12, 0));
            var file3 = Video("DJI_20240315120000_0003_D.MP4", 3, Time(12, 0));
            var numbered1 = SetupNumbered(1, file1);
            var numbered2 = SetupNumbered(2, file2);
            var numbered3 = SetupNumbered(3, file3);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file3, file2, file1], null);

            result.Should().Equal(numbered1, numbered2, numbered3);
            _loggerMock.VerifyLogged(LogLevel.Warning, Times.Never());
        }

        [TestMethod]
        public void FileSort_ShouldWarnOnlyOnce_WhenTimestampsGoBackwardsMoreThanOnce()
        {
            var file1 = Video("DJI_20240315150000_0001_D.MP4", 1, Time(15, 0));
            var file2 = Video("DJI_20240315090000_0002_D.MP4", 2, Time(9, 0));
            var file3 = Video("DJI_20240315080000_0003_D.MP4", 3, Time(8, 0));
            var file4 = Video("DJI_20240315200000_0004_D.MP4", 4, Time(20, 0));
            var numbered1 = SetupNumbered(1, file1);
            var numbered2 = SetupNumbered(2, file2);
            var numbered3 = SetupNumbered(3, file3);
            var numbered4 = SetupNumbered(4, file4);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file4, file3, file2, file1], null);

            result.Should().Equal(numbered1, numbered2, numbered3, numbered4);
            _loggerMock.VerifyLogged(LogLevel.Warning, Times.Once());
        }

        [TestMethod]
        public void FileSort_ShouldThrowForDuplicateSequenceNumbers()
        {
            var file1 = Video("DJI_20240315090000_0003_D.MP4", 3, Time(9, 0));
            var file2 = Video("DJI_20240315140000_0003_D.MP4", 3, Time(14, 0));
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            Action act = () => sort.GetOrderedFiles([file1, file2], null);

            act.Should().ThrowExactly<ArgumentException>().WithMessage("*3*");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
            _numberedFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileSort_ShouldReturnEmptyForEmptyInput()
        {
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([], null);

            result.Should().BeEmpty();
            _numberedFactoryMock.VerifyNoOtherCalls();
        }

        private static DateTime Time(int hour, int minute) => new DateTime(2024, 3, 15, hour, minute, 0);

        private static IVideoFile Video(string name, int sequenceNumber, DateTime timestamp) =>
            DirectoryFileMocking.GetMockedIVideoFile(name, sequenceNumber, timestamp);

        private INumberedVideoFile SetupNumbered(int newIndex, IVideoFile file)
        {
            var numbered = new Mock<INumberedVideoFile>().Object;
            _numberedFactoryMock.Setup(m => m.Create(newIndex, file)).Returns(numbered);
            return numbered;
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~FileSortTests"`
Expected: build error `CS0246` for `FileSort`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/Interfaces/IFileSort.cs`:

```csharp
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File.Interfaces
{
    public interface IFileSort
    {
        List<INumberedVideoFile> GetOrderedFiles(IEnumerable<IVideoFile> files, int? startingNumber);
    }
}
```

`OsmoVideoRenamer/File/FileSort.cs`:

```csharp
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File
{
    /// <summary>
    /// Orders videos by the camera's capture counter (the NNNN in DJI_yyyyMMddHHmmss_NNNN_D.MP4).
    /// The timestamp is deliberately not used for ordering: the camera clock can be wrong or
    /// change time zone mid-trip, while the counter always reflects recording order.
    /// </summary>
    public class FileSort : IFileSort
    {
        private readonly ILogger<FileSort> _logger;
        private readonly INumberedVideoFileFactory _numberedVideoFileFactory;

        public FileSort(ILogger<FileSort> logger, INumberedVideoFileFactory numberedVideoFileFactory)
        {
            _logger = logger;
            _numberedVideoFileFactory = numberedVideoFileFactory;
        }

        public List<INumberedVideoFile> GetOrderedFiles(IEnumerable<IVideoFile> files, int? startingNumber)
        {
            int firstNumber = startingNumber ?? 1;
            VerifyStartingNumberIsNotNegative(firstNumber);
            List<IVideoFile> ordered = files.OrderBy(file => file.SequenceNumber).ToList();
            VerifyNoDuplicateSequenceNumbers(ordered);
            WarnIfTimestampsDisagreeWithSequenceOrder(ordered);
            return ordered
                .Select((file, i) => _numberedVideoFileFactory.Create(i + firstNumber, file))
                .ToList();
        }

        private void VerifyStartingNumberIsNotNegative(int startingNumber)
        {
            if (startingNumber < 0)
            {
                _logger.LogCritical("Cannot use starting number {startingNumber} because it is negative.", startingNumber);
                throw new ArgumentOutOfRangeException(nameof(startingNumber), "Starting number must be zero or greater.");
            }
        }

        private void VerifyNoDuplicateSequenceNumbers(List<IVideoFile> ordered)
        {
            List<int> duplicates = ordered
                .GroupBy(file => file.SequenceNumber)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicates.Count > 0)
            {
                string duplicateList = string.Join(", ", duplicates);
                _logger.LogCritical(
                    "Sequence number(s) {duplicates} appear more than once; the camera counter restarted or files from more than one card are mixed together.",
                    duplicateList);
                throw new ArgumentException(
                    $"Sequence number(s) {duplicateList} appear more than once. The camera counter restarted or files from more than one card are mixed together; split them into separate directories and rename each directory separately.");
            }
        }

        private void WarnIfTimestampsDisagreeWithSequenceOrder(List<IVideoFile> ordered)
        {
            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].CaptureTimestamp < ordered[i - 1].CaptureTimestamp)
                {
                    _logger.LogWarning(
                        "Timestamps are not in sequence order ({later} was recorded after {earlier} but has an earlier timestamp); the camera clock may have changed or the time zone may have been adjusted. Files are ordered by sequence number.",
                        ordered[i].Name,
                        ordered[i - 1].Name);
                    return;
                }
            }
        }
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~FileSortTests"`
Expected: PASS, 9 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add FileSort ordering by DJI capture counter

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 13: FileRename (spec 5.3 and 5.4)

**Files:**
- Create: `OsmoVideoRenamer/File/Interfaces/IFileRename.cs`
- Create: `OsmoVideoRenamer/File/FileRename.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/FileRenameTests.cs`

- [x] **Step 1: Write the failing tests**

```csharp
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using OsmoVideoRenamer.UnitTests.Logging;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class FileRenameTests
    {
        private readonly Mock<ILogger<FileRename>> _loggerMock = new Mock<ILogger<FileRename>>();
        private readonly Mock<IRenamedVideoFileFactory> _renamedVideoFactoryMock = new Mock<IRenamedVideoFileFactory>();
        private readonly Mock<IRenamedCompanionFileFactory> _renamedCompanionFactoryMock = new Mock<IRenamedCompanionFileFactory>();
        private readonly Mock<ICompanionFileFinder> _companionFinderMock = new Mock<ICompanionFileFinder>();
        private readonly IDirectoryFile[] _allFiles = [DirectoryFileMocking.GetMockedIDirectoryFile("unrelated.txt")];

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
            _renamedVideoFactoryMock.Reset();
            _renamedCompanionFactoryMock.Reset();
            _companionFinderMock.Reset();
            _companionFinderMock
                .Setup(m => m.GetCompanions(It.IsAny<IVideoFile>(), _allFiles))
                .Returns(Array.Empty<IDirectoryFile>());
            _renamedVideoFactoryMock
                .Setup(m => m.Create(It.IsAny<string>(), It.IsAny<IReadOnlyList<IRenamedCompanionFile>>(), It.IsAny<INumberedVideoFile>()))
                .Returns<string, IReadOnlyList<IRenamedCompanionFile>, INumberedVideoFile>((newName, companions, file) => GetRenamedVideo(newName, companions));
            _renamedCompanionFactoryMock
                .Setup(m => m.Create(It.IsAny<string>(), It.IsAny<IDirectoryFile>()))
                .Returns<string, IDirectoryFile>((newName, file) => GetRenamedCompanion(newName));
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenPrefixSupplied()
        {
            CheckPrefixSuffixNaming(
                "some result - ",
                null,
                "some result - 1.MP4",
                "some result - 2.MP4",
                "some result - 3.MP4",
                "some result - 4.MP4",
                "some result - 5.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenSuffixSupplied()
        {
            CheckPrefixSuffixNaming(
                null,
                " - some suffix to follow",
                "1 - some suffix to follow.MP4",
                "2 - some suffix to follow.MP4",
                "3 - some suffix to follow.MP4",
                "4 - some suffix to follow.MP4",
                "5 - some suffix to follow.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenPrefixAndSuffixSupplied()
        {
            CheckPrefixSuffixNaming(
                "a prefix - ",
                " - and a suffix",
                "a prefix - 1 - and a suffix.MP4",
                "a prefix - 2 - and a suffix.MP4",
                "a prefix - 3 - and a suffix.MP4",
                "a prefix - 4 - and a suffix.MP4",
                "a prefix - 5 - and a suffix.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenNeitherPrefixNorSuffixSupplied()
        {
            CheckPrefixSuffixNaming(null, null, "1.MP4", "2.MP4", "3.MP4", "4.MP4", "5.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenDigitCountSpecified()
        {
            var input = GetMockedInput(5);
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, null, null, 3);

            result.Select(r => r.NewName).Should().Equal("001.MP4", "002.MP4", "003.MP4", "004.MP4", "005.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldPadWhenDigitCountNotSpecified()
        {
            var input = GetMockedInput(10);
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, null, null, null);

            result.Select(r => r.NewName).Should().Equal(
                "01.MP4", "02.MP4", "03.MP4", "04.MP4", "05.MP4", "06.MP4", "07.MP4", "08.MP4", "09.MP4", "10.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldPadToOneDigitWhenIndexesStartAtZero()
        {
            var input = GetMockedInput(3, firstIndex: 0);
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, null, null, null);

            result.Select(r => r.NewName).Should().Equal("0.MP4", "1.MP4", "2.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldKeepOriginalExtensionCase()
        {
            var input = GetMockedInput(2, extension: ".mp4");
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, null, null, null);

            result.Select(r => r.NewName).Should().Equal("1.mp4", "2.mp4");
        }

        [TestMethod]
        public void FileRename_ShouldThrowIfSpecifiedDigitsTooLow()
        {
            var input = GetMockedInput(10);
            var rename = CreateFileRename();

            Action act = () => rename.GetRenamedFiles(input, _allFiles, null, null, 1);

            act.Should().ThrowExactly<ArgumentOutOfRangeException>();
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
            _renamedVideoFactoryMock.VerifyNoOtherCalls();
            _renamedCompanionFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileRename_ShouldRenameCompanionsToMatchTheirVideo()
        {
            var input = GetMockedInput(2);
            var lrf1 = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315120000_0001_D.LRF");
            var wav1 = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315120000_0001_D.WAV");
            var lrf2 = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315120000_0002_D.LRF");
            _companionFinderMock.Setup(m => m.GetCompanions(input[0], _allFiles)).Returns(new[] { lrf1, wav1 });
            _companionFinderMock.Setup(m => m.GetCompanions(input[1], _allFiles)).Returns(new[] { lrf2 });
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, "Trip - ", null, 3);

            result.Select(r => r.NewName).Should().Equal("Trip - 001.MP4", "Trip - 002.MP4");
            result[0].Companions.Select(c => c.NewName).Should().Equal("Trip - 001.LRF", "Trip - 001.WAV");
            result[1].Companions.Select(c => c.NewName).Should().Equal("Trip - 002.LRF");
            _renamedCompanionFactoryMock.Verify(m => m.Create("Trip - 001.LRF", lrf1), Times.Once());
            _renamedCompanionFactoryMock.Verify(m => m.Create("Trip - 001.WAV", wav1), Times.Once());
            _renamedCompanionFactoryMock.Verify(m => m.Create("Trip - 002.LRF", lrf2), Times.Once());
            _renamedCompanionFactoryMock.VerifyNoOtherCalls();
            _renamedVideoFactoryMock.Verify(m => m.Create("Trip - 001.MP4", It.Is<IReadOnlyList<IRenamedCompanionFile>>(c => c.Count == 2), input[0]), Times.Once());
            _renamedVideoFactoryMock.Verify(m => m.Create("Trip - 002.MP4", It.Is<IReadOnlyList<IRenamedCompanionFile>>(c => c.Count == 1), input[1]), Times.Once());
            _renamedVideoFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileRename_ShouldReturnEmptyForEmptyInput()
        {
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(new List<INumberedVideoFile>(), _allFiles, "prefix", "suffix", 3);

            result.Should().BeEmpty();
            _renamedVideoFactoryMock.VerifyNoOtherCalls();
            _renamedCompanionFactoryMock.VerifyNoOtherCalls();
            _companionFinderMock.VerifyNoOtherCalls();
        }

        private void CheckPrefixSuffixNaming(string? prefix, string? suffix, params string[] expectedFilenames)
        {
            var input = GetMockedInput(5);
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, prefix, suffix, null);

            result.Select(r => r.NewName).Should().Equal(expectedFilenames);
            foreach (INumberedVideoFile file in input)
            {
                _renamedVideoFactoryMock.Verify(m => m.Create(It.IsAny<string>(), It.IsAny<IReadOnlyList<IRenamedCompanionFile>>(), file), Times.Once());
            }
        }

        private FileRename CreateFileRename() =>
            new FileRename(_loggerMock.Object, _renamedVideoFactoryMock.Object, _renamedCompanionFactoryMock.Object, _companionFinderMock.Object);

        private static IRenamedVideoFile GetRenamedVideo(string newName, IReadOnlyList<IRenamedCompanionFile> companions)
        {
            var result = new Mock<IRenamedVideoFile>();
            result.Setup(m => m.NewName).Returns(newName);
            result.Setup(m => m.Companions).Returns(companions);
            return result.Object;
        }

        private static IRenamedCompanionFile GetRenamedCompanion(string newName)
        {
            var result = new Mock<IRenamedCompanionFile>();
            result.Setup(m => m.NewName).Returns(newName);
            return result.Object;
        }

        private static IList<INumberedVideoFile> GetMockedInput(int count, int firstIndex = 1, string extension = ".MP4")
        {
            var result = new List<INumberedVideoFile>();
            for (int i = 0; i < count; i++)
            {
                var file = new Mock<INumberedVideoFile>();
                file.Setup(m => m.FileExtension).Returns(extension);
                file.Setup(m => m.Name).Returns($"DJI_20240315120000_{i + 1:D4}_D{extension}");
                file.Setup(m => m.NewIndex).Returns(i + firstIndex);
                result.Add(file.Object);
            }

            return result;
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~FileRenameTests"`
Expected: build error `CS0246` for `FileRename`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/Interfaces/IFileRename.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File.Interfaces
{
    public interface IFileRename
    {
        IList<IRenamedVideoFile> GetRenamedFiles(
            IList<INumberedVideoFile> files,
            IEnumerable<IDirectoryFile> allFiles,
            string? prefix,
            string? suffix,
            int? digitCount);
    }
}
```

`OsmoVideoRenamer/File/FileRename.cs`:

```csharp
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using System.Globalization;

namespace OsmoVideoRenamer.File
{
    public class FileRename : IFileRename
    {
        private readonly ILogger<FileRename> _logger;
        private readonly IRenamedVideoFileFactory _renamedVideoFileFactory;
        private readonly IRenamedCompanionFileFactory _renamedCompanionFileFactory;
        private readonly ICompanionFileFinder _companionFileFinder;

        public FileRename(
            ILogger<FileRename> logger,
            IRenamedVideoFileFactory renamedVideoFileFactory,
            IRenamedCompanionFileFactory renamedCompanionFileFactory,
            ICompanionFileFinder companionFileFinder)
        {
            _logger = logger;
            _renamedVideoFileFactory = renamedVideoFileFactory;
            _renamedCompanionFileFactory = renamedCompanionFileFactory;
            _companionFileFinder = companionFileFinder;
        }

        public IList<IRenamedVideoFile> GetRenamedFiles(
            IList<INumberedVideoFile> files,
            IEnumerable<IDirectoryFile> allFiles,
            string? prefix,
            string? suffix,
            int? digitCount)
        {
            if (files.Count == 0)
            {
                return new List<IRenamedVideoFile>();
            }

            int maxNewIndex = files.Max(file => file.NewIndex);
            int digits = GetDigitCount(digitCount, maxNewIndex);
            return files
                .Select(file => CreateRenamedFile(file, allFiles, prefix, suffix, digits))
                .ToList();
        }

        private IRenamedVideoFile CreateRenamedFile(
            INumberedVideoFile file,
            IEnumerable<IDirectoryFile> allFiles,
            string? prefix,
            string? suffix,
            int digits)
        {
            string numberFormat = "D" + digits.ToString(CultureInfo.InvariantCulture);
            string newBaseName = prefix + file.NewIndex.ToString(numberFormat, CultureInfo.InvariantCulture) + suffix;
            List<IRenamedCompanionFile> companions = _companionFileFinder
                .GetCompanions(file, allFiles)
                .Select(companion => _renamedCompanionFileFactory.Create(newBaseName + companion.FileExtension, companion))
                .ToList();
            return _renamedVideoFileFactory.Create(newBaseName + file.FileExtension, companions, file);
        }

        private int GetDigitCount(int? digitCount, int maxNewIndex)
        {
            int requiredDigits = maxNewIndex.ToString(CultureInfo.InvariantCulture).Length;
            if (!digitCount.HasValue)
            {
                return requiredDigits;
            }

            _logger.LogInformation("Verifying that digit count is large enough to accommodate maximum file index...");
            if (digitCount.Value < requiredDigits)
            {
                _logger.LogCritical(
                    "Cannot use provided digit count {digitCount} because maximum file index digits {maxDigits} is greater.",
                    digitCount.Value,
                    requiredDigits);
                throw new ArgumentOutOfRangeException(
                    nameof(digitCount),
                    $"Digit count must be at least the number of digits in the largest renamed file index, which is {requiredDigits}");
            }

            _logger.LogInformation("Verified that digit count is large enough to accommodate maximum file index.");
            return digitCount.Value;
        }
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~FileRenameTests"`
Expected: PASS, 11 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add FileRename with companion file naming

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 14: RenameCollisionChecker (spec 5.5)

**Files:**
- Create: `OsmoVideoRenamer/File/Interfaces/IRenameCollisionChecker.cs`
- Create: `OsmoVideoRenamer/File/RenameCollisionChecker.cs`
- Test: `OsmoVideoRenamer.UnitTests/File/RenameCollisionCheckerTests.cs`

- [x] **Step 1: Write the failing tests**

```csharp
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using OsmoVideoRenamer.UnitTests.Logging;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class RenameCollisionCheckerTests
    {
        private readonly Mock<ILogger<RenameCollisionChecker>> _loggerMock = new Mock<ILogger<RenameCollisionChecker>>();

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
        }

        [TestMethod]
        public void Checker_ShouldPassWhenNoPlannedNameCollides()
        {
            var renamed = new[]
            {
                Renamed("Trip - 001.MP4", "Trip - 001.LRF", "Trip - 001.WAV"),
                Renamed("Trip - 002.MP4", "Trip - 002.LRF"),
            };
            var existing = Existing(
                "DJI_20240315120000_0001_D.MP4", "DJI_20240315120000_0001_D.LRF", "DJI_20240315120000_0001_D.WAV",
                "DJI_20240315120100_0002_D.MP4", "DJI_20240315120100_0002_D.LRF", "notes.txt");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().NotThrow();
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Never());
        }

        [TestMethod]
        public void Checker_ShouldThrowWhenPlannedVideoNameAlreadyExists()
        {
            var renamed = new[] { Renamed("Trip - 001.MP4"), Renamed("Trip - 002.MP4") };
            var existing = Existing("DJI_20240315120000_0001_D.MP4", "DJI_20240315120100_0002_D.MP4", "Trip - 002.MP4");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().ThrowExactly<IOException>().WithMessage("*Trip - 002.MP4*");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
        }

        [TestMethod]
        public void Checker_ShouldThrowWhenPlannedCompanionNameAlreadyExists()
        {
            var renamed = new[] { Renamed("Trip - 001.MP4", "Trip - 001.LRF") };
            var existing = Existing("DJI_20240315120000_0001_D.MP4", "DJI_20240315120000_0001_D.LRF", "Trip - 001.LRF");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().ThrowExactly<IOException>().WithMessage("*Trip - 001.LRF*");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
        }

        [TestMethod]
        public void Checker_ShouldThrowWhenTwoPlannedNamesAreEqual()
        {
            var renamed = new[] { Renamed("Trip - 001.MP4"), Renamed("Trip - 001.MP4") };
            var existing = Existing("DJI_20240315120000_0001_D.MP4", "DJI_20240315120100_0002_D.MP4");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().ThrowExactly<IOException>().WithMessage("*Trip - 001.MP4*");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
        }

        [TestMethod]
        public void Checker_ShouldReportBothDuplicateAndExistingNamesInOneRun()
        {
            var renamed = new[] { Renamed("Trip - 001.MP4"), Renamed("Trip - 001.MP4"), Renamed("Trip - 002.MP4") };
            var existing = Existing("DJI_20240315120000_0001_D.MP4", "Trip - 002.MP4");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().ThrowExactly<IOException>()
                .WithMessage("*more than once: Trip - 001.MP4*")
                .WithMessage("*already exist in the directory: Trip - 002.MP4*");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
        }

        [TestMethod]
        public void Checker_ShouldCompareNamesCaseInsensitively()
        {
            var renamed = new[] { Renamed("Trip - 001.MP4") };
            var existing = Existing("DJI_20240315120000_0001_D.MP4", "trip - 001.mp4");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().ThrowExactly<IOException>();
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
        }

        [TestMethod]
        public void Checker_ShouldPassForEmptyInput()
        {
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(Array.Empty<IRenamedVideoFile>(), Existing("notes.txt"));

            act.Should().NotThrow();
        }

        private static IRenamedVideoFile Renamed(string newName, params string[] companionNewNames)
        {
            var companions = new List<IRenamedCompanionFile>();
            foreach (string companionNewName in companionNewNames)
            {
                var companion = new Mock<IRenamedCompanionFile>();
                companion.Setup(m => m.NewName).Returns(companionNewName);
                companions.Add(companion.Object);
            }

            var file = new Mock<IRenamedVideoFile>();
            file.Setup(m => m.NewName).Returns(newName);
            file.Setup(m => m.Companions).Returns(companions);
            return file.Object;
        }

        private static IDirectoryFile[] Existing(params string[] names) =>
            names.Select(DirectoryFileMocking.GetMockedIDirectoryFile).ToArray();
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~RenameCollisionCheckerTests"`
Expected: build error `CS0246` for `RenameCollisionChecker`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/File/Interfaces/IRenameCollisionChecker.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File.Interfaces
{
    public interface IRenameCollisionChecker
    {
        void VerifyNoCollisions(IEnumerable<IRenamedVideoFile> renamedFiles, IEnumerable<IDirectoryFile> allFiles);
    }
}
```

`OsmoVideoRenamer/File/RenameCollisionChecker.cs`:

```csharp
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File
{
    /// <summary>
    /// Runs before any file is touched so that a run either renames everything or nothing.
    /// Comparisons are case-insensitive because macOS and Windows file systems usually are.
    /// </summary>
    public class RenameCollisionChecker : IRenameCollisionChecker
    {
        private readonly ILogger<RenameCollisionChecker> _logger;

        public RenameCollisionChecker(ILogger<RenameCollisionChecker> logger)
        {
            _logger = logger;
        }

        public void VerifyNoCollisions(IEnumerable<IRenamedVideoFile> renamedFiles, IEnumerable<IDirectoryFile> allFiles)
        {
            List<string> plannedNames = renamedFiles
                .SelectMany(file => file.Companions.Select(companion => companion.NewName).Prepend(file.NewName))
                .ToList();
            List<string> duplicates = plannedNames
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            var existingNames = new HashSet<string>(allFiles.Select(file => file.Name), StringComparer.OrdinalIgnoreCase);
            List<string> alreadyPresent = plannedNames
                .Where(name => existingNames.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (duplicates.Count > 0 || alreadyPresent.Count > 0)
            {
                string message = BuildMessage(duplicates, alreadyPresent);
                _logger.LogCritical("{message}", message);
                throw new IOException(message);
            }

            _logger.LogInformation("Verified that no planned file name is used twice or already exists in the directory.");
        }

        private static string BuildMessage(List<string> duplicates, List<string> alreadyPresent)
        {
            var parts = new List<string> { "Cannot rename; no files were changed." };
            if (duplicates.Count > 0)
            {
                parts.Add($"These new names would be used more than once: {string.Join(", ", duplicates)}.");
            }

            if (alreadyPresent.Count > 0)
            {
                parts.Add($"These new names already exist in the directory: {string.Join(", ", alreadyPresent)}.");
            }

            return string.Join(" ", parts);
        }
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~RenameCollisionCheckerTests"`
Expected: PASS, 7 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add RenameCollisionChecker pre-flight check

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 15: VideoDirectory and its factory

**Files:**
- Create: `OsmoVideoRenamer/Directory/Interfaces/IVideoDirectory.cs`
- Create: `OsmoVideoRenamer/Directory/Interfaces/IVideoDirectoryFactory.cs`
- Create: `OsmoVideoRenamer/Directory/VideoDirectory.cs`
- Create: `OsmoVideoRenamer/Directory/VideoDirectoryFactory.cs`
- Test: `OsmoVideoRenamer.UnitTests/Directory/VideoDirectoryTests.cs`
- Test: `OsmoVideoRenamer.UnitTests/Directory/VideoDirectoryFactoryTests.cs`

- [x] **Step 1: Write the failing tests**

`OsmoVideoRenamer.UnitTests/Directory/VideoDirectoryTests.cs`:

```csharp
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.Directory;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.Directory
{
    [TestClass]
    public class VideoDirectoryTests
    {
        private readonly Mock<ILogger<VideoDirectory>> _loggerMock = new Mock<ILogger<VideoDirectory>>();
        private readonly Mock<IFileSystem> _fileSystemMock = new Mock<IFileSystem>();
        private readonly Mock<IDirectoryInfoFactory> _directoryInfoFactoryMock = new Mock<IDirectoryInfoFactory>();
        private readonly Mock<IDirectoryFileFactory> _directoryFileFactoryMock = new Mock<IDirectoryFileFactory>();
        private readonly string _directoryPath = "asdf1234";

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
            _fileSystemMock.Reset();
            _directoryInfoFactoryMock.Reset();
            _directoryFileFactoryMock.Reset();
            _fileSystemMock.Setup(m => m.DirectoryInfo).Returns(_directoryInfoFactoryMock.Object);
        }

        [TestMethod]
        public void VideoDirectory_ShouldThrow_WhenDirectoryDoesNotExist()
        {
            var badDirectory = new Mock<IDirectoryInfo>();
            badDirectory.Setup(m => m.Exists).Returns(false);
            _directoryInfoFactoryMock.Setup(m => m.New(_directoryPath)).Returns(badDirectory.Object);

            Action act = () => new VideoDirectory(
                _loggerMock.Object,
                _fileSystemMock.Object,
                _directoryFileFactoryMock.Object,
                _directoryPath);

            act.Should().ThrowExactly<DirectoryNotFoundException>();
        }

        [TestMethod]
        public void VideoDirectory_ShouldCreateDirectoryFileObjects_InDirectoryOrder()
        {
            int fileCount = 5;
            IFileInfo[] directoryFiles = new IFileInfo[fileCount];
            IDirectoryFile[] expected = new IDirectoryFile[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                directoryFiles[i] = new Mock<IFileInfo>().Object;
                expected[i] = new Mock<IDirectoryFile>().Object;
                _directoryFileFactoryMock.Setup(m => m.Create(directoryFiles[i])).Returns(expected[i]);
            }

            var goodDirectory = new Mock<IDirectoryInfo>();
            goodDirectory.Setup(m => m.Exists).Returns(true);
            goodDirectory.Setup(m => m.GetFiles()).Returns(directoryFiles);
            _directoryInfoFactoryMock.Setup(m => m.New(_directoryPath)).Returns(goodDirectory.Object);
            var videoDirectory = new VideoDirectory(
                _loggerMock.Object,
                _fileSystemMock.Object,
                _directoryFileFactoryMock.Object,
                _directoryPath);

            var result = videoDirectory.GetFilesInDirectory();

            result.Should().Equal(expected);
            for (int i = 0; i < fileCount; i++)
            {
                _directoryFileFactoryMock.Verify(m => m.Create(directoryFiles[i]), Times.Once());
            }
        }
    }
}
```

`OsmoVideoRenamer.UnitTests/Directory/VideoDirectoryFactoryTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.Directory;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.Directory
{
    [TestClass]
    public class VideoDirectoryFactoryTests
    {
        private readonly ServiceCollection _serviceCollection = new ServiceCollection();
        private readonly Mock<ILogger<VideoDirectory>> _loggerMock = new Mock<ILogger<VideoDirectory>>();
        private readonly Mock<IFileSystem> _fileSystemMock = new Mock<IFileSystem>();
        private readonly Mock<IDirectoryInfoFactory> _directoryInfoFactoryMock = new Mock<IDirectoryInfoFactory>();
        private readonly Mock<IDirectoryInfo> _directoryInfoMock = new Mock<IDirectoryInfo>();
        private readonly Mock<IDirectoryFileFactory> _directoryFileFactoryMock = new Mock<IDirectoryFileFactory>();
        private readonly string _directory = "asdf1234";

        [TestInitialize]
        public void Setup()
        {
            _directoryInfoMock.Setup(m => m.Exists).Returns(true);
            _directoryInfoFactoryMock.Setup(m => m.New(_directory)).Returns(_directoryInfoMock.Object);
            _fileSystemMock.Setup(m => m.DirectoryInfo).Returns(_directoryInfoFactoryMock.Object);
            _serviceCollection.Clear();
            _serviceCollection.AddSingleton<ILogger<VideoDirectory>>((_) => _loggerMock.Object);
            _serviceCollection.AddSingleton<IFileSystem>((_) => _fileSystemMock.Object);
            _serviceCollection.AddSingleton<IDirectoryFileFactory>((_) => _directoryFileFactoryMock.Object);
        }

        [TestMethod]
        public void VideoDirectoryFactory_ShouldCreateDirectory()
        {
            var factory = new VideoDirectoryFactory(_serviceCollection.BuildServiceProvider());

            var videoDirectory = factory.Create(_directory);

            _fileSystemMock.Verify(m => m.DirectoryInfo);
            _directoryInfoFactoryMock.Verify(m => m.New(_directory));
            _directoryInfoMock.Verify(m => m.Exists);
            videoDirectory.Should().BeAssignableTo<IVideoDirectory>();
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~VideoDirectory"`
Expected: build error `CS0246` for `VideoDirectory`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/Directory/Interfaces/IVideoDirectory.cs`:

```csharp
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.Directory.Interfaces
{
    public interface IVideoDirectory
    {
        IReadOnlyList<IDirectoryFile> GetFilesInDirectory();
    }
}
```

`OsmoVideoRenamer/Directory/Interfaces/IVideoDirectoryFactory.cs`:

```csharp
namespace OsmoVideoRenamer.Directory.Interfaces
{
    public interface IVideoDirectoryFactory
    {
        IVideoDirectory Create(string directoryPath);
    }
}
```

`OsmoVideoRenamer/Directory/VideoDirectory.cs`:

```csharp
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.Directory
{
    public class VideoDirectory : IVideoDirectory
    {
        private readonly ILogger<VideoDirectory> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly string _directoryPath;
        private readonly IDirectoryInfo _directoryInfo;
        private readonly IDirectoryFileFactory _directoryFileFactory;

        public VideoDirectory(
            ILogger<VideoDirectory> logger,
            IFileSystem fileSystem,
            IDirectoryFileFactory directoryFileFactory,
            string directoryPath)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _directoryFileFactory = directoryFileFactory;
            _directoryPath = directoryPath;
            _directoryInfo = _fileSystem.DirectoryInfo.New(_directoryPath);
            VerifyDirectoryExists();
        }

        private void VerifyDirectoryExists()
        {
            _logger.LogInformation("Checking for existence of directory at path {path}", _directoryPath);
            if (!_directoryInfo.Exists)
            {
                _logger.LogCritical("Directory at path {path} does not exist!", _directoryPath);
                throw new DirectoryNotFoundException("Unable to find specified source directory.");
            }

            _logger.LogInformation("Directory at path {path} does exist.", _directoryPath);
        }

        public IReadOnlyList<IDirectoryFile> GetFilesInDirectory() =>
            _directoryInfo.GetFiles().Select(file => _directoryFileFactory.Create(file)).ToList();
    }
}
```

`OsmoVideoRenamer/Directory/VideoDirectoryFactory.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.Directory
{
    public class VideoDirectoryFactory : IVideoDirectoryFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public VideoDirectoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IVideoDirectory Create(string directoryPath)
        {
            return new VideoDirectory(
                _serviceProvider.GetRequiredService<ILogger<VideoDirectory>>(),
                _serviceProvider.GetRequiredService<IFileSystem>(),
                _serviceProvider.GetRequiredService<IDirectoryFileFactory>(),
                directoryPath);
        }
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~VideoDirectory"`
Expected: PASS, 3 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add VideoDirectory listing and factory

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 16: RenameCommand (spec 4 and 6.1)

**Files:**
- Create: `OsmoVideoRenamer/RenameCommand.cs`
- Test: `OsmoVideoRenamer.UnitTests/RenameCommandTests.cs`

- [x] **Step 1: Write the failing tests**

```csharp
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.ConsoleWrapping;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using OsmoVideoRenamer.UnitTests.Logging;

namespace OsmoVideoRenamer.UnitTests
{
    [TestClass]
    public class RenameCommandTests
    {
        private readonly Mock<ILogger<RenameCommand>> _loggerMock = new Mock<ILogger<RenameCommand>>();
        private readonly Mock<IVideoDirectoryFactory> _directoryFactoryMock = new Mock<IVideoDirectoryFactory>();
        private readonly Mock<IVideoDirectory> _directoryMock = new Mock<IVideoDirectory>();
        private readonly Mock<IFileFilter> _fileFilterMock = new Mock<IFileFilter>();
        private readonly Mock<IFileSort> _fileSortMock = new Mock<IFileSort>();
        private readonly Mock<IFileRename> _fileRenameMock = new Mock<IFileRename>();
        private readonly Mock<IRenameCollisionChecker> _collisionCheckerMock = new Mock<IRenameCollisionChecker>();
        private readonly Mock<IConsoleWrapper> _consoleWrapperMock = new Mock<IConsoleWrapper>();
        private readonly Mock<IRenamedVideoFile> _testRenamed1 = new Mock<IRenamedVideoFile>();
        private readonly Mock<IRenamedVideoFile> _testRenamed2 = new Mock<IRenamedVideoFile>();
        private readonly Mock<IRenamedCompanionFile> _testCompanion1 = new Mock<IRenamedCompanionFile>();
        private readonly Mock<IRenamedCompanionFile> _testCompanion2 = new Mock<IRenamedCompanionFile>();
        private readonly IDirectoryFile[] _allFiles = [new Mock<IDirectoryFile>().Object, new Mock<IDirectoryFile>().Object, new Mock<IDirectoryFile>().Object];
        private readonly IVideoFile[] _filtered = [new Mock<IVideoFile>().Object, new Mock<IVideoFile>().Object];
        private readonly List<INumberedVideoFile> _numbered = [new Mock<INumberedVideoFile>().Object, new Mock<INumberedVideoFile>().Object];
        private List<IRenamedVideoFile> _renamed = new List<IRenamedVideoFile>();
        private readonly string _fileLocation = "asdf1234";
        private readonly string _prefix = "prefix1234";
        private readonly string _suffix = "1234suffix";
        private const int _startingNumber = 5;
        private const int _digitCount = 7;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
            _directoryFactoryMock.Reset();
            _directoryMock.Reset();
            _fileFilterMock.Reset();
            _fileSortMock.Reset();
            _fileRenameMock.Reset();
            _collisionCheckerMock.Reset();
            _consoleWrapperMock.Reset();
            _testRenamed1.Reset();
            _testRenamed2.Reset();
            _testCompanion1.Reset();
            _testCompanion2.Reset();

            _directoryFactoryMock.Setup(m => m.Create(_fileLocation)).Returns(_directoryMock.Object);
            _directoryMock.Setup(m => m.GetFilesInDirectory()).Returns(_allFiles);
            _fileFilterMock.Setup(m => m.GetMatchingVideos(_allFiles)).Returns(_filtered);
            _fileSortMock.Setup(m => m.GetOrderedFiles(_filtered, _startingNumber)).Returns(_numbered);
            SetupCompanion(_testCompanion1, "old1.LRF", "new1.LRF");
            SetupCompanion(_testCompanion2, "old1.WAV", "new1.WAV");
            SetupRenamedFile(_testRenamed1, "old1", "new1", 1, new List<IRenamedCompanionFile> { _testCompanion1.Object, _testCompanion2.Object });
            SetupRenamedFile(_testRenamed2, "old2", "new file", 2, new List<IRenamedCompanionFile>());
            _renamed = new List<IRenamedVideoFile> { _testRenamed1.Object, _testRenamed2.Object };
            _fileRenameMock.Setup(m => m.GetRenamedFiles(_numbered, _allFiles, _prefix, _suffix, _digitCount)).Returns(_renamed);
        }

        [TestMethod]
        public void Command_ShouldRunThePipelineInOrder()
        {
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount, true);

            _directoryFactoryMock.Verify(m => m.Create(_fileLocation), Times.Once());
            _directoryMock.Verify(m => m.GetFilesInDirectory(), Times.Once());
            _fileFilterMock.Verify(m => m.GetMatchingVideos(_allFiles), Times.Once());
            _fileSortMock.Verify(m => m.GetOrderedFiles(_filtered, _startingNumber), Times.Once());
            _fileRenameMock.Verify(m => m.GetRenamedFiles(_numbered, _allFiles, _prefix, _suffix, _digitCount), Times.Once());
            _collisionCheckerMock.Verify(m => m.VerifyNoCollisions(_renamed, _allFiles), Times.Once());
            _loggerMock.VerifyLogged(LogLevel.Information, Times.Once());
            _directoryFactoryMock.VerifyNoOtherCalls();
            _directoryMock.VerifyNoOtherCalls();
            _fileFilterMock.VerifyNoOtherCalls();
            _fileSortMock.VerifyNoOtherCalls();
            _fileRenameMock.VerifyNoOtherCalls();
            _collisionCheckerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Command_ShouldWritePreviewToConsole_IncludingCompanions()
        {
            var lines = new List<string>();
            _consoleWrapperMock.Setup(m => m.WriteLine(It.IsAny<string>())).Callback<string>(line => lines.Add(line));
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount, true);

            lines.Should().Equal(
                "1: old1 -> new1",
                "    old1.LRF -> new1.LRF",
                "    old1.WAV -> new1.WAV",
                "2: old2 -> new file");
            _consoleWrapperMock.Verify(m => m.WriteLine(It.IsAny<string>()), Times.Exactly(4));
            _consoleWrapperMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Command_ShouldNotCommitToDisk_IfDryRun()
        {
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount, true);

            _collisionCheckerMock.Verify(m => m.VerifyNoCollisions(_renamed, _allFiles), Times.Once());
            _testRenamed1.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testRenamed2.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testCompanion1.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testCompanion2.Verify(m => m.CommitRenameToDisk(), Times.Never());
        }

        [TestMethod]
        public void Command_ShouldCommitVideosAndCompanionsToDisk_IfNotDryRun()
        {
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount);

            _testRenamed1.Verify(m => m.CommitRenameToDisk(), Times.Once());
            _testCompanion1.Verify(m => m.CommitRenameToDisk(), Times.Once());
            _testCompanion2.Verify(m => m.CommitRenameToDisk(), Times.Once());
            _testRenamed2.Verify(m => m.CommitRenameToDisk(), Times.Once());
        }

        [TestMethod]
        public void Command_ShouldCommitEachVideoBeforeItsCompanions()
        {
            var order = new List<string>();
            _testRenamed1.Setup(m => m.CommitRenameToDisk()).Callback(() => order.Add("video1"));
            _testCompanion1.Setup(m => m.CommitRenameToDisk()).Callback(() => order.Add("companion1"));
            _testCompanion2.Setup(m => m.CommitRenameToDisk()).Callback(() => order.Add("companion2"));
            _testRenamed2.Setup(m => m.CommitRenameToDisk()).Callback(() => order.Add("video2"));
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount);

            order.Should().Equal("video1", "companion1", "companion2", "video2");
        }

        [TestMethod]
        public void Command_ShouldPrintMessageAndStop_IfNoMatchingFiles()
        {
            _fileSortMock.Setup(m => m.GetOrderedFiles(_filtered, _startingNumber)).Returns(new List<INumberedVideoFile>());
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount);

            _consoleWrapperMock.Verify(m => m.WriteLine("No DJI Osmo videos found in asdf1234."), Times.Once());
            _consoleWrapperMock.VerifyNoOtherCalls();
            _fileRenameMock.VerifyNoOtherCalls();
            _collisionCheckerMock.VerifyNoOtherCalls();
            _loggerMock.VerifyLogged(LogLevel.Information, Times.Never());
        }

        [TestMethod]
        public void Command_ShouldNotPrintOrRename_IfCollisionCheckFails()
        {
            _collisionCheckerMock.Setup(m => m.VerifyNoCollisions(_renamed, _allFiles)).Throws(new IOException("collision"));
            var command = CreateCommand();

            Action act = () => command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount);

            act.Should().ThrowExactly<IOException>();
            _consoleWrapperMock.VerifyNoOtherCalls();
            _testRenamed1.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testRenamed2.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testCompanion1.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testCompanion2.Verify(m => m.CommitRenameToDisk(), Times.Never());
        }

        private RenameCommand CreateCommand() =>
            new RenameCommand(
                _loggerMock.Object,
                _directoryFactoryMock.Object,
                _fileFilterMock.Object,
                _fileSortMock.Object,
                _fileRenameMock.Object,
                _collisionCheckerMock.Object,
                _consoleWrapperMock.Object);

        private static void SetupRenamedFile(Mock<IRenamedVideoFile> file, string oldName, string newName, int order, IReadOnlyList<IRenamedCompanionFile> companions)
        {
            file.Setup(m => m.Name).Returns(oldName);
            file.Setup(m => m.NewName).Returns(newName);
            file.Setup(m => m.NewIndex).Returns(order);
            file.Setup(m => m.Companions).Returns(companions);
        }

        private static void SetupCompanion(Mock<IRenamedCompanionFile> file, string oldName, string newName)
        {
            file.Setup(m => m.Name).Returns(oldName);
            file.Setup(m => m.NewName).Returns(newName);
        }
    }
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~RenameCommandTests"`
Expected: build error `CS0246` for `RenameCommand`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/RenameCommand.cs`:

```csharp
using Cocona;
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.ConsoleWrapping;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer
{
    public class RenameCommand
    {
        private readonly ILogger<RenameCommand> _logger;
        private readonly IVideoDirectoryFactory _directoryFactory;
        private readonly IFileFilter _fileFilter;
        private readonly IFileSort _fileSort;
        private readonly IFileRename _fileRename;
        private readonly IRenameCollisionChecker _collisionChecker;
        private readonly IConsoleWrapper _console;

        public RenameCommand(
            ILogger<RenameCommand> logger,
            IVideoDirectoryFactory directoryFactory,
            IFileFilter fileFilter,
            IFileSort fileSort,
            IFileRename fileRename,
            IRenameCollisionChecker collisionChecker,
            IConsoleWrapper console)
        {
            _logger = logger;
            _directoryFactory = directoryFactory;
            _fileFilter = fileFilter;
            _fileSort = fileSort;
            _fileRename = fileRename;
            _collisionChecker = collisionChecker;
            _console = console;
        }

        public void Rename(
            [Option(Description = "Directory where the Osmo videos are stored")] string fileLocation,
            [Option(Description = "Text that should appear before each file's number")] string? prefix,
            [Option(Description = "Text that should appear after each file's number")] string? suffix,
            [Option(Description = "What number the renamed files should start at (default 1)")] int? startingNumber,
            [Option(Description = "The number of digits to include in each file number")] int? digitCount,
            [Option(Description = "Print a list of the files to be renamed, but do not rename them")] bool dryRun = false)
        {
            IVideoDirectory videoDirectory = _directoryFactory.Create(fileLocation);
            IReadOnlyList<IDirectoryFile> allFiles = videoDirectory.GetFilesInDirectory();
            IEnumerable<IVideoFile> matchingVideos = _fileFilter.GetMatchingVideos(allFiles);
            List<INumberedVideoFile> sortedVideos = _fileSort.GetOrderedFiles(matchingVideos, startingNumber);
            if (sortedVideos.Count == 0)
            {
                _console.WriteLine($"No DJI Osmo videos found in {fileLocation}.");
                return;
            }

            _logger.LogInformation("Found {count} DJI Osmo video(s) to rename.", sortedVideos.Count);
            IList<IRenamedVideoFile> renamedFiles = _fileRename.GetRenamedFiles(sortedVideos, allFiles, prefix, suffix, digitCount);
            _collisionChecker.VerifyNoCollisions(renamedFiles, allFiles);
            foreach (IRenamedVideoFile file in renamedFiles)
            {
                _console.WriteLine($"{file.NewIndex}: {file.Name} -> {file.NewName}");
                foreach (IRenamedCompanionFile companion in file.Companions)
                {
                    _console.WriteLine($"    {companion.Name} -> {companion.NewName}");
                }

                if (!dryRun)
                {
                    file.CommitRenameToDisk();
                    foreach (IRenamedCompanionFile companion in file.Companions)
                    {
                        companion.CommitRenameToDisk();
                    }
                }
            }
        }
    }
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~RenameCommandTests"`
Expected: PASS, 7 tests.

- [x] **Step 5: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Add RenameCommand orchestrating the rename pipeline

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 17: Configuration and the real entry point

**Files:**
- Create: `OsmoVideoRenamer/Configuration/ServiceConfiguration.cs`
- Create: `OsmoVideoRenamer/Configuration/CommandConfiguration.cs`
- Create: `OsmoVideoRenamer/Configuration/FilterConfiguration.cs`
- Modify: `OsmoVideoRenamer/Program.cs` (replace the placeholder)
- Test: `OsmoVideoRenamer.UnitTests/Configuration/ServiceConfigurationTests.cs`
- Test: `OsmoVideoRenamer.UnitTests/Configuration/CommandConfigurationTests.cs`
- Test: `OsmoVideoRenamer.UnitTests/Configuration/FilterConfigurationTests.cs`

- [x] **Step 1: Write the failing tests**

`OsmoVideoRenamer.UnitTests/Configuration/ServiceConfigurationTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OsmoVideoRenamer.Configuration;
using OsmoVideoRenamer.ConsoleWrapping;
using OsmoVideoRenamer.Directory;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.CompanionFiles;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.Configuration
{
    [TestClass]
    public class ServiceConfigurationTests
    {
        [TestMethod]
        [DataRow(typeof(IConsoleWrapper), typeof(ConsoleWrapper))]
        [DataRow(typeof(IFileSystem), typeof(FileSystem))]
        [DataRow(typeof(IVideoDirectoryFactory), typeof(VideoDirectoryFactory))]
        [DataRow(typeof(IDirectoryFileFactory), typeof(DirectoryFileFactory))]
        [DataRow(typeof(IVideoFileFactory), typeof(VideoFileFactory))]
        [DataRow(typeof(INumberedVideoFileFactory), typeof(NumberedVideoFileFactory))]
        [DataRow(typeof(IRenamedVideoFileFactory), typeof(RenamedVideoFileFactory))]
        [DataRow(typeof(IRenamedCompanionFileFactory), typeof(RenamedCompanionFileFactory))]
        [DataRow(typeof(ICompanionFileFinder), typeof(CompanionFileFinder))]
        [DataRow(typeof(IFileFilter), typeof(FileFilter))]
        [DataRow(typeof(IFileSort), typeof(FileSort))]
        [DataRow(typeof(IFileRename), typeof(FileRename))]
        [DataRow(typeof(IRenameCollisionChecker), typeof(RenameCollisionChecker))]
        public void ServiceConfiguration_ShouldRegisterService(Type serviceType, Type implementationType)
        {
            var collection = new ServiceCollection();
            collection.AddLogging();

            ServiceConfiguration.Configure(collection);

            collection
                .BuildServiceProvider()
                .GetRequiredService(serviceType)
                .Should()
                .BeOfType(implementationType);
        }
    }
}
```

`OsmoVideoRenamer.UnitTests/Configuration/CommandConfigurationTests.cs`:

```csharp
using Cocona.Builder;
using Cocona.Filters;
using OsmoVideoRenamer.Configuration;

namespace OsmoVideoRenamer.UnitTests.Configuration
{
    [TestClass]
    public class CommandConfigurationTests
    {
        [TestMethod]
        public void CommandConfiguration_ShouldAddRenameCommand()
        {
            var mockedApp = new Mock<ICoconaCommandsBuilder>();
            mockedApp.DefaultValue = DefaultValue.Mock;

            CommandConfiguration.RegisterAllCommands(mockedApp.Object);

            mockedApp.Verify(m => m.Add(It.Is<TypeCommandDataSource>(t => ((TypeCommandData)t.Build()).Type == typeof(RenameCommand))));
        }
    }
}
```

`OsmoVideoRenamer.UnitTests/Configuration/FilterConfigurationTests.cs`:

```csharp
using Cocona.Builder;
using OsmoVideoRenamer.Configuration;
using OsmoVideoRenamer.ParameterLogging;

namespace OsmoVideoRenamer.UnitTests.Configuration
{
    [TestClass]
    public class FilterConfigurationTests
    {
        [TestMethod]
        public void FilterConfiguration_ShouldRegisterParameterLoggingFilter()
        {
            var mockedApp = new Mock<ICoconaCommandsBuilder>();
            var mockedPropertyDict = new Mock<IDictionary<string, object?>>();
            var mockedObjectList = new Mock<IList<object>>();
            mockedPropertyDict.Setup(m => m.TryGetValue("Cocona.Builder.CoconaCommandsBuilder+Filters", out It.Ref<object>.IsAny!))
                .Callback((string key, out object result) => { result = mockedObjectList.Object; })
                .Returns(true);
            mockedApp.Setup(m => m.Properties).Returns(mockedPropertyDict.Object);

            FilterConfiguration.RegisterAllFilters(mockedApp.Object);

            mockedObjectList.Verify(m => m.Add(It.IsAny<ParameterLoggingCommandFilterFactory>()));
        }
    }
}
```

(The two Cocona tests are copied from the GoPro project, where they pass against Cocona 2.2.0; if `Cocona.Filters` is reported unused in `CommandConfigurationTests`, keep it, the `TypeCommandDataSource` type lives in `Cocona.Builder`.)

- [x] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~Configuration"`
Expected: build error `CS0246` for `ServiceConfiguration`.

- [x] **Step 3: Write the implementation**

`OsmoVideoRenamer/Configuration/ServiceConfiguration.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OsmoVideoRenamer.ConsoleWrapping;
using OsmoVideoRenamer.Directory;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.CompanionFiles;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.Configuration
{
    public static class ServiceConfiguration
    {
        public static void Configure(IServiceCollection collection)
        {
            collection
                .AddTransient<IConsoleWrapper, ConsoleWrapper>()
                .AddTransient<IFileSystem, FileSystem>()
                .AddTransient<IVideoDirectoryFactory, VideoDirectoryFactory>()
                .AddTransient<IDirectoryFileFactory, DirectoryFileFactory>()
                .AddTransient<IVideoFileFactory, VideoFileFactory>()
                .AddTransient<INumberedVideoFileFactory, NumberedVideoFileFactory>()
                .AddTransient<IRenamedVideoFileFactory, RenamedVideoFileFactory>()
                .AddTransient<IRenamedCompanionFileFactory, RenamedCompanionFileFactory>()
                .AddTransient<ICompanionFileFinder, CompanionFileFinder>()
                .AddTransient<IFileFilter, FileFilter>()
                .AddTransient<IFileSort, FileSort>()
                .AddTransient<IFileRename, FileRename>()
                .AddTransient<IRenameCollisionChecker, RenameCollisionChecker>();
        }
    }
}
```

`OsmoVideoRenamer/Configuration/CommandConfiguration.cs`:

```csharp
using Cocona;
using Cocona.Builder;

namespace OsmoVideoRenamer.Configuration
{
    public static class CommandConfiguration
    {
        public static void RegisterAllCommands(ICoconaCommandsBuilder app)
        {
            app.AddCommands<RenameCommand>();
        }
    }
}
```

`OsmoVideoRenamer/Configuration/FilterConfiguration.cs`:

```csharp
using Cocona;
using Cocona.Builder;
using Cocona.Filters;
using OsmoVideoRenamer.ParameterLogging;

namespace OsmoVideoRenamer.Configuration
{
    public static class FilterConfiguration
    {
        public static void RegisterAllFilters(ICoconaCommandsBuilder app)
        {
            app.UseFilter(new ParameterLoggingCommandFilterFactory());
        }
    }
}
```

`OsmoVideoRenamer/Program.cs` (replace the placeholder entirely):

```csharp
using Cocona;
using OsmoVideoRenamer.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace OsmoVideoRenamer
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        static void Main(string[] args)
        {
            var builder = CoconaApp.CreateBuilder(args);
            ServiceConfiguration.Configure(builder.Services);
            var app = builder.Build();
            FilterConfiguration.RegisterAllFilters(app);
            CommandConfiguration.RegisterAllCommands(app);
            app.Run();
        }
    }
}
```

- [x] **Step 4: Run the tests to verify they pass, then the whole suite**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test --filter "FullyQualifiedName~Configuration"`
Expected: PASS, 15 tests (13 data rows + 2).

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet test`
Expected: PASS, 107 tests, `Failed: 0`.

- [x] **Step 5: Run the real program's help**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet run --project OsmoVideoRenamer -- --help`
Expected: a usage block listing `--file-location`, `--prefix`, `--suffix`, `--starting-number`, `--digit-count`, `--dry-run`, `-h, --help`, `--version`. Keep this output; Task 18 pastes it into the README. On macOS the first line reads `Usage: OsmoVideoRename [...]` with the final letter missing, because the operating system truncates the process name to 15 characters and Cocona prints the process name; that is not a bug in the code.

- [x] **Step 6: Commit**

```bash
cd /Users/charlie/repos/osmo-video-renamer
git add -A
git commit -m "Wire up Cocona configuration and entry point

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 18: README, end-to-end verification, and coverage

**Files:**
- Create: `README.md`

- [x] **Step 1: Write the README**

Replace the `<paste>` block with the exact output of `dotnet run --project OsmoVideoRenamer -- --help` from Task 17, correcting the truncated `OsmoVideoRename` in the `Usage:` line to `OsmoVideoRenamer` (see Task 17 Step 5).

````markdown
# osmo-video-renamer
Renames DJI Osmo Pocket 3 videos so that they appear in the correct order when sorted alphanumerically.

## Usage

```text
~$ OsmoVideoRenamer --help

<paste the --help output here>
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

Only files directly inside the given directory are considered; sub-directories are not
searched. Run the tool once per folder, for example once per `DJI_00n` folder copied from
the card.

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

Companion lines are printed in the order the files appear in the directory, so the `.LRF` and `.WAV` lines may swap.
````

- [x] **Step 2: Build a scratch directory of fake DJI files**

Run:

```bash
E2E=/private/tmp/claude-501/-Users-charlie-repos-osmo-video-renamer/c39be8f0-77d6-4811-8735-caf9e1ad1ae9/scratchpad/e2e
rm -rf "$E2E" && mkdir -p "$E2E" && cd "$E2E"
for name in DJI_20240315120000_0001_D DJI_20240315120500_0002_D DJI_20240315121000_0003_D DJI_20240315121500_0010_D; do
  echo "video" > "$name.MP4"; echo "proxy" > "$name.LRF"
done
echo "audio" > DJI_20240315120000_0001_D.WAV
echo "audio" > DJI_20240315121000_0003_D.WAV
echo "photo" > DJI_20240315120200_0004_D.JPG
echo "raw" > DJI_20240315120200_0004_D.DNG
echo "notes" > notes.txt
ls -1 "$E2E"
```

Expected: 13 files listed (4 `.MP4`, 4 `.LRF`, 2 `.WAV`, 1 `.JPG`, 1 `.DNG`, `notes.txt`).

- [x] **Step 3: Dry run, then verify nothing changed**

Run:

```bash
cd /Users/charlie/repos/osmo-video-renamer
E2E=/private/tmp/claude-501/-Users-charlie-repos-osmo-video-renamer/c39be8f0-77d6-4811-8735-caf9e1ad1ae9/scratchpad/e2e
dotnet run --project OsmoVideoRenamer -- --file-location "$E2E" --prefix "Trip - " --dry-run
ls -1 "$E2E"
```

Expected console lines (log lines from Cocona may appear between them):

```text
1: DJI_20240315120000_0001_D.MP4 -> Trip - 1.MP4
    DJI_20240315120000_0001_D.LRF -> Trip - 1.LRF
    DJI_20240315120000_0001_D.WAV -> Trip - 1.WAV
2: DJI_20240315120500_0002_D.MP4 -> Trip - 2.MP4
    DJI_20240315120500_0002_D.LRF -> Trip - 2.LRF
3: DJI_20240315121000_0003_D.MP4 -> Trip - 3.MP4
    DJI_20240315121000_0003_D.LRF -> Trip - 3.LRF
    DJI_20240315121000_0003_D.WAV -> Trip - 3.WAV
4: DJI_20240315121500_0010_D.MP4 -> Trip - 4.MP4
    DJI_20240315121500_0010_D.LRF -> Trip - 4.LRF
```

(Companion order within a video follows directory listing order, so `.LRF`/`.WAV` may swap.) The `ls` afterwards must show the same 13 original names.

- [x] **Step 4: Real run, then verify the result**

Run:

```bash
cd /Users/charlie/repos/osmo-video-renamer
E2E=/private/tmp/claude-501/-Users-charlie-repos-osmo-video-renamer/c39be8f0-77d6-4811-8735-caf9e1ad1ae9/scratchpad/e2e
dotnet run --project OsmoVideoRenamer -- --file-location "$E2E" --prefix "Trip - " --digit-count 3
ls -1 "$E2E"
```

Expected `ls` (macOS lists `notes.txt` before the `Trip - ...` names; the set of names is what matters):

```text
DJI_20240315120200_0004_D.DNG
DJI_20240315120200_0004_D.JPG
Trip - 001.LRF
Trip - 001.MP4
Trip - 001.WAV
Trip - 002.LRF
Trip - 002.MP4
Trip - 003.LRF
Trip - 003.MP4
Trip - 003.WAV
Trip - 004.LRF
Trip - 004.MP4
notes.txt
```

- [x] **Step 5: Re-run on the renamed directory**

Run: `cd /Users/charlie/repos/osmo-video-renamer && dotnet run --project OsmoVideoRenamer -- --file-location "/private/tmp/claude-501/-Users-charlie-repos-osmo-video-renamer/c39be8f0-77d6-4811-8735-caf9e1ad1ae9/scratchpad/e2e" --prefix "Trip - "`
Expected: `No DJI Osmo videos found in /private/tmp/.../e2e.` and exit code 0.

- [x] **Step 6: Collision and duplicate-counter checks**

Run:

```bash
cd /Users/charlie/repos/osmo-video-renamer
E2E=/private/tmp/claude-501/-Users-charlie-repos-osmo-video-renamer/c39be8f0-77d6-4811-8735-caf9e1ad1ae9/scratchpad/e2e
echo "video" > "$E2E/DJI_20240316090000_0011_D.MP4"
dotnet run --project OsmoVideoRenamer -- --file-location "$E2E" --prefix "Trip - " --digit-count 3; echo "exit code: $?"
ls -1 "$E2E" | grep -c "DJI_20240316090000_0011_D.MP4"
echo "video" > "$E2E/DJI_20240316100000_0011_D.MP4"
dotnet run --project OsmoVideoRenamer -- --file-location "$E2E" --prefix "Other - "; echo "exit code: $?"
```

Expected: the first run fails with an `IOException` whose message names `Trip - 001.MP4` as already existing, non-zero exit code, and the grep prints `1` (the new file was not renamed). The second run fails with an `ArgumentException` about sequence number 11 appearing more than once, non-zero exit code.

- [x] **Step 7: Measure coverage**

Run:

```bash
cd /Users/charlie/repos/osmo-video-renamer
rm -rf OsmoVideoRenamer.UnitTests/TestResults
dotnet test --collect:"XPlat Code Coverage"
grep -o 'line-rate="[0-9.]*"' $(find OsmoVideoRenamer.UnitTests/TestResults -name coverage.cobertura.xml | head -1) | head -1
```

Expected: `Failed: 0` and a first `line-rate` of `0.97` or higher (Program is excluded). If lower, open the cobertura file, find classes with `line-rate` below 1 and add tests for the uncovered lines before continuing.

- [x] **Step 8: Commit and clean up**

```bash
cd /Users/charlie/repos/osmo-video-renamer
rm -rf OsmoVideoRenamer.UnitTests/TestResults
git status --short
git add -A
git commit -m "Add README

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

Expected: `git status` shows only `README.md` before the add (`TestResults/`, `bin/`, `obj/` are ignored). Then update the plan file's checkboxes for every completed task and commit that too.
