﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FluxHueBridge
{
    public class DelegateCommand : ICommand
    {
        public Action? CommandAction { get; set; }
        public Func<bool>? CanExecuteFunc { get; set; }

        public void Execute(object? parameter)
        {
            if (CommandAction == null) return;

            CommandAction();
        }

        public bool CanExecute(object? parameter)
        {
            return CanExecuteFunc == null || CanExecuteFunc();
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
