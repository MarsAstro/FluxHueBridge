using System.Windows;
using System.Linq;
using System.Windows.Input;

namespace FluxHueBridge
{
    public class TaskbarIconViewModel
    {
        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => !IsMainWindowOpen(),
                    CommandAction = () =>
                    {
                        Application.Current.MainWindow = new MainWindow();
                        Application.Current.MainWindow.Show();
                    }
                };
            }
        }

        public ICommand HideWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () => Application.Current.MainWindow.Close(),
                    CanExecuteFunc = () => IsMainWindowOpen()
                };
            }
        }

        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand { CommandAction = () => Application.Current.Shutdown() };
            }
        }

        private bool IsMainWindowOpen()
        {
            return Application.Current.Windows.OfType<Window>().Any(w => w is MainWindow);
        }
    }
}
