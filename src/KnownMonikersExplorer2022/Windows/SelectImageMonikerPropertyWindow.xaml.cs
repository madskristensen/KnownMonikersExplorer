using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace KnownMonikersExplorer.Windows
{
    public partial class SelectImageMonikerPropertyWindow : DialogWindow
    {
        public SelectImageMonikerPropertyWindow(IReadOnlyList<ImageMonikerProperty> properties)
        {
            ImageMonikers = properties;
            SelectedImageMoniker = ImageMonikers[0];
            DataContext = this;
            InitializeComponent();
        }

        public IReadOnlyList<ImageMonikerProperty> ImageMonikers { get; }

        public ImageMonikerProperty SelectedImageMoniker { get; set; }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            SelectedImageMoniker = (ImageMonikerProperty)((ListBoxItem)e.Source).DataContext;
            DialogResult = true;
        }

    }
}
