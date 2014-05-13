using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using AwesomiumMono;

public class WebTexture : MonoBehaviour
{
    // Public Variables
    public int width = 512;
    public int height = 512;
    public string initialURL = "http://google.com";

    // Internal Variables
    private static GameObject webCoreHelper;
    private bool isFocused = false;
    private bool isScrollable = false;
    private static List<WebView> webViewList = new List<WebView>();
    private WebView webView;
    private Texture2D texture;
    private Color32[] Pixels;
    private GCHandle PixelsHandle;

    public void Start()
    {
        // Initialize the WebCore if it's not active
        if (!WebCore.IsRunning)
        {
            WebCoreConfig conf = new WebCoreConfig();
            conf.EnablePlugins = true;
            WebCore.Initialize(conf);

            webCoreHelper = new GameObject("WebCoreHelperInstance");
            webCoreHelper.AddComponent<WebCoreHelper>();
        }

        webView = WebCore.CreateWebView(width, height);

        webViewList.Add(webView);
        LoadURL(initialURL);
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Pixels = texture.GetPixels32(0);
        PixelsHandle = GCHandle.Alloc(Pixels, GCHandleType.Pinned);

        if (renderer)
        {
            renderer.material.mainTexture = texture;
        }
        else if (GetComponent(typeof(GUITexture)))
        {
            GUITexture gui = GetComponent(typeof(GUITexture)) as GUITexture;
            gui.texture = texture;
        }
        else
        {
            Debug.Log("Game Object has no Material or GUI Texture, we cannot render a web-page to this object!");
        }
    }

    // Start loading a certain URL
    public void LoadURL(string url)
    {
        if (WebCore.IsRunning)
            webView.LoadURL(url);
    }

    // Start loading a string of HTML
    public void LoadHTML(string html)
    {
        if (WebCore.IsRunning)
            webView.LoadHTML(html);
    }

    public void GoBack()
    {
        if (WebCore.IsRunning)
            webView.GoToHistoryOffset(-1);
    }

    public void GoForward()
    {
        if (WebCore.IsRunning)
            webView.GoToHistoryOffset(1);
    }

    public void Reload()
    {
        if (WebCore.IsRunning)
            webView.Reload();
    }

    // Create a global, persistent named JS Object accessible from any web-page
    public void CreateJSObject(string objectName)
    {
        if (WebCore.IsRunning)
            webView.CreateObject(objectName);
    }

    // Set a property of a JS Object, accessible from any web-page
    public void SetJSObjectProperty(string objectName, string propertyName, JSValue val)
    {
        if (WebCore.IsRunning)
            webView.SetObjectProperty(objectName, propertyName, val);
    }

    // Set a callback of a JS Object, accessible from any web-page
    public void BindJSObjectCallback(string objectName, string callbackName, JSCallback callback)
    {
        if (WebCore.IsRunning)
            webView.SetObjectCallback(objectName, callbackName, callback);
    }

    public void PauseRendering()
    {
        if (WebCore.IsRunning)
            webView.PauseRendering();
    }

    public void ResumeRendering()
    {
        if (WebCore.IsRunning)
            webView.ResumeRendering();
    }

    private void OnDisable()
    {
        // Free the pinned array handle.
        PixelsHandle.Free();

        webViewList.Remove(webView);
        webView = null;

        if (WebCore.IsRunning)
            webView.Close();
    }

    public void Focus()
    {
        if (!WebCore.IsRunning)
            return;

        // Unfocus all open webViews, then focus the webView that was just clicked

        foreach (WebView view in webViewList)
            view.Unfocus();

        webView.Focus();
        isFocused = true;
    }

    public void Unfocus()
    {
        if (!WebCore.IsRunning)
            return;

        // Unfocus all open webViews

        foreach (WebView view in webViewList)
            view.Unfocus();

        isFocused = false;
    }

    private void OnGUI()
    {
        if (!WebCore.IsRunning)
            return;

        Event e = Event.current;

        // We only inject keyboard input when the GameObject has focus
        if (e.isKey == true && isFocused == true)
        {
            if (e.type == EventType.KeyDown)
            {
                if (e.character == 0)
                {
                    WebKeyboardEvent keyEvent = new WebKeyboardEvent();
                    keyEvent.Type = WebKeyType.KeyDown;
                    keyEvent.VirtualKeyCode = MapKeys(e);
                    keyEvent.Modifiers = MapModifiers(e);
                    webView.InjectKeyboardEvent(keyEvent);
                }
                else
                {
                    WebKeyboardEvent keyEvent = new WebKeyboardEvent();
                    keyEvent.Type = WebKeyType.Char;
                    keyEvent.Text = new ushort[] { e.character, 0, 0, 0 };
                    keyEvent.Modifiers = MapModifiers(e);
                    webView.InjectKeyboardEvent(keyEvent);
                }
            }

            if (e.type == EventType.KeyUp)
            {
                WebKeyboardEvent keyEvent = new WebKeyboardEvent();
                keyEvent.Type = WebKeyType.KeyUp;
                keyEvent.VirtualKeyCode = MapKeys(e);
                keyEvent.Modifiers = MapModifiers(e);
                webView.InjectKeyboardEvent(keyEvent);
            }
        }

        // We unfocus each WebView whenever a MouseDown event is encountered in OnGUI.
        // The actual focusing of a specific element occurs in OnMouseDown
        if (e.type == EventType.MouseDown)
        {
            Unfocus();
        }

        if (e.type == EventType.ScrollWheel && isScrollable == true)
        {
            webView.InjectMouseWheel((int)e.delta.y * -10);
        }
    }

    private void Update()
    {
        if (!WebCore.IsRunning)
            return;

        if (webView.IsDirty)
        {
            AwesomiumMono.RenderBuffer rBuffer = webView.Render();

            if (rBuffer != null)
            {
                rBuffer.CopyTo(PixelsHandle.AddrOfPinnedObject(), rBuffer.Rowspan, 4, true, true);
                texture.SetPixels32(Pixels, 0);
                texture.Apply(false, false);
            }
        }

    }

    private void OnApplicationQuit()
    {
        // We shutdown the WebCore only once
        if (WebCore.IsRunning)
        {
            Destroy(GameObject.Find("WebCoreHelperInstance"));

            WebCore.Shutdown();
        }
    }

    private void OnMouseOver()
    {
        if (!WebCore.IsRunning)
            return;

        RaycastHit hit;

        // Used for injecting a MouseMove event on a game object
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
        {
            int x = (int)(hit.textureCoord.x * width);
            int y = (int)(hit.textureCoord.y * height);
            webView.InjectMouseMove(x, height - y);
        }
        else // Used for injecting a MouseMove event on a GUITexture
        {
            GUITexture gui = GetComponent(typeof(GUITexture)) as GUITexture;
            if (gui != null)
            {
                int x = (int)((Input.mousePosition.x) - (gui.pixelInset.x + Screen.width * transform.position.x));
                int y = (int)((Input.mousePosition.y) - (gui.pixelInset.y + Screen.height * transform.position.y));
                webView.InjectMouseMove(x, height - y);
            }
        }
    }

    private void OnMouseDown()
    {
        if (!WebCore.IsRunning)
            return;

        Focus();

        RaycastHit hit;

        // Used for injecting a MouseDown event on a game object
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
        {
            webView.Focus();

            int x = (int)(hit.textureCoord.x * width);
            int y = (int)(hit.textureCoord.y * height);
            webView.InjectMouseMove(x, height - y);
            webView.InjectMouseDown(MouseButton.Left);
        }
        else  // Used for injecting a MouseDown event on a GUITexture
        {
            GUITexture gui = GetComponent(typeof(GUITexture)) as GUITexture;
            if (gui != null)
            {
                int x = (int)((Input.mousePosition.x) - (gui.pixelInset.x + Screen.width * transform.position.x));
                int y = (int)((Input.mousePosition.y) - (gui.pixelInset.y + Screen.height * transform.position.y));
                webView.InjectMouseMove(x, height - y);
                webView.InjectMouseDown(MouseButton.Left);
            }
        }
    }

    private void OnMouseUp()
    {
        if (!WebCore.IsRunning)
            return;

        RaycastHit hit;

        // Used for injecting a MouseUp event on a game object
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
        {
            int x = (int)(hit.textureCoord.x * width);
            int y = (int)(hit.textureCoord.y * height);
            webView.InjectMouseMove(x, height - y);
            webView.InjectMouseUp(MouseButton.Left);
        }
        else // Used for injecting a MouseUp event on a GUITexture
        {
            GUITexture gui = GetComponent(typeof(GUITexture)) as GUITexture;
            if (gui != null)
            {
                int x = (int)((Input.mousePosition.x) - (gui.pixelInset.x + Screen.width * transform.position.x));
                int y = (int)((Input.mousePosition.y) - (gui.pixelInset.y + Screen.height * transform.position.y));
                webView.InjectMouseMove(x, height - y);
                webView.InjectMouseUp(MouseButton.Left);
            }
        }
    }

    private void OnMouseEnter()
    {
        isScrollable = true;
    }

    private void OnMouseExit()
    {
        isScrollable = false;
    }

    private WebKeyModifiers MapModifiers(Event e)
    {
        int modifiers = 0;

        if (e.control)
            modifiers |= (int)WebKeyModifiers.ControlKey;

        if (e.shift)
            modifiers |= (int)WebKeyModifiers.ShiftKey;

        if (e.alt)
            modifiers |= (int)WebKeyModifiers.AltKey;

        return (WebKeyModifiers)modifiers;
    }

    private VirtualKey MapKeys(Event e)
    {
        switch (e.keyCode)
        {
            case KeyCode.Backspace: return VirtualKey.BACK;
            case KeyCode.Delete: return VirtualKey.DELETE;
            case KeyCode.Tab: return VirtualKey.TAB;
            case KeyCode.Clear: return VirtualKey.CLEAR;
            case KeyCode.Return: return VirtualKey.RETURN;
            case KeyCode.Pause: return VirtualKey.PAUSE;
            case KeyCode.Escape: return VirtualKey.ESCAPE;
            case KeyCode.Space: return VirtualKey.SPACE;
            case KeyCode.Keypad0: return VirtualKey.NUMPAD0;
            case KeyCode.Keypad1: return VirtualKey.NUMPAD1;
            case KeyCode.Keypad2: return VirtualKey.NUMPAD2;
            case KeyCode.Keypad3: return VirtualKey.NUMPAD3;
            case KeyCode.Keypad4: return VirtualKey.NUMPAD4;
            case KeyCode.Keypad5: return VirtualKey.NUMPAD5;
            case KeyCode.Keypad6: return VirtualKey.NUMPAD6;
            case KeyCode.Keypad7: return VirtualKey.NUMPAD7;
            case KeyCode.Keypad8: return VirtualKey.NUMPAD8;
            case KeyCode.Keypad9: return VirtualKey.NUMPAD9;
            case KeyCode.KeypadPeriod: return VirtualKey.DECIMAL;
            case KeyCode.KeypadDivide: return VirtualKey.DIVIDE;
            case KeyCode.KeypadMultiply: return VirtualKey.MULTIPLY;
            case KeyCode.KeypadMinus: return VirtualKey.SUBTRACT;
            case KeyCode.KeypadPlus: return VirtualKey.ADD;
            case KeyCode.KeypadEnter: return VirtualKey.SEPARATOR;
            case KeyCode.KeypadEquals: return VirtualKey.UNKNOWN;
            case KeyCode.UpArrow: return VirtualKey.UP;
            case KeyCode.DownArrow: return VirtualKey.DOWN;
            case KeyCode.RightArrow: return VirtualKey.RIGHT;
            case KeyCode.LeftArrow: return VirtualKey.LEFT;
            case KeyCode.Insert: return VirtualKey.INSERT;
            case KeyCode.Home: return VirtualKey.HOME;
            case KeyCode.End: return VirtualKey.END;
            case KeyCode.PageUp: return VirtualKey.PRIOR;
            case KeyCode.PageDown: return VirtualKey.NEXT;
            case KeyCode.F1: return VirtualKey.F1;
            case KeyCode.F2: return VirtualKey.F2;
            case KeyCode.F3: return VirtualKey.F3;
            case KeyCode.F4: return VirtualKey.F4;
            case KeyCode.F5: return VirtualKey.F5;
            case KeyCode.F6: return VirtualKey.F6;
            case KeyCode.F7: return VirtualKey.F7;
            case KeyCode.F8: return VirtualKey.F8;
            case KeyCode.F9: return VirtualKey.F9;
            case KeyCode.F10: return VirtualKey.F10;
            case KeyCode.F11: return VirtualKey.F11;
            case KeyCode.F12: return VirtualKey.F12;
            case KeyCode.F13: return VirtualKey.F13;
            case KeyCode.F14: return VirtualKey.F14;
            case KeyCode.F15: return VirtualKey.F15;
            case KeyCode.Alpha0: return VirtualKey.NUM_0;
            case KeyCode.Alpha1: return VirtualKey.NUM_1;
            case KeyCode.Alpha2: return VirtualKey.NUM_2;
            case KeyCode.Alpha3: return VirtualKey.NUM_3;
            case KeyCode.Alpha4: return VirtualKey.NUM_4;
            case KeyCode.Alpha5: return VirtualKey.NUM_5;
            case KeyCode.Alpha6: return VirtualKey.NUM_6;
            case KeyCode.Alpha7: return VirtualKey.NUM_7;
            case KeyCode.Alpha8: return VirtualKey.NUM_8;
            case KeyCode.Alpha9: return VirtualKey.NUM_9;
            case KeyCode.Exclaim: return VirtualKey.NUM_1;
            case KeyCode.DoubleQuote: return VirtualKey.OEM_7;
            case KeyCode.Hash: return VirtualKey.NUM_3;
            case KeyCode.Dollar: return VirtualKey.NUM_4;
            case KeyCode.Ampersand: return VirtualKey.NUM_7;
            case KeyCode.Quote: return VirtualKey.OEM_7;
            case KeyCode.LeftParen: return VirtualKey.NUM_9;
            case KeyCode.RightParen: return VirtualKey.NUM_0;
            case KeyCode.Asterisk: return VirtualKey.NUM_8;
            case KeyCode.Plus: return VirtualKey.OEM_PLUS;
            case KeyCode.Comma: return VirtualKey.OEM_COMMA;
            case KeyCode.Minus: return VirtualKey.OEM_MINUS;
            case KeyCode.Period: return VirtualKey.OEM_PERIOD;
            case KeyCode.Slash: return VirtualKey.OEM_2;
            case KeyCode.Colon: return VirtualKey.OEM_1;
            case KeyCode.Semicolon: return VirtualKey.OEM_1;
            case KeyCode.Less: return VirtualKey.OEM_COMMA;
            case KeyCode.Equals: return VirtualKey.OEM_PLUS;
            case KeyCode.Greater: return VirtualKey.OEM_PERIOD;
            case KeyCode.Question: return VirtualKey.OEM_2;
            case KeyCode.At: return VirtualKey.NUM_2;
            case KeyCode.LeftBracket: return VirtualKey.OEM_4;
            case KeyCode.Backslash: return VirtualKey.OEM_102;
            case KeyCode.RightBracket: return VirtualKey.OEM_6;
            case KeyCode.Caret: return VirtualKey.NUM_6;
            case KeyCode.Underscore: return VirtualKey.OEM_MINUS;
            case KeyCode.BackQuote: return VirtualKey.OEM_3;
            case KeyCode.A: return VirtualKey.A;
            case KeyCode.B: return VirtualKey.B;
            case KeyCode.C: return VirtualKey.C;
            case KeyCode.D: return VirtualKey.D;
            case KeyCode.E: return VirtualKey.E;
            case KeyCode.F: return VirtualKey.F;
            case KeyCode.G: return VirtualKey.G;
            case KeyCode.H: return VirtualKey.H;
            case KeyCode.I: return VirtualKey.I;
            case KeyCode.J: return VirtualKey.J;
            case KeyCode.K: return VirtualKey.K;
            case KeyCode.L: return VirtualKey.L;
            case KeyCode.M: return VirtualKey.M;
            case KeyCode.N: return VirtualKey.N;
            case KeyCode.O: return VirtualKey.O;
            case KeyCode.P: return VirtualKey.P;
            case KeyCode.Q: return VirtualKey.Q;
            case KeyCode.R: return VirtualKey.R;
            case KeyCode.S: return VirtualKey.S;
            case KeyCode.T: return VirtualKey.T;
            case KeyCode.U: return VirtualKey.U;
            case KeyCode.V: return VirtualKey.V;
            case KeyCode.W: return VirtualKey.W;
            case KeyCode.X: return VirtualKey.X;
            case KeyCode.Y: return VirtualKey.Y;
            case KeyCode.Z: return VirtualKey.Z;
            case KeyCode.Numlock: return VirtualKey.NUMLOCK;
            case KeyCode.CapsLock: return VirtualKey.CAPITAL;
            case KeyCode.ScrollLock: return VirtualKey.SCROLL;
            case KeyCode.RightShift: return VirtualKey.RSHIFT;
            case KeyCode.LeftShift: return VirtualKey.LSHIFT;
            case KeyCode.RightControl: return VirtualKey.RCONTROL;
            case KeyCode.LeftControl: return VirtualKey.LCONTROL;
            case KeyCode.RightAlt: return VirtualKey.RMENU;
            case KeyCode.LeftAlt: return VirtualKey.LMENU;
            case KeyCode.LeftApple: return VirtualKey.LWIN;
            case KeyCode.LeftWindows: return VirtualKey.LWIN;
            case KeyCode.RightApple: return VirtualKey.RWIN;
            case KeyCode.RightWindows: return VirtualKey.RWIN;
            case KeyCode.AltGr: return VirtualKey.UNKNOWN;
            case KeyCode.Help: return VirtualKey.HELP;
            case KeyCode.Print: return VirtualKey.PRINT;
            case KeyCode.SysReq: return VirtualKey.UNKNOWN;
            case KeyCode.Break: return VirtualKey.PAUSE;
            case KeyCode.Menu: return VirtualKey.MENU;
            default: return 0;
        }

    }

}