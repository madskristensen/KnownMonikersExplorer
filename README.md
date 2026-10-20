[marketplace]: <https://marketplace.visualstudio.com/items?itemName=MadsKristensen.KnownMonikersExplorer2022>
[vsixgallery]: <https://www.vsixgallery.com/extension/c38f3512-4653-4d97-a4c5-860a425209f6>
[repo]: <https://github.com/madskristensen/KnownMonikersExplorer>

# KnownMonikers Explorer

[![Build status](https://github.com/madskristensen/KnownMonikersExplorer/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/KnownMonikersExplorer/actions/workflows/build.yaml)
![GitHub Sponsors](https://img.shields.io/github/sponsors/madskristensen)

Download this extension from the [Visual Studio Marketplace][marketplace] or get the [CI build][vsixgallery]

--------------------------------------

**A tool window for Visual Studio extension authors** that lets you easily browse all the image monikers in the KnownMonikers catalog.

## Features

### Browse and Search

Open the tool window from **View → Other Windows → KnownMonikers Explorer** and filter image monikers by typing in the search box.

The window displays a loading message while the catalog initializes. Search and Find Moniker wait for initialization, and clearing or replacing a search cancels the previous request.

![Tool window](art/toolwindow.png)

### Context Menu Actions

Right-click any moniker for a list of actions:

![Context Menu](art/context-menu.png)

| Action | Shortcut | Description                                        |
| ------ | -------- | -------------------------------------------------- |
| Export | `Enter`  | Export moniker to PNG, JPEG, or GIF at custom size |
| Copy   | `Ctrl+C` | Copy moniker name to clipboard                     |

### Export Dialog

Export the image moniker to file in the size you specify. PNG, JPEG and GIF formats are supported.

Image widths from 1 to 4096 pixels are supported. Invalid sizes disable export, unsupported file extensions are rejected, and export failures are shown in the dialog with diagnostic details logged to the Output window.

![Export Dialog](art/export-dialog.png)

### Find Moniker in Visual Studio

Hover the mouse over an image moniker in Visual Studio (for example, an image on a toolbar button) and press `Ctrl+Shift+Alt+Q`. The KnownMonikers Explorer window will open and highlight the image moniker.

![Find Moniker](art/find-moniker.gif)

## Requirements

- Visual Studio 2022
- Visual Studio 2026

## Building from source

Open `KnownMonikersExplorer.slnx` in Visual Studio with the **Visual Studio extension development** workload and the .NET Framework 4.8 targeting pack installed. The extension uses an SDK-style VSIX project.

Build with Visual Studio MSBuild:

```powershell
msbuild KnownMonikersExplorer.slnx /restore /p:Configuration=Release /p:DeployExtension=false
```

The VSIX is written to `src\KnownMonikersExplorer2022\bin\Release\KnownMonikersExplorer2022.vsix`. Use **Debug / Any CPU** and press **F5** to deploy and debug in the experimental instance.

Assembly metadata remains in `Properties\AssemblyInfo.cs` so it continues to use the manifest-generated `Vsix` constants and CI version stamping.

### Tests

The solution includes .NET Framework 4.8 MSTest regression tests. Run them from Visual Studio Test Explorer or, after building, use:

```powershell
dotnet test tests\KnownMonikersExplorer.Tests\KnownMonikersExplorer.Tests.csproj --configuration Release --no-build --no-restore --settings tests\KnownMonikersExplorer.Tests\CodeCoverage.runsettings --collect "Code Coverage;Format=Cobertura"
```

The GitHub Actions build runs the tests before publishing, reports TRX results, summarizes line and branch coverage, and uploads test results and coverage reports.

## Related Resources

- [Image Service and Catalog](https://msdn.microsoft.com/en-US/library/mt628927.aspx)
- [Image Library Viewer](https://msdn.microsoft.com/en-us/library/mt629250.aspx)
- [Glyph List](http://glyphlist.azurewebsites.net/)

## License

- This extension: [Apache 2.0](LICENSE)
- [License for the images](https://www.microsoft.com/download/details.aspx?id=35825)

## How can I help?

Give it a rating on the [Visual Studio Marketplace][marketplace] or report issues on [GitHub][repo].
