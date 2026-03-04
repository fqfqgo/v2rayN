namespace v2rayN.Views;

public partial class SubEditWindow
{
    private readonly bool _focusLoginPassword;

    public SubEditWindow(SubItem subItem, bool focusLoginPassword = false)
    {
        InitializeComponent();
        _focusLoginPassword = focusLoginPassword;

        Owner = Application.Current.MainWindow;
        Loaded += Window_Loaded;
        chkShowPassword.Checked += ChkShowPassword_Changed;
        chkShowPassword.Unchecked += ChkShowPassword_Changed;
        pwdLoginPassword.PasswordChanged += PwdLoginPassword_PasswordChanged;

        ViewModel = new SubEditViewModel(subItem, UpdateViewHandler);

        cmbConvertTarget.ItemsSource = Global.SubConvertTargets;

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.txtRemarks.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Url, v => v.txtUrl.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.MoreUrl, v => v.txtMoreUrl.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.LoginPassword, v => v.txtLoginPassword.Text).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedSource.Enabled, v => v.togEnable.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.AutoUpdateInterval, v => v.txtAutoUpdateInterval.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.UserAgent, v => v.txtUserAgent.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Sort, v => v.txtSort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Filter, v => v.txtFilter.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.ConvertTarget, v => v.cmbConvertTarget.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.PrevProfile, v => v.txtPrevProfile.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.NextProfile, v => v.txtNextProfile.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.PreSocksPort, v => v.txtPreSocksPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Memo, v => v.txtMemo.Text).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SaveCmd, v => v.btnSave).DisposeWith(disposables);
        });
        WindowsUtils.SetDarkBorder(this, AppManager.Instance.Config.UiItem.CurrentTheme);
    }

    private async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        switch (action)
        {
            case EViewAction.CloseWindow:
                DialogResult = true;
                break;
        }
        return await Task.FromResult(true);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        pwdLoginPassword.Password = ViewModel?.SelectedSource?.LoginPassword ?? "";
        if (_focusLoginPassword)
        {
            if (chkShowPassword.IsChecked == true)
            {
                txtLoginPassword.Focus();
                txtLoginPassword.SelectAll();
            }
            else
            {
                pwdLoginPassword.Focus();
            }
            return;
        }
        txtRemarks.Focus();
    }

    private void ChkShowPassword_Changed(object sender, RoutedEventArgs e)
    {
        if (chkShowPassword.IsChecked == true)
        {
            txtLoginPassword.Text = pwdLoginPassword.Password;
            txtLoginPassword.Visibility = Visibility.Visible;
            pwdLoginPassword.Visibility = Visibility.Collapsed;
        }
        else
        {
            pwdLoginPassword.Password = txtLoginPassword.Text ?? "";
            pwdLoginPassword.Visibility = Visibility.Visible;
            txtLoginPassword.Visibility = Visibility.Collapsed;
        }
    }

    private void PwdLoginPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && pwdLoginPassword.Visibility == Visibility.Visible)
        {
            ViewModel.SelectedSource.LoginPassword = pwdLoginPassword.Password;
        }
    }


    private async void BtnSelectPrevProfile_Click(object sender, RoutedEventArgs e)
    {
        var selectWindow = new ProfilesSelectWindow();
        selectWindow.SetConfigTypeFilter([EConfigType.Custom], exclude: true);
        if (selectWindow.ShowDialog() == true)
        {
            var profile = await selectWindow.ProfileItem;
            if (profile != null)
            {
                txtPrevProfile.Text = profile.Remarks;
            }
        }
    }

    private async void BtnSelectNextProfile_Click(object sender, RoutedEventArgs e)
    {
        var selectWindow = new ProfilesSelectWindow();
        selectWindow.SetConfigTypeFilter([EConfigType.Custom], exclude: true);
        if (selectWindow.ShowDialog() == true)
        {
            var profile = await selectWindow.ProfileItem;
            if (profile != null)
            {
                txtNextProfile.Text = profile.Remarks;
            }
        }
    }
}
