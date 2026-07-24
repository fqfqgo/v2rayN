namespace ServiceLib.ViewModels;

public class SubEditViewModel : MyReactiveObject
{
    [Reactive]
    public SubItem SelectedSource { get; set; }

    public ReactiveCommand<Unit, Unit> SaveCmd { get; }

    public SubEditViewModel(SubItem subItem, Func<EViewAction, object?, Task<bool>>? updateView)
    {
        _config = AppManager.Instance.Config;
        _updateView = updateView;

        SaveCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SaveSubAsync();
        });

        SelectedSource = subItem.Id.IsNullOrEmpty() ? subItem : JsonUtils.DeepCopy(subItem);
    }

    private async Task SaveSubAsync()
    {
        var remarks = SelectedSource.Remarks;
        if (remarks.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseFillRemarks);
            return;
        }

        var url = SelectedSource.Url?.Trim();
        if (url.IsNotEmpty())
        {
            // 订阅地址只能是单行、以 http(s):// 开头，不能是 vmess:// / vless:// 等节点链接
            if (url.Any(c => c is '\r' or '\n')
                || !(url.StartsWith(Global.HttpsProtocol, StringComparison.OrdinalIgnoreCase)
                     || url.StartsWith(Global.HttpProtocol, StringComparison.OrdinalIgnoreCase)))
            {
                NoticeManager.Instance.Enqueue(ResUI.InvalidSubUrlFormatTip);
                return;
            }
            var uri = Utils.TryUri(url);
            if (uri == null)
            {
                NoticeManager.Instance.Enqueue(ResUI.InvalidUrlTip);
                return;
            }
            //Do not allow http protocol
            if (url.StartsWith(Global.HttpProtocol, StringComparison.OrdinalIgnoreCase) && !Utils.IsPrivateNetwork(uri.IdnHost))
            {
                NoticeManager.Instance.Enqueue(ResUI.InsecureUrlProtocol);
                //return;
            }
            // 可达性校验：拿到任意 HTTP 响应（含 403/503）即视为通；仅解析失败/超时等视为无效
            if (!await HttpClientHelper.Instance.CheckReachableAsync(url))
            {
                NoticeManager.Instance.Enqueue(ResUI.SubUrlUnreachableTip);
                return;
            }
        }
        SelectedSource.Url = url;

        if (await ConfigHandler.AddSubItem(_config, SelectedSource) == 0)
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
            _updateView?.Invoke(EViewAction.CloseWindow, null);
        }
        else
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
        }
    }
}
