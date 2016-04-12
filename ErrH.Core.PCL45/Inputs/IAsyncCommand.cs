﻿using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ErrH.Core.PCL45.Inputs
{
    public interface IAsyncCommand : ICommand, INotifyPropertyChanged
    {
        Task     ExecuteAsync   (object parameter);

        string   CurrentLabel    { get; set; }
        string   IdleLabel       { get; }
        string   ExecutingLabel  { get; }
        string   FinishedLabel   { get; set; }

        bool     IsRunning       { get; }
        bool     IsEnabled       { get; set; }
    }
}
