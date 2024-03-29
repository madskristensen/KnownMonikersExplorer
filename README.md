# KnownMonikers Explorer

[![Build status](https://ci.appveyor.com/api/projects/status/85xmotii0u1n5rtd?svg=true)](https://ci.appveyor.com/project/madskristensen/knownmonikersexplorer)

**Requires Visual Studio 2019 or newer**

Provides a tool window for Visual Studio extension authors that lets you easily browse all the image monikers in the **KnownMonikers** catalog.

Download this extension from the [Marketplace](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.KnownMonikersExplorer)
or get the [CI build](http://vsixgallery.com/extension/c38f3512-4653-4d97-a4c5-860a425209f5/).

----------------------------------------------

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

## Find a moniker used in Visual Studio

Hover the mouse over an image moniker in Visual Studio (for example, an image on a toolbar button) and press **Ctrl+Shift+Alt+Q**. The KnownMonikers Explorer window will open and highlight the image moniker.

![Find Moniker](art/find-moniker.gif)

## Related resources

* [Image Service and Catalog](https://msdn.microsoft.com/en-US/library/mt628927.aspx)
* [Image Libray Viewer](https://msdn.microsoft.com/en-us/library/mt629250.aspx)
* [Glyph List](http://glyphlist.azurewebsites.net/)

## License
* This extension: [Apache 2.0](LICENSE)
* [License for the images](https://www.microsoft.com/download/details.aspx?id=35825)