using NetDimension.NanUI.Resources;
using Xilium.CefGlue;
using static Vanara.PInvoke.User32;

namespace NetDimension.NanUI.Browser;

internal sealed class WinFormiumLifeSpanHandler : CefLifeSpanHandler
{
    private readonly Formium _owner;

    public WinFormiumLifeSpanHandler(Formium owner)
    {
        _owner = owner;

    }

    protected override void OnAfterCreated(CefBrowser browser)
    {
        base.OnAfterCreated(browser);

        _owner.WebView.OnBrowserCreated(browser);
    }

    protected override bool DoClose(CefBrowser browser)
    {
        return base.DoClose(browser);
    }

    protected override void OnBeforeClose(CefBrowser browser)
    {
        _owner?.WebView?.MessageBridge?.OnBeforeClose(browser);
    }



    protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
    {

        var e = new BeforePopupEventArgs(frame, targetUrl, targetFrameName, userGesture, popupFeatures, windowInfo, client, settings, noJavascriptAccess);

        _owner.InvokeIfRequired(() => _owner.OnBeforePopup(e));




        if (e.Handled == false)
        {
            var cefBounds = new CefRectangle();

            if (popupFeatures.X.HasValue)
            {
                cefBounds.X = popupFeatures.X.Value;
            }


            if (popupFeatures.Y.HasValue)
            {
                cefBounds.Y = popupFeatures.Y.Value;
            }

            if (popupFeatures.Width.HasValue)
            {
                cefBounds.Width = popupFeatures.Width.Value;
            }
            else
            {
                cefBounds.Width = _owner.Width;
            }

            if (popupFeatures.Height.HasValue)
            {
                cefBounds.Height = popupFeatures.Height.Value;
            }
            else
            {
                cefBounds.Height = _owner.Height;
            }

            windowInfo.Bounds = cefBounds;



            windowInfo.SetAsPopup(_owner.HostWindowHandle, $"{Messages.Browser_Loading} - {_owner.Title}");

            client = new PopupBrowserClient(_owner);
        }


        return e.Handled;
    }
}

internal class PopupBrowserClient : CefClient
{
    class PopupBrowserLifeSpanHandler : CefLifeSpanHandler
    {
        const int ICO_SMALL = 0;
        const int ICO_BIG = 1;


        private Formium _formium;

        public PopupBrowserLifeSpanHandler(Formium formium)
        {
            _formium = formium;
        }

        protected override void OnAfterCreated(CefBrowser browser)
        {
            var hostHandle = browser.GetHost().GetWindowHandle();

            var hIcon = (_formium.Icon ?? Properties.Resources.DefaultIcon).Handle;

            SendMessage(hostHandle, (uint)WindowMessage.WM_SETICON, (IntPtr)ICO_SMALL, hIcon);
            SendMessage(hostHandle, (uint)WindowMessage.WM_SETICON, (IntPtr)ICO_BIG, hIcon);

            base.OnAfterCreated(browser);
        }
    }

    class PopupBrowserDisplayHandler : CefDisplayHandler
    {
        private Formium _formium;

        public PopupBrowserDisplayHandler(Formium formium)
        {
            _formium = formium;
        }

        protected override void OnTitleChange(CefBrowser browser, string title)
        {
            var hWindow = browser.GetHost().GetWindowHandle();

            var caption = $"{title} - {_formium.GetWindowTitle()}";

            var hText = Marshal.StringToCoTaskMemAuto(caption);

            SendMessage(hWindow, (uint)WindowMessage.WM_SETTEXT, IntPtr.Zero, hText);

            Marshal.FreeCoTaskMem(hText);
        }
    }

    class PopupBrowserDownloadHandler : CefDownloadHandler
    {
        private Formium _formium;

        public PopupBrowserDownloadHandler(Formium formium)
        {
            _formium = formium;
        }

        protected override void OnBeforeDownload(CefBrowser browser, CefDownloadItem downloadItem, string suggestedName, CefBeforeDownloadCallback callback)
        {
            callback.Continue(suggestedName, true);
        }
    }

    private readonly Formium _formium;
    private readonly CefLifeSpanHandler _lifeSpanHandler;
    private readonly CefDisplayHandler _displayHandler;
    private readonly CefDownloadHandler _downloadHandler;


    public PopupBrowserClient(Formium formium)
    {
        _formium = formium;
        _lifeSpanHandler = new PopupBrowserLifeSpanHandler(formium);
        _displayHandler = new PopupBrowserDisplayHandler(formium);
        _downloadHandler = new PopupBrowserDownloadHandler(formium);
    }

    protected override CefLifeSpanHandler GetLifeSpanHandler()
    {
        return _lifeSpanHandler;
    }

    protected override CefDisplayHandler GetDisplayHandler()
    {
        return _displayHandler;
    }

    protected override CefDownloadHandler GetDownloadHandler()
    {
        return _downloadHandler;
    }
}

public sealed class FormiumCloseEventArgs : EventArgs
{
    public bool Canceled { get; set; } = false;

}

public sealed class BeforePopupEventArgs : EventArgs
{
    internal BeforePopupEventArgs(
        CefFrame frame,
        string targetUrl,
        string targetFrameName,
        bool userGesture,
        CefPopupFeatures popupFeatures,
        CefWindowInfo windowInfo,
        CefClient client,
        CefBrowserSettings settings,
        bool noJavascriptAccess)
    {
        Frame = frame;
        TargetUrl = targetUrl;
        TargetFrameName = targetFrameName;
        UserGesture = userGesture;
        PopupFeatures = popupFeatures;
        WindowInfo = windowInfo;
        Client = client;
        Settings = settings;
        NoJavascriptAccess = noJavascriptAccess;
    }

    public CefFrame Frame { get; }
    public string TargetUrl { get; }
    public string TargetFrameName { get; }
    public bool UserGesture { get; }
    public CefPopupFeatures PopupFeatures { get; }
    public CefWindowInfo WindowInfo { get; }
    public CefClient Client { get; set; }
    public CefBrowserSettings Settings { get; }
    public bool NoJavascriptAccess { get; set; }
    public bool Handled { get; set; } = false;

}
