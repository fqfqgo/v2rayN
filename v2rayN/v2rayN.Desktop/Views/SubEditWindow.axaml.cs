using v2rayN.Desktop.Base;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class SubEditWindow : WindowBase<SubEditViewModel>
{
    public SubEditWindow()
    {
        InitializeComponent();

        Loaded += Window_Loaded;
        btnCancel.Click += (s, e) => Close();
        chkShowLoginPassword.IsCheckedChanged += (_, _) =>
        {
            pwdLoginPassword.IsVisible = chkShowLoginPassword.IsChecked != true;
            txtLoginPassword.IsVisible = !pwdLoginPassword.IsVisible;
            (txtLoginPassword.IsVisible ? txtLoginPassword : pwdLoginPassword).Focus();
        };

        cmbConvertTarget.ItemsSource = Global.SubConvertTargets;
        cmbCustomCoreType.ItemsSource = Utils.GetEnumNames<ECoreType>().Where(t => t != nameof(ECoreType.v2rayN)).ToList().AppendEmpty();

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.txtRemarks.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Url, v => v.txtUrl.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.MoreUrl, v => v.txtMoreUrl.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Enabled, v => v.togEnable.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.AutoUpdateInterval, v => v.txtAutoUpdateInterval.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.UserAgent, v => v.txtUserAgent.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Sort, v => v.txtSort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Filter, v => v.txtFilter.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.ConvertTarget, v => v.cmbConvertTarget.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PrevProfile, v => v.txtPrevProfile.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.NextProfile, v => v.txtNextProfile.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.PreSocksPort, v => v.txtPreSocksPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Memo, v => v.txtMemo.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.LoginPassword, v => v.pwdLoginPassword.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.LoginPassword, v => v.txtLoginPassword.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CustomCoreType, v => v.cmbCustomCoreType.SelectedValue).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SelectPrevProfileCmd, v => v.btnSelectPrevProfile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SelectNextProfileCmd, v => v.btnSelectNextProfile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SaveCmd, v => v.btnSave).DisposeWith(disposables);

            ViewModel.ShowMsgInteraction.RegisterHandler(async interaction =>
            {
                await UI.Show(interaction.Input);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);
        });
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.FocusLoginPassword == true)
        {
            pwdLoginPassword.Focus();
            return;
        }
        txtRemarks.Focus();
    }
}
