﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ErrH.WpfTools.Commands
{
    public abstract class AsyncCommandBase : IAsyncCommand
    {
        public event EventHandler CanExecuteChanged
        {
            add    { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }


        public abstract bool  CanExecute   (object parameter);
        public abstract Task  ExecuteAsync (object parameter);


        public async void Execute(object parameter)
            => await ExecuteAsync(parameter);


        protected void RaiseCanExecuteChanged()
            => CommandManager.InvalidateRequerySuggested();
    }
}
