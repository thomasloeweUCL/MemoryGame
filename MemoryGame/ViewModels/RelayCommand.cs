using System;
using System.Windows.Input;

namespace MemoryGame.ViewModels
{
    // A generic command whose logic is "relayed" from other objects by invoking delegates.
    // This class simplifies the implementation of the ICommand interface.
    public class RelayCommand : ICommand
    {
        // A delegate for the method to be executed when the command is invoked.
        private readonly Action<object> _execute;

        // A delegate for the method that determines whether the command can execute in its current state.
        private readonly Func<object, bool> _canExecute;

        // This event is automatically managed by the CommandManager. It tells UI elements
        // that are bound to this command to re-evaluate the CanExecute method.
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Constructor. It requires an 'execute' action and can optionally take a 'canExecute' function.
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            // The execute action is mandatory.
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // The method that determines if the command can be executed.
        // It's called by UI elements (like a Button) to enable or disable themselves.
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        // The method that is called when the command is invoked (e.g., when a button is clicked).
        public void Execute(object parameter) => _execute(parameter);
    }
}