using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class MessageBoxDialog : Window
{
    public MessageBoxDialog()
        : this(string.Empty, string.Empty)
    {
    }

    public MessageBoxDialog(string caption, string message, bool showCancel = true)
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            caption = "Design Caption";
            message = "Design Message";
        }

        Title = caption;
        txtMessage.Text = message;
        btnNo.IsVisible = showCancel;

        btnYes.Click += BtnYes_Click;
        btnNo.Click += BtnNo_Click;

        CanMinimize = false;
    }

    private void BtnYes_Click(object? sender, RoutedEventArgs e)
    {
        Close(ButtonResult.Yes);
    }

    private void BtnNo_Click(object? sender, RoutedEventArgs e)
    {
        Close(ButtonResult.No);
    }
}
