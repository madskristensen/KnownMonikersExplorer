using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Community.VisualStudio.Toolkit;
using KnownMonikersExplorer.ToolWindows;
using KnownMonikersExplorer.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace KnownMonikersExplorer
{
    [Command(PackageIds.FindMoniker)]
    public class FindMoniker : BaseCommand<FindMoniker>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (TryFindMonikerInElement(Application.Current.MainWindow, out ImageMoniker? moniker))
            {
                if (moniker.HasValue)
                {
                    await ShowMonikerInToolWindowAsync(moniker.Value);
                }
                return;
            }

            // Floating tool windows are not part of the main window, so if the
            // mouse if over an image in a floating tool window, we wouldn't have
            // found the image by hit-testing the main window. Test each tool window.
            foreach (UIElement element in await GetContentFromToolWindowsAsync())
            {
                if (TryFindMonikerInElement(element, out moniker))
                {
                    if (moniker.HasValue)
                    {
                        await ShowMonikerInToolWindowAsync(moniker.Value);
                    }
                    return;
                }
            }

            await VS.MessageBox.ShowAsync(
                "The element under the cursor is not an image moniker.",
                icon: OLEMSGICON.OLEMSGICON_CRITICAL,
                buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
            );
            return;
        }

        private static bool TryFindMonikerInElement(UIElement element, out ImageMoniker? moniker)
        {
            Point point = Mouse.GetPosition(element);
            HitTestResult result = VisualTreeHelper.HitTest(element, point);

            if (result?.VisualHit != null)
            {
                // A `CrispImage` control is usually used to display an
                // `ImageMoniker`, so check if that's what was found first.
                if (result.VisualHit is CrispImage image)
                {
                    moniker = image.Moniker;
                    return true;
                }

                // Some elements display an `ImageMoniker` in other ways (Solution Explorer nodes are an example)
                // If there's a property that contains an `ImageMoniker`, then we'll use the value from that property.
                var monikers = result
                    .VisualHit
                    .GetType()
                    .GetProperties()
                    .Where(x => typeof(ImageMoniker).IsAssignableFrom(x.PropertyType))
                    .Select(x => new ImageMonikerProperty(x.Name, (ImageMoniker)x.GetValue(result.VisualHit)))
                    .Where(x => x.ImageMoniker.Guid != Guid.Empty) // Filter out the properties that are empty.
                    .ToList();

                if (monikers.Count > 0)
                {
                    // The element may have multiple `ImageMoniker` properties, but if they all
                    // contain the same image then there's no need to ask the user which one to use.
                    // If any of the images are different, then we'll get the user to select the image to use.
                    ImageMonikerProperty selectedImageMoniker;
                    if (monikers.Select(x => x.ImageMoniker).Distinct().Skip(1).Any())
                    {
                        var window = new SelectImageMonikerPropertyWindow(monikers);
                        if (window.ShowModal().GetValueOrDefault())
                        {
                            selectedImageMoniker = window.SelectedImageMoniker;
                        }
                        else
                        {
                            // Selecting the property was cancelled. Return true to indicate
                            // that a moniker was found, but set the found moniker to null.
                            moniker = null;
                            return true;
                        }
                    }
                    else
                    {
                        selectedImageMoniker = monikers[0];
                    }

                    moniker = selectedImageMoniker.ImageMoniker;
                    return true;
                }
            }

            moniker = default;
            return false;
        }

        private async Task ShowMonikerInToolWindowAsync(ImageMoniker moniker)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ToolWindowPane toolWindow = await KnownMonikersExplorerWindow.ShowAsync();
            var explorer = toolWindow.Content as KnownMonikersExplorerControl;

            if (!explorer.SelectMoniker(moniker, out var needsClearSearch))
            {
                var window = new UnknownMonikerWindow(moniker)
                {
                    Owner = Application.Current.MainWindow
                };
                window.ShowDialog();
            }
            else if (needsClearSearch)
            {
                // Clear the search box text in the tool window
                toolWindow.ClearSearch();
            }
            return;
        }

        private static async Task<IEnumerable<UIElement>> GetContentFromToolWindowsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var windows = new List<UIElement>();
            IVsUIShell uiShell = await VS.Services.GetUIShellAsync();

            ErrorHandler.ThrowOnFailure(uiShell.GetToolWindowEnum(out IEnumWindowFrames windowEnumerator));
            var frame = new IVsWindowFrame[1];

            while (true)
            {
                ErrorHandler.ThrowOnFailure(windowEnumerator.Next(1, frame, out var fetched));
                if (fetched == 1)
                {
                    if (TryGetContentFromToolWindow(frame[0], out UIElement content))
                    {
                        windows.Add(content);
                    }
                }
                else
                {
                    break;
                }
            }

            return windows;
        }

        private static bool TryGetContentFromToolWindow(IVsWindowFrame frame, out UIElement content)
        {
            // The `IVsWindowFrame` doesn't expose the content publicly, but there should be a `RootView` property
            // that returns a `View` object (which is an internal type) which has a `Content` property.
            PropertyInfo viewProperty = frame.GetType().GetProperty("RootView");
            if (viewProperty != null)
            {
                var view = viewProperty.GetValue(frame);
                PropertyInfo contentProperty = view.GetType().GetProperty("Content");
                if (contentProperty != null)
                {
                    content = contentProperty.GetValue(view) as UIElement;
                    if (content != null)
                    {
                        return true;
                    }
                }
            }

            content = null;
            return false;
        }
    }
}
