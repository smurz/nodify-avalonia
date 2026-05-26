using System.Windows.Controls;

namespace Nodify.Playground
{
    public partial class NodifyEditorView : UserControl
    {
        public NodifyEditor EditorInstance => Editor;

        public NodifyEditorView()
        {
            InitializeComponent();
            // Avalonia 12 XAML compiler only wires events whose handler is EventHandler<RoutedEventArgs>
            // (generic-arg derivatives like EventHandler<ZoomEventArgs> aren't recognised by the
            // attribute syntax). Wire it in code-behind instead.
            Minimap.Zoom += Minimap_Zoom;
        }

        private void Minimap_Zoom(object? sender, ZoomEventArgs e)
        {
            EditorInstance.ZoomAtPosition(e.Zoom, e.Location);
        }
    }
}
