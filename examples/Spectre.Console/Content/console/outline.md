Great. I’ll generate two separate Diátaxis-structured documentation outlines for Spectre.Console and Spectre.Console.Cli. Each outline will include Tutorials, How-To Guides, Explanations, and Reference sections, and will be organized two levels deep. Widgets and AnsiConsole commands will be grouped by category. I’ll base this on the current documentation, GitHub repositories, examples, and relevant blog articles.

I’ll let you know once the outlines are ready.


# Spectre.Console Documentation Outline

Spectre.Console is a library for creating rich console output in .NET, including color text, tables, live progress, and interactive prompts. The documentation is organized using the Diátaxis framework, with separate sections for Tutorials, How-To Guides, Reference, and Explanation, ensuring both new and advanced users can find relevant information.

## Tutorials

### **Getting Started: Building a Rich Console App**

A beginner-friendly tutorial that walks through creating a simple console application using Spectre.Console. It covers installation of the NuGet package and basic setup. The tutorial guides the reader to output a colored “Hello World” using the **Markup** syntax, then incrementally adds features: creating a **Table** with some data, styling text with **Colors** and **Styles**, and finally using a **Progress Bar** to simulate a long-running task. By the end, the reader will have a working app demonstrating text styling, table rendering, and a progress indicator.

### **Interactive Prompt and Dashboard Tutorial**

An intermediate tutorial focused on building an interactive console “dashboard.” This guides the reader through creating a menu-driven application that asks the user for input and displays dynamic output. It shows how to use **Prompt** methods (e.g. `AnsiConsole.Ask`, `Confirm`) to gather input, including a multi-selection list for choices. It then demonstrates updating a live **Status** spinner while performing a task, and updating a **Live Display** (live-rendered table or chart) with new data periodically. This tutorial gives a hands-on feel for Spectre.Console’s interactive features, combining **Widgets** like Tables or Charts with real-time updates.

## How-To Guides

### **Styling Text with Markup and Color**

How to output text with rich styles and colors using Spectre.Console’s markup language. This guide explains the markup syntax (e.g. `[red]text[/]` for colored text, `[bold]` for bold) and lists available style names (bold, dim, italic, etc.). It shows how to combine styles and colors in text, how to escape markup when needed (using `Markup.Escape` to avoid errors with `[` characters), and how to use the `AnsiConsole.MarkupLine` convenience method. Readers will learn best practices like not assuming background color and using safe color contrasts. Examples include coloring portions of text, making ASCII banners with **Figlet** fonts, and drawing horizontal rules with **Rule** for separators.

### **Displaying Tables and Trees**

How to present structured data in the console using **Table** and **Tree** widgets. This guide covers creating a `Table` with columns, adding rows of data, and customizing borders and styles (using different border types and column alignment). It also covers the **Tree** widget for hierarchical data, showing how to build a tree of nodes and style each node. Practical examples include a table of results from an API and a file system directory tree. The guide demonstrates using built-in **Borders** styles (from simple to double lines) and how to handle wide content gracefully. By following this, users can format data into easy-to-read tables or expandable trees.

### **Organizing Layout with Panels and Grids**

How to arrange multiple pieces of output using layout widgets such as **Panel**, **Grid**, **Columns**, **Rows**, and **Align**. This guide shows how to use `Panel` to draw a bordered panel around content (e.g. to call out an important message), and how to use `Align` and `Padder` to position content within a region. It introduces the **Grid** layout for placing content in a flexible row-by-column arrangement (like a table without explicit borders), as well as using `Columns`/`Rows` containers and the higher-level **Layout** system to divide the console into regions. Examples include creating a two-column layout (using `Columns` or `Grid`) to display side-by-side panels, and using `Align.Center` to center text or ASCII art in the console. The guide ensures every container widget (Align, Padder, Panel, Grid, etc.) is demonstrated so developers can effectively organize complex console UIs.

### **Drawing Charts and Diagrams**

How to create simple charts and visualizations in the console. This guide covers the **BarChart** widget for rendering bar graphs and the **BreakdownChart** for pie-chart-like breakdowns of percentages. It explains how to supply data series, labels, and customize colors for each segment. Additionally, it shows how to display a **Calendar** widget to render a monthly calendar view in text (useful for highlighting dates) and how to use the **Rule** widget to draw separators (which can act as a visual timeline or section break). By following the steps, readers can generate console-based charts (for example, a bar chart of task durations or a breakdown of categories) to visually represent data in a text environment.

### **Rendering ASCII Art and Figlet Text**

How to add decorative text and art to the console. This guide focuses on using the **Figlet** text renderer to create large ASCII-art text from strings (great for banners or headings), covering how to choose fonts and apply colors to Figlet output. It also mentions using emojis in text (via the `:shortcode:` syntax) to enrich output with icons. The guide provides examples of printing a stylized application title with Figlet, drawing a line with `Rule` for emphasis, and sprinkling some emoji symbols for fun. Tips are included on ensuring the console font supports the Unicode characters (as Spectre.Console can auto-detect and fallback for unsupported characters).

### **Using Canvas for Pixel Art and Images**

How to draw graphics in the console using the **Canvas** and **CanvasImage** widgets. The guide explains creating a `Canvas` with a specific size and plotting colored “pixels” on it to draw shapes or simple images (e.g. a smiley face or a low-res logo). It then shows how to use `CanvasImage` to take an image (or a 2D data array) and render an ASCII representation in the console. Readers will learn how to control color fidelity (knowing Spectre.Console supports 3-bit to 24-bit colors depending on terminal capabilities) and how to scale images or adjust dithering for better console representation. By the end, developers will be able to include basic graphics alongside text output.

### **Formatting JSON and File Paths**

How to output structured data like JSON and file system paths in a readable way. This guide shows how to use the **JsonHighlight** (JSON) widget to pretty-print JSON strings or objects with syntax highlighting of braces, keys, and values. It covers customizing the colors for different JSON elements if needed. The guide also covers the **TextPath** widget, which is used to render file or directory paths in a truncated, friendly format. It demonstrates how a long file path can be automatically cropped to fit the console width and styled (e.g. drive, separators, and folder names in different colors). By using these widgets, developers can display config files, data, or file locations in a user-friendly manner without writing custom formatting logic.

### **Prompting for User Input**

How to interactively prompt the user for input using Spectre.Console. This guide covers the various prompt utilities:

* **Ask<T>**: Asking for a free-form input of a specific type (e.g. string, int) with optional default. It shows how to prompt for a value and handle the returned typed result.
* **Confirm**: Presenting a yes/no question to the user, with examples of confirmation dialogs and default values (yes/no).
* **SelectionPrompt**: Creating a menu of options for the user to select one (arrow-key navigation).
* **MultiSelectionPrompt**: Allowing the user to choose multiple items from a list (with checkboxes).
* **TextPrompt**: (If distinct from Ask) Configuring input with validation or masking (for passwords).
  This guide will include code snippets to illustrate each type of prompt, such as asking for the user’s name, confirming an action, or choosing from a list of items. It also provides tips on customizing prompt appearance and ensuring prompts do not conflict with other live output.

### **Showing Progress Bars and Spinners**

How to track and display progress of long-running tasks in the console. This guide demonstrates using `AnsiConsole.Progress` to create a **Progress Bar** with multiple tasks, updating their completion percentage or status messages. It explains how to start the progress, update task progress in code, and finish gracefully, leveraging the fluent API to configure appearance (e.g. auto refresh rate, finished behavior). The guide also covers using the **Status** widget to show a single live spinner with a status message (for example, “Processing…” with an animated spinner). Configuration of spinners is discussed (choosing a spinner style from the built-in set, or customizing the spinner frames). Examples include a single long task (using `Status.Start`) and multiple parallel tasks (with `Progress().Start` and tasks). The guide notes that multiple live-updating elements should not run concurrently to avoid flicker.

### **Live Rendering and Dynamic Updates**

How to use Spectre.Console’s live rendering features to continuously update console output. This guide focuses on the **LiveDisplay** mechanism (`AnsiConsole.Live`) which allows re-rendering a widget in-place as data changes. It shows how to wrap an `IRenderable` (like a Table or Chart) in a live context and periodically update its content within a loop. For example, a live table that adds new rows every second, or updating a progress chart in real-time. The guide provides an example of a ticking dashboard: perhaps CPU usage chart that updates, or a list of tasks that refresh statuses. It emphasizes best practices for live rendering (e.g. keep updates on a single thread, don’t mix multiple live renderers simultaneously) to avoid conflicts. By following this, users can create dynamic console interfaces that update in real time.

### **Running Tasks with an Async Spinner**

How to run asynchronous tasks with a spinner animation using Spectre.Console’s async extensions. This guide shows how to call the extension methods like `.Spinner()` on a `Task` or `Task<T>` to automatically display a spinner while the task runs. It covers customizing the spinner type (choosing one of the built-in spinner presets or defining a custom sequence) and styling the spinner (color, style). The guide also notes limitations: the inline spinner is not thread-safe to use alongside other interactive elements, so it should be used for standalone tasks. An example is provided where a long-running computation (`Task.Delay` or a data fetch) is run with a spinner indicator, making it easy to give feedback during async operations with minimal code.

### **Formatting and Handling Exceptions**

How to output exceptions in a readable, color-highlighted format. This guide covers using `AnsiConsole.WriteException` to render an `Exception` object with Spectre.Console’s default exception style (which highlights stack trace, message, and inner exceptions in colors). It explains the options via `ExceptionFormats` (such as shortening paths, skipping method info, etc.) to tailor the output. The guide also touches on best practices for handling exceptions in console apps – for example, using `AnsiConsole.WriteException(ex)` inside catch blocks to ensure consistent formatting. An example shows a try/catch where an exception is intentionally thrown and caught to demonstrate the console output. This guide ensures developers can make debugging and error messages user-friendly, aligning with Spectre.Console’s capabilities.

### **Advanced Console Control (Clear, Alternate Screen, Record/Export)**

How to use Spectre.Console for advanced console operations. This guide is a grab-bag of utilities:

* **Clearing the Console**: Using `AnsiConsole.Clear()` to clear the screen.
* **Alternate Screen Buffer**: Using `AnsiConsole.AlternateScreen` to switch to an alternate console buffer for full-screen like applications, and disposing it to return to the main screen (useful for temporary UI like text editors in console).
* **Recording and Exporting Output**: Using `AnsiConsole.Record()` to capture console output programmatically, then `AnsiConsole.ExportText()` or `ExportHtml()` to get the output as a string or HTML. This can be useful for logging or creating reports of console output.
* **Resetting Styles**: Using `AnsiConsole.ResetColors()` and `ResetDecoration()` to restore console to default state (useful after applying many style changes).
  This guide provides short examples for each operation, such as clearing the screen between steps of a demo, or exporting console output to an HTML file for viewing results in a browser. It helps developers take advantage of Spectre.Console’s integration with the console’s lower-level capabilities.

### **Testing Console Output with Spectre.Console.Testing**

How to write unit tests for console applications built with Spectre.Console. This guide introduces the `Spectre.Console.Testing` library and how it provides test harnesses for console output. It shows how to use `TestConsole` to simulate a console – capturing output and supplying input. For Spectre.Console rendering, it demonstrates writing tests that verify the output string (for example, ensuring a certain table or message was written). For interactive prompts, it shows how to queue input responses (via `TestConsole.Input`) so that when code calls `AnsiConsole.Prompt` or `Ask`, the test provides predefined answers. Additionally, if using Spectre.Console.Cli (the CLI parser) together, it explains the `CommandAppTester` which can execute a command and capture its output and exit code. The guide emphasizes structuring console code to inject an `IAnsiConsole` (as shown in the examples) for testability. By following this, developers can confidently validate their console apps’ behavior automatically.

## Reference

### **Color Reference**

A comprehensive reference of color usage in Spectre.Console. This page lists the ways to specify colors: named **ANSI colors** (like `red`, `green`, `blue`, etc.), the 256-color palette indexes, and 24-bit hex colors. It enumerates the 16 basic color names and explains that Spectre.Console auto-detects the terminal’s color support (3-bit, 4-bit, 8-bit, or 24-bit). Examples show using `Color.Red` in code versus markup `[red]`. It also notes how foreground and background colors can be set, and how to reset to defaults. This reference ensures users know all available color specifications for consistent theming.

### **Text Style Reference**

A listing of text styles and decoration options. It describes the style flags like **bold**, **dim**, **italic**, **underline**, **strikethrough**, etc., and how to apply them via markup tags or the `Style` class. It includes examples of combining styles (e.g. bold+underline) and notes which styles might not be supported on all consoles. Additionally, this reference covers the **Decoration** property on `AnsiConsole` (for setting global decoration) and how to reset decorations. It provides a quick lookup for all style names that Spectre.Console recognizes in markup.

### **Border Styles Reference**

A reference page for table and panel border styles. Spectre.Console provides several preset border types (e.g. **Ascii**, **Rounded**, **Square**, **Double**, **Heavy**). This page shows each border style with an example table so users can see how they look. It lists the constant names (if any, like `TableBorder.Rounded`) and explains when to use them (e.g. ASCII for maximum compatibility vs. rounded for nicer aesthetics). This helps users quickly pick a border for tables, panels, or grids to match their desired console look.

### **Emoji Cheat Sheet**

A list of all emoji shortcodes supported by Spectre.Console’s Markup. It shows the text code (like `:smile:`) and the corresponding Unicode symbol 😀. This reference makes it easy to find an emoji to include in console output without remembering the Unicode. It also notes that emoji rendering depends on the console’s font and that not all consoles support color emoji. Example usage is given (e.g. `AnsiConsole.Markup("Hello :wave:")`). This ensures developers can add some visual flair using emoji icons in a supported way.

### **Spinner Styles Reference**

A reference of built-in spinner animations available for the **Status** and **Spinner** APIs. It lists each spinner by name (e.g. `Spinner.Known.Dots`, `Spinner.Known.Star`, etc.) and provides a preview of the pattern (e.g. for Dots, a sequence of dot characters frames). For each spinner, it notes the symbol sequence and interval. The page also explains how to create a custom spinner by specifying a series of frames manually. By consulting this list, users can choose an appropriate spinner animation for their long-running tasks or use it with the async spinner extension.

### **API Reference**

Links to the complete API reference for Spectre.Console (auto-generated documentation). This includes detailed descriptions of all classes, methods, and properties not covered in the guides. For example, the full `AnsiConsole` API with all overloads, the `IRenderable` interface, `Profile` and `Capabilities` details, etc. Readers are directed here for low-level details and types. (This will likely link to the Spectre.Console API docs website or generated docs.)

## Explanation

### **Understanding Spectre.Console’s Rendering Model**

An in-depth explanation of how Spectre.Console renders text and widgets to the terminal. This article discusses the console **Capabilities** detection (how it checks for ANSI support, Unicode capability, terminal type, etc.) and how that influences rendering. It explains concepts like measuring content width and auto-adjusting to console window size, and how Spectre.Console avoids flicker by rendering off-screen (if applicable) then updating. It also covers how **IRenderable** objects work – describing that most widgets implement `IRenderable` and the console simply calls their render logic. This section might also touch on performance considerations and how updating works (e.g. the render loop for live widgets). It gives readers a mental model of what happens when they call `AnsiConsole.Write` or update a live widget, enhancing their understanding of the library’s internals.

### **Best Practices for Console Applications**

General guidance and recommended practices when using Spectre.Console, distilled from the library authors’ experience. This explanation includes:

* **Output best practices**: Test in multiple terminal environments, avoid hard-coding unicode characters or emoji without fallbacks, and consider users’ terminal background colors (don’t assume a black background; use the default 16 colors when possible for theming).
* **Live rendering best practices**: Use a single thread for rendering updates, do not run two live animations (e.g. Progress and Status) at once, and keep the UI responsive by doing heavy work on background threads but rendering on the main thread.
* **Prompting and input**: Suggest injecting an `IAnsiConsole` into business logic so that it can be mocked for testing, and avoiding calling `AnsiConsole` statically inside commands (as shown in the unit testing example).
  This section reads as a set of guidelines and rationales, helping developers avoid common pitfalls and write robust console apps.

### **Spectre.Console vs Traditional Console Output**

A conceptual comparison that explains what Spectre.Console offers over standard `System.Console`. It briefly outlines the shortcomings of basic console printing (no built-in color, manual spacing for tables, etc.) and how Spectre.Console addresses those with higher-level abstractions (markup, panels, tables, etc.). It might highlight the inspiration from the Python Rich library, explaining how Spectre.Console takes similar approaches in a .NET context. This is more of a background discussion, giving context on why to use Spectre.Console and how it fits into building modern CLI applications.

### **Extending Spectre.Console with Custom Renderables**

An explanation of how developers can create their own widgets or renderable components. It discusses the `IRenderable` interface and the expected implementation of the `Render` method to output a **Segment** sequence (or other renderable content). It might walk through a conceptual example of a custom renderable (e.g. a simple “progress pie” text graphic) without full code, explaining how to integrate it so that `AnsiConsole.Render(myRenderable)` will work. This section helps advanced users understand the extension points of the library and how Spectre.Console is designed for flexibility.

---

# Spectre.Console.Cli Documentation Outline

Spectre.Console.Cli is a companion library providing a **command-line parser and app framework** for .NET console applications. It helps structure complex CLI apps (with multiple commands, options, and subcommands) in a strongly-typed way, while following familiar conventions (like Git or `dotnet` CLI). The documentation for Spectre.Console.Cli is also organized via Diátaxis, guiding users from basic tutorials through detailed reference and conceptual topics.

## Tutorials

### **Quick Start: Your First CLI App**

A beginner tutorial that shows how to create a simple command-line application with Spectre.Console.Cli. It walks through installing the Spectre.Console.Cli NuGet package and setting up a single **Command**. The tutorial uses a “Hello World” style example: defining a `HelloCommand` with an option (e.g. `--name`) and a setting class with that option, then configuring a `CommandApp` to use this command. The steps include writing the `Execute` method to output a greeting (possibly using Spectre.Console for colored output) and running the app to parse arguments. By the end, the user can run the compiled app with `--name Alice` to see a personalized greeting. This tutorial introduces the basic patterns: creating a `Command<TSettings>` class, a nested `CommandSettings` class with `[CommandOption]` or `[CommandArgument]`, and using `CommandApp.Run(args)` to parse and execute.

### **Tutorial: Building a Multi-Command CLI Tool**

An intermediate tutorial that expands to multiple commands and subcommands, illustrating Spectre.Console.Cli’s support for complex CLI structures. Using a real-world scenario (e.g. a simple version control CLI or a file utility), it guides the user to create several commands with a shared theme. The tutorial covers:

* Defining multiple `CommandSettings` classes (some possibly inheriting from a common base for shared options).
* Creating corresponding `Command` classes for each (e.g. `AddCommand`, `CommitCommand`, etc.) with their `Execute` logic.
* Composing the commands into a hierarchy using `app.Configure(config => config.AddBranch(...)...)` for subcommands (for example, a top-level "add" command with subcommands "package" and "reference").
* Running and testing the CLI with various arguments (`app.exe add package --version 1.0`).
  This tutorial emphasizes how to structure the code cleanly via composition and shows the automatically generated help output for the commands. By completing it, the reader will understand how to build and organize a CLI with multiple verbs and nested commands.

## How-To Guides

### **Defining Commands and Arguments**

How to declare command-line parameters (arguments and options) using Spectre.Console.Cli’s attributes and settings classes. This guide covers creating a class that inherits `CommandSettings` and using:

* `[CommandArgument]` for positional arguments, with examples of required (`<angle brackets>`) vs optional (`[square brackets]`) syntax in the attribute name. It explains the significance of the position index and how only one argument can gather multiple values via arrays (the argument vector).
* `[CommandOption]` for named options (flags/switches), demonstrating short vs long form (`-c|--count`). It also shows boolean flags (which don’t require a value; specifying the flag sets true) and how to hide an option from help (`IsHidden = true`).
* Using .NET’s `[Description]` attribute on properties to provide help text, and `[DefaultValue]` to supply a default if the option is not provided.
  Through a clear example (perhaps a “greet” command that takes an optional name and a repeat count), the guide illustrates how to define robust command inputs. It also notes that Spectre.Console.Cli will automatically generate help and usage messages from these definitions.

### **Implementing Command Handlers**

How to create the logic for commands by implementing the `Command` classes. This guide explains the difference between inheriting from `Command<TSettings>` vs `AsyncCommand<TSettings>`, and when to use each (sync vs async execution). It covers writing the `Execute(CommandContext, TSettings)` method where the business logic goes, and returning an exit code (0 for success, non-zero for error). A simple example is given (e.g. a `HelloCommand` that reads `settings.Name` and prints a greeting). The guide also discusses:

* **Dependency Injection in commands**: how Spectre.Console.Cli supports constructor injection for commands if a DI container/registrar is configured. For instance, showing a command that takes an `ILoggingService` via its constructor.
* **Validation**: using the `Validate` method override in a Command to perform pre-execution validation of settings. An example demonstrates checking that a file path provided as an argument exists, returning `ValidationResult.Error` with a message if not. This prevents execution if inputs are invalid.
  By following this guide, developers will learn to flesh out command classes with the needed logic and safety checks, leveraging base class features like validation and DI.

### **Configuring CommandApp and Commands**

How to register commands with the `CommandApp` and configure global settings. This guide covers the use of `CommandApp.Configure(...)` to add commands to the application. It shows basic registration with `config.AddCommand<T>("name")` for each command, and describes how to add multiple commands (e.g., a list of top-level commands like “add”, “commit”, “push” for a git-like CLI). It then details the fluent configuration options:

* **Aliases**: using `.WithAlias("alias")` to add alternate names for a command.
* **Descriptions**: `.WithDescription("text")` to set the help text summary for the command.
* **Examples**: `.WithExample(new[] {...})` to provide usage examples that will appear in help.
  The guide also touches on global settings via `config.Settings`: for instance, enabling exception propagation or validation of examples in DEBUG builds. It might mention `config.SetApplicationName` or other metadata if available. By going through this, readers can properly wire up their commands into the app and fine-tune how they appear and behave in the CLI.

### **Working with Multiple Command Hierarchies**

How to create hierarchical (nested) commands using branching. This guide specifically addresses scenarios where commands have subcommands (like `git add` having subcommands in future, or `dotnet tool install/uninstall`). It explains using `AddBranch<TSettings>("name", branch => { ... })` to create a grouping command that isn’t directly executable but routes to subcommands. The example from the documentation is used: a top-level “add” command that shares common settings, and two subcommands “package” and “reference” each with their own specific settings and command classes. The guide shows how the `AddBranch` takes a base settings type (for shared options like maybe a “project” in the example) and how subcommands inherit those settings. It also covers that you can nest branches further for deeper hierarchies. After reading this, users will know how to structure complex CLI command trees and understand that the type system (inheritance of settings classes) helps reuse common parameters across subcommands.

### **Dependency Injection in CLI Commands**

How to integrate a DI container with Spectre.Console.Cli for injecting services into commands. This guide walks through setting up an `ITypeRegistrar` for your preferred DI library. It provides an example using **Microsoft Extensions DI** (via `ServiceCollection`):

* Register services (e.g. add a singleton for an interface).
* Implement `ITypeRegistrar` and `ITypeResolver` or use the provided base classes to adapt the container.
* Pass the registrar into the `CommandApp` constructor (`new CommandApp(registrar)` or `CommandApp<DefaultCommand>(registrar)`).
  The guide references the available example in the docs where a custom `MyTypeRegistrar` is used to hook into Microsoft DI. It then shows how a command can declare a dependency in its constructor (like a database or logger service) and Spectre.Console.Cli will resolve it when running the command. Tips on testing the TypeRegistrar using `TypeRegistrarBaseTests.RunAllTests()` from Spectre.Console.Testing are mentioned to ensure the DI integration is correct. By using this guide, developers can compose their CLI app using dependency injection for cleaner, testable command code.

### **Handling Errors and Exit Codes**

How Spectre.Console.Cli deals with exceptions and how to customize error handling. This guide explains the default behavior: any unhandled exception in a command results in an error message to the console and an exit code of -1. It then shows ways to override this:

* **PropagateExceptions**: Setting `config.PropagateExceptions()` to let exceptions bubble up to your `Main` method. The guide demonstrates wrapping `app.Run(args)` in a try-catch in Program.Main, catching exceptions and using `AnsiConsole.WriteException` (from Spectre.Console) to print them, then returning a custom exit code.
* **Custom Exception Handler**: Using `config.SetExceptionHandler(...)` to intercept exceptions. It shows both overloads: one where you return an int (to set a specific exit code), and one where you don’t (using a default exit code). An example is given where the handler prints the exception in a formatted way and returns, say, -99 as the exit code.
  The guide also notes that these handlers catch exceptions thrown during command parsing or execution, and that using them can centralize error reporting. By following this, readers can implement robust error handling strategies for their CLI tools.

### **Customizing Help Text and Usage**

How to tailor the automatically generated help output of Spectre.Console.Cli. This guide covers:

* Providing a **High-level App Description** and examples: using `config.SetApplicationName` (if available) and ensuring top-level examples are set via `.WithExample` on default command registration, so that running `app.exe --help` shows a summary and usage.
* **Styling the help**: adjusting `HelpProviderStyle` via `config.Settings.HelpProviderStyles`. It gives an example of changing the style of the description header to bold or even setting `HelpProviderStyles = null` to remove all styling for plain output – helpful for accessibility or plain text needs.
* Hiding commands or options from help: reminding that `.IsHidden()` on a command or `IsHidden=true` on an option will omit them from the help listing (for advanced or internal commands).
* **Custom Help Provider**: if needed, how to replace the help system entirely by implementing `IHelpProvider` and calling `config.SetHelpProvider(new CustomHelpProvider())`. The guide likely references the existence of an example on GitHub for a custom help provider.
  By using this guide, users can fine-tune how their CLI’s help and usage information is presented, ensuring end-users get clear instructions.

### **Intercepting Command Execution**

How to use command interceptors to run logic before or after any command executes. This guide explains the `ICommandInterceptor` interface and how to register an interceptor via the DI container or by using `config.SetInterceptor(...)` on the CommandApp. It describes the two methods:

* `Intercept(CommandContext, CommandSettings)` – called *before* the command’s `Execute`, where you can modify settings or perform setup (e.g. configure logging, initialize a database, etc.).
* `InterceptResult(CommandContext, CommandSettings, int exitCode)` – called *after* the command execution, where you can inspect or alter the result (exit code) and do teardown (e.g. flush logs, dispose resources).
  The guide provides an example scenario: an interceptor that starts a logging scope in `Intercept` and closes it or adjusts exit code in `InterceptResult`. It references the documentation’s example of using an interceptor for logging with Serilog. It also notes that interceptors run around every command invocation, making them suitable for cross-cutting concerns (like timing, logging, or setting up global state) without cluttering individual command code.

### **Testing Command-Line Applications**

How to test CLI apps built with Spectre.Console.Cli to ensure they parse and execute correctly. This guide introduces the `CommandAppTester` class from Spectre.Console.Testing, which allows running commands in-memory. It shows how to set up a test: instantiate a `CommandAppTester`, register commands (or even pass in a registrar for DI if needed), then call `app.Run(args)` and capture the `CommandAppResult`. The guide demonstrates asserting on the `ExitCode` and captured `Output` string to verify that a given input produces the expected outcome (for example, running `app.Run(new[]{"hello","--name","Bob"})` yields exit code 0 and output contains "Hello Bob"). It also covers testing interactive commands by using `TestConsole` – for instance, feeding input to a prompt-driven command and verifying the output. Best practices are discussed, such as injecting `IAnsiConsole` into commands rather than using `AnsiConsole` directly, which makes it easier to capture output in tests. Following this guide, developers can automate testing of their CLI argument parsing and command logic, catching regressions or incorrect behaviors early.

## Reference

### **Attribute and Parameter Reference**

A summary of all attributes and parameter-related features in Spectre.Console.Cli. This reference lists:

* **CommandArgumentAttribute** – its constructor (position, name) and how angle vs square bracket notation works to denote required/optional.
* **CommandOptionAttribute** – its constructor (aliases string) and properties like `IsHidden`. Includes notes on boolean flags and how default values are handled.
* **DefaultValueAttribute** (from System.ComponentModel) – mention that Spectre.Console.Cli honors it for options/arguments defaults.
* **DescriptionAttribute** (System.ComponentModel) – used for help text.
* **TypeConverter support** – note that custom types can be bound by providing a TypeConverter (e.g. converting a string to a complex object or enum).
* Possibly **CommandSettings** features – e.g., if `CommandSettings` has methods like `Validate` that can be overridden (though typically one would override in Command, not settings).
  This page serves as a quick lookup for how to decorate command properties and what each attribute does, without going into usage scenarios (which are in how-to guides).

### **Configuration API Reference**

A reference page enumerating the methods and properties available on the `CommandApp` configuration object and related classes:

* **CommandApp.Configure** – mention how to call it and that inside the lambda you can use `AddCommand`, `AddBranch`, etc.
* **Configurator (IConfigurator)** – list methods like `AddCommand<T>`, `AddBranch<T>`, and extension methods like `.WithAlias`, `.WithDescription`, `.WithExample`, `.IsHidden` with brief descriptions.
* **Config.Settings** – list relevant properties in `CommandAppSettings` such as `CaseSensitivity`, `StrictParsing`, `ValidateExamples`, `PropagateExceptions`, `ApplicationName`, `HelpProviderStyles`, etc., and what they control.
* **SetInterceptor**, **SetExceptionHandler**, **SetHelpProvider** – summarize these configuration hooks and reference to where in docs they are explained.
  This reference acts as an index of the fluent configuration API for those who want to see all options at a glance.

### **Built-in Command Behaviors**

A reference describing Spectre.Console.Cli’s built-in behaviors and conventions for completeness. This could include:

* The default **Help** option (`-h/--help`) that’s automatically available and how it triggers help output.
* The **--version** option if one exists by default (not sure if Spectre.Console.Cli provides a default version flag; if so, document it).
* How unrecognized commands or arguments are handled (perhaps throwing a `CommandParseException`).
* The default parsing rules (e.g., `--` to stop parsing options, handling of quotes in arguments which is mostly done by the shell, etc.).
* Mention of **CommandContext** – that each command’s Execute gets a `CommandContext` object which contains the raw arguments, remaining args, and parent commands (if any).
  This section is more for advanced users to know the framework’s default behaviors and how it conforms to typical CLI standards (for example, how it deals with case sensitivity, or combining short options).

### **Extensibility Points**

A reference page on extending or integrating with Spectre.Console.Cli beyond the basics. It could list:

* **ITypeRegistrar / ITypeResolver** – brief description of these interfaces for DI integration and that you can implement them or use provided helpers.
* **ICommandInterceptor** – recap of how to create one and that multiple can be registered via DI.
* **IRemainingArguments (if exists)** – some CLI frameworks have ways to capture all unmatched tail arguments; if Spectre.Console.Cli supports something like this or a special attribute for remaining args, document it.
* **Custom Parsing** – if advanced scenarios require manual parsing, note if Spectre.Console.Cli allows hooking or if one should pre-process args.
  This reference ensures that if developers need to extend functionality (like using a custom help provider or integrating with a config file), they know what extension interfaces are available.

### **API Reference**

Link to the full Spectre.Console.Cli API documentation for all classes and members. This is where one would find detailed info on classes like `CommandApp`, `Command`, `CommandSettings`, exceptions thrown by the library (e.g. `Spectre.Console.Cli.CommandParseException`), and so on. The docs will likely direct users here for any API detail not covered in the how-to or reference pages.

## Explanation

### **Design Philosophy: Convention over Configuration**

An explanation of the guiding philosophy behind Spectre.Console.Cli. This section discusses how the library is **opinionated** in following established CLI conventions – for example, the way options are named (single `-` for short, `--` for long), the automatic help generation, and the enforcement of a structured command pattern. It explains why the library uses the .NET type system (attributes and generic classes) to define commands rather than manual parsing: to catch errors at compile-time and provide a clear separation of concerns. The concept of composition is highlighted: commands and settings are separate, which encourages reuse and cleaner code, as demonstrated by the “add” command example. This narrative may reference how this approach leads to easier testing and maintenance. Essentially, this is a behind-the-scenes rationale that helps users understand the “why” of the design, not just the “how.”

### **Spectre.Console.Cli vs. Spectre.Cli (Migration Guide)**

While a step-by-step migration is in the How-To or Reference, this explanation provides context for those familiar with the older Spectre.Cli library. It outlines what changed conceptually when the functionality moved into Spectre.Console.Cli. Key points include:

* The merging of libraries (Spectre.Cli is no longer updated; Spectre.Console.Cli is the path forward).
* **Namespace changes**: everything now lives under `Spectre.Console.Cli` instead of `Spectre.Cli`.
* Minor breaking changes: for instance, exceptions namespace moved (no more `Spectre.Cli.Exceptions`), and possibly any class or API renames.
* Improvements in the new library (if any) – e.g., better help text styling or new features like interceptors that may not have existed in Spectre.Cli.
  This section doesn’t just list the steps to migrate (the how-to does that), but explains why the split was made (perhaps to unify development under one umbrella) and reassures that the new CLI library aligns with Spectre.Console’s patterns. It’s useful for context and for convincing teams to upgrade.

### **Command Lifecycle and Execution Flow**

An explanatory deep-dive into what happens from the moment `app.Run(args)` is called to when a command finishes execution. It describes:

* **Parsing Phase**: how the arguments array is parsed against the configured commands and settings, how Spectre.Console.Cli matches arguments to `CommandSettings` properties (and provides errors if something is wrong).
* **Validation Phase**: how and when the library calls `Validate()` on settings or commands (e.g., does it validate CommandSettings by invoking an optional `Validate` method on settings class or just the command’s override as documented).
* **Execution Phase**: how the appropriate `Command` instance is constructed (using DI if available), then `Execute` or `ExecuteAsync` is called.
* **Post-Execution**: handling the result (the int exit code) and any exception propagation or interception.
* **Help invocation**: mention that if `--help` is detected, the above phases short-circuit to display help instead of executing a command.
  This explanation might include a simple flow diagram or description: Input args -> Parser selects Command -> Settings populated -> If parse errors, show error/help -> If ok, create Command -> (Interceptor before) -> Execute -> (Interceptor after) -> return exit code. By understanding this flow, users can reason about behaviors like why their code in `Execute` might not run (e.g. if parsing failed or validation failed first).

### **Comparison with Other CLI Libraries**

A high-level comparison that situates Spectre.Console.Cli in the ecosystem (optional, but could be useful conceptually). It might briefly compare to frameworks like **System.CommandLine**, **CommandLineParser**, or others, highlighting Spectre.Console.Cli’s unique approach (heavy use of attributes and class hierarchy, integrated with Spectre.Console for output). It explains the trade-offs of this approach: for instance, being opinionated means less flexibility in some parsing scenarios, but usually a quicker setup for common patterns. This is more of an editorial piece to help advanced users or those evaluating libraries to understand Spectre.Console.Cli’s strengths (like strong typing, built-in DI support, automatic help) versus alternatives. (If the official docs prefer not to mention other libraries, this section could be omitted or kept generic.)
