using Avalonia.Controls;

namespace DemoForAvaloniaUI
{
    public partial class InputDialog : Window
    {
        public string? Result { get; private set; }

        public InputDialog()
        {
            InitializeComponent();
        }

        private void OnOkClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Result = this.FindControl<TextBox>("InputTextBox")!.Text;
            Close(Result);
        }

        private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
