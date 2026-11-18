using System.Windows;

namespace RecorderStudio
{
    public interface IUserDialogs
    {
        bool Confirm(Window owner, string message, string title);
        void Inform(Window owner, string message, string title, bool warning);
    }

    public sealed class UserDialogs : IUserDialogs
    {
        public bool Confirm(Window owner, string message, string title)
        {
            return MessageBox.Show(owner, UiText.T(message), UiText.T(title), MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No, UiText.DialogOptions) == MessageBoxResult.Yes;
        }

        public void Inform(Window owner, string message, string title, bool warning)
        {
            MessageBox.Show(owner, UiText.T(message), UiText.T(title), MessageBoxButton.OK,
                warning ? MessageBoxImage.Warning : MessageBoxImage.Information, MessageBoxResult.OK, UiText.DialogOptions);
        }
    }
}
