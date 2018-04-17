# KnownMonikers Explorer

[![Build status](https://ci.appveyor.com/api/projects/status/85xmotii0u1n5rtd?svg=true)](https://ci.appveyor.com/project/madskristensen/knownmonikersexplorer)

**Requires Visual Studio 2017.6 or newer**

Provides a tool window for Visual Studio extension authors that lets you easily browse all the image monikers in the **KnownMonikers** catalog.

Open the tool window from the top menu **View -> Other Windows -> KnownMonikers Explorer**.

Filter the image monikers by typing in the search box.

![Tool window](art/toolwindow.png)

Right-click any moniker for a list of actions to take.

![Context Menu](art/context-menu.png)

## Export...
Export the image moniker to file in the size you specify. PNG, JPEG and Gif formats are supported

![Export Dialog](art/export-dialog.png)

Hitting the **Enter** key will also show the export dialog.

## Copy to clipboard
This will copy the name of the moniker to the clipboard. You can also use **Ctrl+C** to do the same.

## Related resources

* [Image Service and Catalog](https://msdn.microsoft.com/en-US/library/mt628927.aspx)
* [Image Libray Viewer](https://msdn.microsoft.com/en-us/library/mt629250.aspx)
* [Glyph List](http://glyphlist.azurewebsites.net/)

## License
[Apache 2.0](LICENSE)