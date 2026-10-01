using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Almoxarifado;

internal static class ValidatedGridSave
{
    private sealed class State
    {
        public bool Busy;
        public object? Committing;
    }
    private static readonly ConditionalWeakTable<RadGridView, State> States = new();

    public static void Save(object sender, GridViewRowValidatingEventArgs e, Func<Task> save)
    {
        if (sender is not RadGridView grid || !e.IsValid || e.Row?.IsInEditMode != true) return;
        var state = States.GetOrCreateValue(grid);
        var item = e.Row.Item;
        if (ReferenceEquals(state.Committing, item)) return;
        e.IsValid = false;
        if (state.Busy) return;
        state.Busy = true;
        grid.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
        {
            if (!ReferenceEquals(e.Row.Item, item) || !e.Row.IsInEditMode)
            {
                state.Busy = false;
                return;
            }
            var hitTest = grid.IsHitTestVisible;
            KeyEventHandler blockKeys = (_, args) => args.Handled = true;
            grid.PreviewKeyDown += blockKeys;
            grid.IsHitTestVisible = false;
            try
            {
                await save();
                state.Committing = item;
                grid.CommitEdit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Dados nao gravados. Corrija a linha ou pressione ESC.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                state.Committing = null;
                state.Busy = false;
                grid.IsHitTestVisible = hitTest;
                grid.PreviewKeyDown -= blockKeys;
            }
        }));
    }
}
