# Cli Helper

Cli Helper is a **.NET library** to build command line applications: a tree of commands registered in the dependency injection container, option parsing POSIX style with declarative requirements, and help generated from the commands themselves. It is a library, not a runnable application, published as the single NuGet package `Etherna.CliHelper` and consumed by the Etherna command line tools (e.g. `etherna-gateway-cli`).

## Build, run, test

The library multi-targets **net8.0 and net9.0**; the test and AOT probe projects target **net10.0 only**. There is no `Directory.Build.props`: every `.csproj` carries its own property block. The library sets `TreatWarningsAsErrors=true`, `AnalysisMode=AllEnabledByDefault`, `Nullable=enable`, `EnableNETAnalyzers=true` and `IsAotCompatible=true` — warnings break the build, on every target framework.

```bash
dotnet restore CliHelper.sln
dotnet build CliHelper.sln -c Release                 # compiles every target framework
dotnet test  CliHelper.sln -c Release                 # runs the xUnit test project
dotnet test --filter "FullyQualifiedName~CommandBaseTest"                                        # single class
dotnet test --filter "FullyQualifiedName~CommandOptionsBaseTest.ParseArgsKeepsParsingSingleOptions" # single test
dotnet publish test/CliHelper.AotCompatibility/CliHelper.AotCompatibility.csproj -c Release -r linux-x64   # Native AOT gate
./test/CliHelper.AotCompatibility/bin/Release/net10.0/linux-x64/publish/CliHelper.AotCompatibility       # then run the probe
```

Because the library compiles against the lowest target (`net8.0`), do not use APIs introduced only in a later framework — it passes on `net9.0` and fails the build on `net8.0`. A green `CliHelper.sln` build means every target compiled.

The library is **Native AOT / trim compatible** (`IsAotCompatible=true`). A normal build does not prove it: the gate is the AOT publish of `test/CliHelper.AotCompatibility`, which roots the whole library (`TrimmerRootAssembly`) and fails on any trim/AOT warning, followed by its native binary, a smoke test of the paths that run by reflection. Run both after touching how commands or the io service are created, or after adding a dependency.

There is nothing to run: build, test and the AOT probe are the verification steps.

Versioning is automatic via **GitVersion** (`GitVersion.MsBuild`, default configuration), plus SourceLink. CI (`.github/workflows/`):

- `aot-compatibility.yml` runs the AOT publish and the probe binary on every pull request and on pushes to `main`, and is called by both deploy workflows, which block on it. It is the only pull request check: the unit tests run in CI only in the deploys, so run the suite locally before opening a PR, and read the run of the base branch after the merge.
- `nuget-unstable-deploy.yml`: pushes to `dev`, `hotfix/**` and `release/**` build, test, pack and push unstable packages to nuget.etherna.io (https://nuget.etherna.io/v3/index.json, Etherna's own NuGet server).
- `nuget-stable-deploy.yml`: tags `v*.*.*` build, test, pack and push stable packages to nuget.org. The push carries no stored key: it authenticates with the trusted publishing of nuget.org, where the job asks GitHub for an OIDC token (`permissions: id-token: write`) and `NuGet/login` exchanges it, right before the push, for an API key that lives one hour. The nuget.org policy is bound to the workflow **file name**: renaming the file, or moving the push into another workflow, stops the publish until the policy is updated.

## Architecture

Solution `CliHelper.sln`, one library project and two test projects. The root namespace is `Etherna.CliHelper`; under it, the namespace mirrors the folder path.

- **`src/CliHelper`** (`Etherna.CliHelper`):
  - `ServiceCollectionExtensions.AddCliHelper<TIoService>()` — the entry point. It registers the `IIoService` implementation (transient) and the `ICommandManager` (singleton, holding the io service it was built with), and returns the `ICommandManagerConfiguration` on which the application declares its command tree.
  - `Commands/` — the command tree. `CommandManagerConfiguration` is its root: each `AddCommand<TCommand>(configSubCommands)` adds a `CommandMap` (the command type, its parent, its sub command maps; a map is itself an `ICommandManagerConfiguration`, so sub commands nest through the callback) and registers the command type as transient, built by `ActivatorUtilities` with its constructor dependencies and given the manager through its required `CommandManager` property. `CommandManager` (internal) creates the registered commands from the container and navigates the tree: sub commands, the path from the root, the parent. A command type appears once in the tree.
  - `Commands/Models/` — `CommandBase` is the base of every command: `Description`, `ExecuteAsync(commandArgs)` and the run of a whole path, `RunAsync(args)`. `CommandBase<TOptions>` adds the `Options` instance of the command. `CommandOptionsBase` declares the options of a command (`Definitions`, a list of `CommandOption`) and their `Requirements`; its `ParseArgs` parses options standalone, which a consumer uses to read the root options before building its container. `OptionsParser` (internal) parses against the options of several owners, the commands of a path. `ParsedOption` is what a requirement validates.
  - `Commands/Models/OptionRequirements/` — the declarative requirements, each one validating the parsed options and printing its own help line: `ExclusiveOptionRequirement`, `ForbiddenOptionRequirement`, `IfPresentThenOptionRequirement`, `MaxValueOptionRequirement`, `MinValueOptionRequirement`, `RangeOptionRequirement`, `RequireOneOfOptionRequirement` (which makes the options required: `OPTIONS` instead of `[OPTIONS]` in the usage line), on `OptionRequirementBase`, reporting `OptionRequirementError`s.
  - `Services/` — `IIoService`, the console abstraction every command writes and reads through (`IoService`, inherited from `CommandBase`), and `ConsoleIoService`, its console implementation (errors in dark red).
- **`test/CliHelper.UnitTest`** — xUnit + Moq unit tests of `CliHelper`, mirroring its folder layout.
- **`test/CliHelper.AotCompatibility`** — not a test project: the Native AOT probe, published by CI and run as a native binary. Its `Program` runs a small command tree (`RootCommand`, `GreetCommand`, `ToolsCommand`, `EchoCommand`) through the container and checks the output of each run.

How a run works, the behavior the tests pin and the consumers rely on:

- **The command name is the type name** without `Command`, lowercased (`ResourceCommand` is `resource`): renaming a command type renames the command.
- **`RunAsync(args)` resolves the whole path before executing anything.** Args are sub command names, then the command args; the options of each command of the path go anywhere after the command defining them and before the command args, short options compose POSIX style (`-ab` is `-a -b`, and an option requiring args ends the group, taking the rest of it as its first arg), and the args an option requires are consumed even when they start with `-`. The first arg that is neither an option nor a sub command begins the command args: what follows, options included, is passed as args. Then the requirements of the whole path are verified, and the path is executed from the root down: the default `ExecuteAsync` dispatches to the sub command, so a command overriding it calls `ExecuteSubCommandAsync` to continue the path, and every command of the path receives the same command args.
- **The executed commands are the instances whose options were parsed**, chained through the resolved sub command of each one. `CreateCommand` builds a new instance at every call (help and usage create the commands of the path and the sub commands to read their descriptions and options): never execute a command obtained from a new `CreateCommand` in place of the resolved one, and keep command constructors free of side effects.
- **Help**: `-h` and `--help` print the help of the command reached so far, unless an option of the path defines that name; a command followed by nothing prints its help when `PrintHelpWithNoArgs` is true (the default). Help and usage are generated from the commands, their options and their requirements: there are no hand-written help strings.
- **Errors**: a mistake of the user (an unknown command or option, a duplicated option, missing option args) throws `ArgumentException` with a message meant for the user, which the consumer prints; the violated requirements, all of them, are written through `IIoService.WriteError` before the `ArgumentException` (`Errors with command options`). A mistake of the application (an option name redefined by another command of the path, an unregistered command type) throws `InvalidOperationException`.

Key cross-cutting points:

- **This is a library: always `ConfigureAwait(false)`** on every await (`CA2007` is enforced by the analyzers). This is the opposite of the Etherna ASP.NET Core service repos (beehive, credit, gateway, index, sso), where `ConfigureAwait` is omitted.
- **Public API surface is the product.** The package is published to NuGet and consumed by the Etherna command line tools: keep the public surface intentional, document it with XML docs, and remember both target frameworks must compile. A breaking change is stated in the PR.
- **Native AOT / trim compatible**: commands and the io service are built by reflection over their constructors, so every generic parameter (or `Type`) that reaches `ActivatorUtilities` carries `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]`, as on `AddCliHelper` and `AddCommand`. Extend the checks of the probe when a change touches a path that runs by reflection.

## Issue tracker

Bugs and features are tracked in Jira project **CLIH** (https://etherna.atlassian.net/projects/CLIH). Branch names follow `feature/CLIH-<id>-<slug>` / `improve/CLIH-<id>-<slug>` / `fix/CLIH-<id>-<slug>` — match this when creating branches. `dev` is the integration branch, `main` is stable; stable releases are tagged `v<version>` (e.g. `v0.1.0`), and a hotfix line is a `hotfix/X.Y.Z` branch from `main`.

**Which version is out and which line is open is read from git, never written here**: `git tag --sort=-v:refname | head -1` is the last release, `git branch -r --list 'origin/hotfix/*' 'origin/release/*'` the open lines, and `git log --oneline main..dev` what the next release will carry.

# Coding Style

## General Principles

- Keep commits clean: only include changes strictly necessary for the task at hand.
- A security fix keeps its commit message and PR title neutral: the Jira key and what changes, never the vulnerability. The write-up stays in the Jira work item, hidden until the fixed version is released; outside reporters are directed to [SECURITY.md](SECURITY.md).
- Keep `README.md` aligned: it is packed as the readme of the package, so a change to features, packages or build steps updates it in the same change.
- Never reference AI agents or assistants in commits or code — no agent names, no `Co-Authored-By` agent trailers, no "generated/assisted by" notes. Commit messages and code must read as the team's own work.
- Exceptions to these conventions are accepted when strictly necessary or when they significantly improve code quality. Justify with a comment where needed.
- All elements (usings, properties, methods, fields, enum members, etc.) are always alphabetically ordered within their respective sections.
- Primary constructors are preferred everywhere the constructor is a simple parameter assignment.
- Keep code clean: remove unused variables, dead code, and redundant imports.
- Don't extract a private helper method for logic used in a single place — inline it. Reserve helpers for code shared by two or more call sites (or when extraction materially clarifies an otherwise long, complex method).
- Every source file starts with the standard LGPL-3.0 copyright header (`// Copyright 2024-present Etherna SA`, `// This file is part of Cli Helper.` … see any existing file; `CliHelper.sln.DotSettings` holds it for the IDE). The license texts `COPYING` and `COPYING-LESSER` are packed with the package.

## Naming

- **Classes/Structs**: PascalCase (`CommandManager`, `OptionsParser`)
- **Interfaces**: `I` prefix (`ICommandManager`, `IIoService`)
- **Async methods**: always `Async` suffix (`RunAsync`, `ExecuteSubCommandAsync`)
- **Properties**: PascalCase, with `Is`/`Has` prefix for booleans (`IsRootCommand`, `HasSubCommands`)
- **Private fields**: `_camelCase` only when backing a same-named property (`_commandManager` for `CommandManager`); otherwise plain `camelCase`
- **Primary constructor parameters**: `camelCase` without underscore
- **Constants**: PascalCase (`ErrorForegroundColor`)
- **Enums**: PascalCase type and members
- **Namespaces**: `Etherna.CliHelper.<Feature>` mirroring the folder (e.g. `Etherna.CliHelper.Commands.Models.OptionRequirements`)
- **Commands**: `<Name>Command`, its options class `<Name>CommandOptions`; requirements `<Kind>OptionRequirement`
- **Option names**: short `-x` (one letter or digit), long `--kebab-case`, as `CommandOption` validates

## Code Organization

- One class per file, filename matches class name
- Namespace mirrors folder structure (under the `Etherna.CliHelper` root namespace)
- Block-scoped namespaces: `namespace X { ... }` — NOT file-scoped
- Using directives at the top of the file, before the namespace block, always alphabetically ordered (`System` namespaces are not placed first) and kept to the minimum necessary
- No global usings — each file declares its own imports

## Comments

Principal comments (generally multiline, important):
```csharp
// Capital start, ending period.
// Continued on next line if needed.
```

Secondary/separator comments:
```csharp
//no space, no capital, no ending period
```

Comment only what helps a future reader: non-obvious behavior, intent, or a gotcha. Do **not** write narration of your own reasoning or decisions — that belongs in the commit message / PR description, never in committed code. Comments and docs describe the **present** state only: never reference removed or replaced concepts — git history already tells that story.

Public API members are documented with XML doc comments (`///`), with a well-composed description and no superfluous information; document non-public members too whenever it aids understanding. The library generates its documentation file, packed beside the assembly for the IntelliSense of the consumers: a public member without XML docs fails the build (`CS1591`). A `<summary>` ends with a period, a `<param>`, `<returns>` or `<exception>` doesn't; an override or an interface implementation adding nothing to the contract takes `/// <inheritdoc />`.

## Member Ordering Within a Class

Use principal-style section comments to delimit groups (singular `// Constructor.` when there is only one), in this order:

```csharp
// Internal classes.
private sealed class Owner { ... }

// Consts.
private const int MaxLength = 100;

// Fields.
private readonly List<Item> items = [];

// Constructors.
public MyType(string name) { ... }

// Properties.
public string Name { get; }

// Protected properties.
protected IIoService IoService => ...;

// Internal properties.
internal virtual CommandOptionsBase? CommandOptions => null;

// Methods.
public void DoSomething() { ... }

// Protected methods.
protected virtual Task ExecuteAsync(string[] commandArgs) { ... }

// Protected helpers.
protected static string GetCommandNameFromType(Type commandType) { ... }

// Helpers.
private void InternalHelper() { ... }
```

## Class Design

- `internal sealed` for implementations that aren't part of the public API (`CommandManager`, `OptionsParser`)
- Primary constructors everywhere the constructor is a simple assignment
- Members only the library itself reads stay `internal` (`CommandOptions`), out of the public surface

## Async Patterns

- Always suffix with `Async`
- `CancellationToken cancellationToken = default` as the optional last parameter of new asynchronous APIs (non-nullable, `default` — not `CancellationToken? = null`)
- Return `Task` or `Task<T>`, never `async void`
- **Always use `ConfigureAwait(false)`** in library code
- `Task.CompletedTask` for synchronous implementations of an asynchronous member

## Null Handling

- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- `ArgumentNullException.ThrowIfNull(param)` for parameter validation, without the redundant parameter name
- `is null` / `is not null` (not `== null`)
- Prefer `null` over `default` wherever the type admits it: optional parameter defaults, late-init member initializers (`= null!`, not `= default!`), returns and assignments. Keep `default` only where `null` can't apply: non-nullable value types (e.g. `CancellationToken cancellationToken = default`) and unconstrained generic type parameters.
- `??` and `??=` operators

## Formatting

- Allman braces (opening brace on new line)
- 4-space indentation (2 spaces in `.csproj` files, per `.editorconfig`)
- Expression-bodied members for single expressions:
  ```csharp
  public override bool HasOptions => true;
  ```
- LINQ method chains: one operation per line, aligned
- Blank line between member sections

## C# Language Features

- Pattern matching: `is`, `is not`, type patterns, property patterns (`arg is "-h" or "--help"`)
- Switch expressions for multi-branch returns
- Primary constructors everywhere applicable
- Collection expressions: `[]`, `[..spread]`
- Prefer collection expressions over constructors to initialize any collection: `[]` not `new()`, `["a", "b"]` not `new List<string> { "a", "b" }`. Use a constructor only when a collection expression can't express the intent (e.g. presizing capacity with `new List<T>(capacity)`).
- Target-typed `new()` when type is clear from context (for non-collection types)
- Tuple deconstruction for multiple return values
- Ranges for slicing args (`args[argsIndex..]`)
- Lock fields: prefer the dedicated `System.Threading.Lock` type (.NET 9+) over a plain `object`. Here it applies only once the `net8.0` target is dropped: the lowest-target rule forbids .NET 9+ APIs today.

## LINQ

- Method syntax preferred over query syntax
- Query syntax only for complex join/groupby with multiple `from` clauses
- Fluent chaining, one operation per line for readability

## Testing (xUnit + Moq)

- `[Fact]` for basic tests, `[Theory]` with `[InlineData]` or `[MemberData]` (static `TheoryData` properties, in a `// Data.` section) for parameterized cases
- Test names state the member under test and the behavior, in PascalCase without underscores (`ParseArgsReportsEveryViolatedRequirement`, `RunAsyncKeepsOptionsAfterCommandArgsAsArgs`)
- AAA pattern with section comments: `// Setup.`, `// Action.`, `// Assert.`, in every test; a statement covering two phases, like an `Assert.Throws` returning the exception asserted next, goes under `// Action and assert.`
- xUnit assertions: `Assert.Equal()`, `Assert.NotNull()`, `Assert.Throws<T>()` / `Assert.ThrowsAsync<T>()`
- Moq for mocking a single collaborator (`Mock.Of<IIoService>()`, `new Mock<IIoService>()`); a command run goes through the real composition instead: `AddCliHelper` on a `ServiceCollection`, a fixture command tree drawn in a comment above its classes, and a capturing `IIoService` recording output and errors
- The test project mirrors the folder layout of the library (`Commands/Models/CommandBaseTest.cs` tests `Commands/Models/CommandBase.cs`)
- **No `ConfigureAwait` in test code** — inside tests and their async helpers write plain `await foo()`: the xUnit analyzer rule `xUnit1030` errors on `ConfigureAwait(false)` in a test method, and `TreatWarningsAsErrors` makes it break the build. The library rule applies to `src/` only.
