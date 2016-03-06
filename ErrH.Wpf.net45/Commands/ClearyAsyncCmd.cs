﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ErrH.Wpf.net45.Helpers;
using PropertyChanged;

namespace ErrH.Wpf.net45.Commands
{
    // https://msdn.microsoft.com/en-us/magazine/dn630647.aspx
    public static class ClearyAsyncCmd
    {
        public static ClearyAsyncCmd<object> Create(Func<Task> command, string idleLabel = null, string executingLabel = null)
            => new ClearyAsyncCmd<object>(async () 
                => { await command(); return null; }, idleLabel, executingLabel);

        public static ClearyAsyncCmd<TResult> Create<TResult>(Func<Task<TResult>> command, string idleLabel = null, string executingLabel = null)
            => new ClearyAsyncCmd<TResult>(command, idleLabel, executingLabel);
    }



    [ImplementPropertyChanged]
    public class ClearyAsyncCmd<TResult> : ClearyAsyncCmdBase//, INotifyPropertyChanged
    {
        private readonly Func<Task<TResult>>  _command;

        public NotifyTaskCompletion<TResult> Execution { get; private set; }

        public ClearyAsyncCmd(Func<Task<TResult>> command, string idleLabel = null, string executingLabel = null)
        {
            _command       = command;
            IdleLabel      = idleLabel;
            ExecutingLabel = executingLabel;
            CurrentLabel   = IdleLabel;
        }


        public override bool CanExecute(object parameter)
            => Execution == null || Execution.IsCompleted;


        public override async Task ExecuteAsync(object parameter)
        {
            Execution = new NotifyTaskCompletion<TResult>(_command());
            RaiseCanExecuteChanged();
            await Execution.Completion;
            RaiseCanExecuteChanged();
        }
    }


    [ImplementPropertyChanged]
    public abstract class ClearyAsyncCmdBase : IAsyncCommand
    {
        public event EventHandler CanExecuteChanged
        {
            add    { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public string    CurrentLabel      { get; protected set; }
        public string    IdleLabel         { get; protected set; }
        public string    ExecutingLabel    { get; protected set; }
        public bool      IsRunning         { get; private set; }

        public abstract bool   CanExecute    (object parameter);
        public abstract Task   ExecuteAsync  (object parameter);


        public async void Execute(object parameter)
        {
            IsRunning    = true;
            CurrentLabel = ExecutingLabel;
            await ExecuteAsync(parameter);
            CurrentLabel = IdleLabel;
            IsRunning    = false;
        }

        protected void RaiseCanExecuteChanged()
            => CommandManager.InvalidateRequerySuggested();
    }


    public interface IAsyncCommand : ICommand
    {
        Task     ExecuteAsync   (object parameter);
        string   CurrentLabel   { get; }
        bool     IsRunning      { get; }
    }
}