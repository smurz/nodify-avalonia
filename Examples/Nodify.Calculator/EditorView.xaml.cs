using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Avalonia.Input;

namespace Nodify.Calculator
{
    public partial class EditorView : UserControl
    {
        // Avalonia 12 replaced DataObject/DragDrop.DoDragDrop with the IDataTransfer pipeline.
        // CreateInProcessFormat carries the live CLR reference across the in-process drag —
        // exactly what the v11 DataObject.Set(name, obj) did, just typed.
        private static readonly Avalonia.Input.DataFormat<OperationInfoViewModel> OperationFormat =
            Avalonia.Input.DataFormat.CreateInProcessFormat<OperationInfoViewModel>(
                typeof(OperationInfoViewModel).FullName!);

        public EditorView()
        {
            InitializeComponent();

            PointerPressedEvent.AddClassHandler<NodifyEditor>(CloseOperationsMenuPointerPressed);
            ItemContainer.DragStartedEvent.AddClassHandler<ItemContainer>(CloseOperationsMenu);
            PointerReleasedEvent.AddClassHandler<NodifyEditor>(OpenOperationsMenu);
            Editor.AddHandler(Avalonia.Input.DragDrop.DropEvent, OnDropNode);
        }

        private void OpenOperationsMenu(object? sender, PointerReleasedEventArgs e)
        {
            if (!e.Handled && e.Source is NodifyEditor editor && !editor.IsPanning && editor.DataContext is CalculatorViewModel calculator &&
                e.InitialPressMouseButton == MouseButton.Right)
            {
                e.Handled = true;
                calculator.OperationsMenu.OpenAt(editor.MouseLocation);
            }
        }

        private void CloseOperationsMenuPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                CloseOperationsMenu(sender, e);
        }

        private void CloseOperationsMenu(object? sender, RoutedEventArgs e)
        {
            ItemContainer? itemContainer = sender as ItemContainer;
            NodifyEditor? editor = sender as NodifyEditor ?? itemContainer?.Editor;

            if (!e.Handled && editor?.DataContext is CalculatorViewModel calculator)
            {
                calculator.OperationsMenu.Close();
            }
        }

        private void OnDropNode(object? sender, Avalonia.Input.DragEventArgs e)
        {
            NodifyEditor? editor = (e.Source as NodifyEditor) ?? (e.Source as Control)?.GetLogicalParent() as NodifyEditor;
            if (editor != null && editor.DataContext is CalculatorViewModel calculator
                && e.DataTransfer.TryGetValue(OperationFormat) is { } operation)
            {
                OperationViewModel op = OperationFactory.GetOperation(operation);
                op.Location = editor.GetLocationInsideEditor(e);
                calculator.Operations.Add(op);

                e.Handled = true;
            }
        }

        private async void OnNodeDrag(object? sender, MouseEventArgs e)
        {
            if (!leftButtonPressed || _pressEvent is null
                || ((Control)sender!).DataContext is not OperationInfoViewModel operation)
                return;

            // DoDragDropAsync now requires the original PointerPressedEventArgs that initiated
            // the gesture (v11 took the move/press event indiscriminately). Captured in OnNodePressed.
            var trigger = _pressEvent;
            _pressEvent = null;
            leftButtonPressed = false;

            var transfer = new Avalonia.Input.DataTransfer();
            transfer.Add(Avalonia.Input.DataTransferItem.Create(OperationFormat, operation));
            await Avalonia.Input.DragDrop.DoDragDropAsync(trigger, transfer, Avalonia.Input.DragDropEffects.Copy);
        }

        private void OnNodePressed(object? sender, PointerPressedEventArgs e)
        {
            leftButtonPressed = e.GetCurrentPoint(this).Properties.PointerUpdateKind ==
                                PointerUpdateKind.LeftButtonPressed;
            _pressEvent = leftButtonPressed ? e : null;
        }

        private void OnNodeExited(object? sender, PointerEventArgs e)
        {
            leftButtonPressed = false;
            _pressEvent = null;
        }

        private bool leftButtonPressed;
        private PointerPressedEventArgs? _pressEvent;
    }
}
