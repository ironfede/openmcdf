using System.Windows.Input;

namespace StructuredStorageExplorerPOC.Types;
public class CommandHandler : ICommand
{
    private Action _action;
    private bool _canExecute;
    public CommandHandler(Action action, bool canExecute)
    {
        _action = action;
        _canExecute = canExecute;
    }
    public bool CanExecute(object parameter)
    {
        return _canExecute;
    }
    public event EventHandler CanExecuteChanged;
    public void Execute(object parameter)
    {
        _action();
    }
}
