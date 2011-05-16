using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using AwesomiumMono;

public class WebTexture : MonoBehaviour {

        static bool WebCoreHasStarted = false;
        static bool WebCoreHasShutdown = false;
        static GameObject webCoreHelper;
        bool isFocused = false;
        bool isScrollable = false;
        static List<WebView> allWebViews = new List<WebView>();
        WebView webView;
        Texture2D texture;
        Color[] Pixels;
        GCHandle PixelsHandle;
        public int width = 512;
        public int height = 512;
        public string initialURL = "http://google.com";
    
    void Start () {
        
        // Initialize webCore and timer if they haven't been already
        if(WebCoreHasStarted == false){
            
            WebCore.Config conf = new WebCore.Config();
            conf.enablePlugins = true;
            WebCore.Initialize(conf);
            
            webCoreHelper = new GameObject("WebCoreHelperInstance");
            webCoreHelper.AddComponent<WebCoreHelper>();
            
            WebCoreHasStarted = true;
            Debug.Log("Started WebCore!");
        }
        
        Debug.Log("Starting new WebTexture");
        // Create a new webView
        webView = WebCore.CreateWebview(width, height);
        
        // Add webView to list of all open webviews
        allWebViews.Add(webView);
        
        // Load webpage
        webView.LoadURL(initialURL);
        
        // Create texture that will be updated 
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        
        // Create the pixel array for the plugin to write into at startup    
        Pixels = texture.GetPixels (0); 
        
        // "pin" the array in memory, so we can pass direct pointer to it's data to the plugin,
        // without costly marshaling of array of structures.
        PixelsHandle = GCHandle.Alloc(Pixels, GCHandleType.Pinned);
             
        // Assign texture to the renderer
        if (renderer)
            renderer.material.mainTexture = texture;
        // or gui texture
        else if (GetComponent(typeof(GUITexture)))
        {
            GUITexture gui = GetComponent(typeof(GUITexture)) as GUITexture;
            gui.texture = texture;
        }
        else
        {
            Debug.Log("Game object has no renderer or gui texture to assign the generated texture to!");
        }
    }
    
    void OnDisable() {
        // Free the pinned array handle.
        PixelsHandle.Free();
        
        allWebViews.Remove(webView);
        webView.Dispose();
        webView = null;
    }
    
    void Focus(){
        // Unfocus all open webViews, then focus the webView that was just clicked
        
        foreach(WebView view in allWebViews){
            view.Unfocus();
        }
        webView.Focus();
        isFocused = true;
    }
    
    void Unfocus(){
        // Unfocus all open webViews
        
        foreach(WebView view in allWebViews){
            view.Unfocus();
        }
        isFocused = false;
    }
    
    void OnGUI() {
      
        Event e = Event.current;
        
        //We only inject input when the GameObject has focus
        if (e.isKey == true && isFocused == true){

            if(e.type == EventType.KeyDown){
    
                if(e.character == 0)
                {
                    WebKeyboardEvent keyEvent = new WebKeyboardEvent();
                    keyEvent.type = WebKeyType.KeyDown;
                    keyEvent.virtualKeyCode = MapKeys(e); 
                    webView.InjectKeyboardEvent(keyEvent);
                }
                else
                {
                    WebKeyboardEvent keyEvent = new WebKeyboardEvent();
                    keyEvent.type = WebKeyType.Char;
                    keyEvent.text =  new ushort[] { e.character, 0, 0, 0 };
                    webView.InjectKeyboardEvent(keyEvent);
                }
            }
    
            if(e.type == EventType.KeyUp){
                WebKeyboardEvent keyEvent = new WebKeyboardEvent();
                keyEvent.type = WebKeyType.KeyUp;
                keyEvent.virtualKeyCode = MapKeys(e); 
                webView.InjectKeyboardEvent(keyEvent);
            }
        }
        
        // We unfocus each WebView whenever a MouseDown event is encountered in OnGUI
        // the actual focusing of a specific element occurs in OnMouseDown
        if(e.type == EventType.MouseDown)
        {
            Unfocus();
        }
        
        if(e.type == EventType.ScrollWheel && isScrollable == true)
        {
            webView.InjectMouseWheel((int)e.delta.y * -10);
        }
    }
    
    void Update () {

            if(webView.IsDirty())
            {
                RenderBuffer rBuffer = webView.Render();
                rBuffer.CopyToFloat(PixelsHandle.AddrOfPinnedObject());
                texture.SetPixels (Pixels, 0);
                texture.Apply ();
            }

    }
    
    void OnApplicationQuit(){
        // When the application quits, shut down the webCore only once
        
        if(WebCoreHasShutdown == false){
            Destroy(GameObject.Find("WebCoreHelperInstance"));
            WebCore.Shutdown();
            WebCoreHasShutdown = true;
        }
    }
    
    void OnMouseOver()
    {
        RaycastHit hit;
            
            // Used for injecting a MouseMove event on a game object
            if (Physics.Raycast (Camera.main.ScreenPointToRay(Input.mousePosition), out hit)){
                int x =  (int) (hit.textureCoord.x * width);
                int y =  (int) (hit.textureCoord.y * height);
                webView.InjectMouseMove(x,height-y);
            }
            // Used for injecting a MouseMove event on a GUITexture
            else{
                GUITexture gui = GetComponent(typeof(GUITexture)) as GUITexture;
                int x =  (int)((Input.mousePosition.x)-(gui.pixelInset.x+Screen.width*transform.position.x));
                int y =  (int)((Input.mousePosition.y)-(gui.pixelInset.y+Screen.height*transform.position.y));
                webView.InjectMouseMove(x,height-y);
            }
    }
    
    void OnMouseDown()
    {
        
        Focus();
        
        RaycastHit hit;
        
            // Used for injecting a MouseDown event on a game object
            if (Physics.Raycast (Camera.main.ScreenPointToRay(Input.mousePosition), out hit)){
                webView.Focus();
                
                int x =  (int)(hit.textureCoord.x * width);
                int y =  (int)(hit.textureCoord.y * height);
                webView.InjectMouseMove(x,height-y);
                webView.InjectMouseDown(MouseButton.Left);
            }
            // Used for injecting a MouseDown event on a GUITexture
            else{
                GUITexture gui = GetComponent(typeof(GUITexture)) as GUITexture;
                int x =  (int)((Input.mousePosition.x)-(gui.pixelInset.x+Screen.width*transform.position.x));
                int y =  (int)((Input.mousePosition.y)-(gui.pixelInset.y+Screen.height*transform.position.y));
                webView.InjectMouseMove(x,height-y);
                webView.InjectMouseDown(MouseButton.Left);
            }
    }
    
    void OnMouseUp()
    {
        RaycastHit hit;
        
            // Used for injecting a MouseUp event on a game object
            if (Physics.Raycast (Camera.main.ScreenPointToRay(Input.mousePosition), out hit)){
                int x =  (int) (hit.textureCoord.x * width);
                int y =  (int) (hit.textureCoord.y * height);
                webView.InjectMouseMove(x,height-y);
                webView.InjectMouseUp(MouseButton.Left);
            }
            // Used for injecting a MouseUp event on a GUITexture
            else{
                GUITexture gui = GetComponent(typeof(GUITexture)) as GUITexture;
                int x =  (int)((Input.mousePosition.x)-(gui.pixelInset.x+Screen.width*transform.position.x));
                int y =  (int)((Input.mousePosition.y)-(gui.pixelInset.y+Screen.height*transform.position.y));
                webView.InjectMouseMove(x,height-y);
                webView.InjectMouseUp(MouseButton.Left);
            }
    }
    
    void OnMouseEnter()
    {
        isScrollable = true;
    }
    
    void OnMouseExit()
    {
        isScrollable = false;
    }
    
    int MapKeys(Event e){
        switch(e.keyCode)
        {
            case KeyCode.Backspace:
                return (int)VirtualKey.BACK;
            
            case KeyCode.Delete:
                return (int)VirtualKey.DELETE;
            
            case KeyCode.Tab:
                return (int)VirtualKey.TAB;
            
            case KeyCode.Clear:
                return (int)VirtualKey.CLEAR;
            
            case KeyCode.Return:
                return (int)VirtualKey.RETURN;
            
            case KeyCode.Pause:
                return (int)VirtualKey.PAUSE;
            
            case KeyCode.Escape:
                return (int)VirtualKey.ESCAPE;
            
            case KeyCode.Space:
                return (int)VirtualKey.SPACE;
            
            case KeyCode.Keypad0:
                return (int)VirtualKey.NUMPAD0;
            
            case KeyCode.Keypad1:
                return (int)VirtualKey.NUMPAD1;
            
            case KeyCode.Keypad2:
                return (int)VirtualKey.NUMPAD2;
            
            case KeyCode.Keypad3:
                return (int)VirtualKey.NUMPAD3;
            
            case KeyCode.Keypad4:
                return (int)VirtualKey.NUMPAD4;
                
            case KeyCode.Keypad5:
                return (int)VirtualKey.NUMPAD5;
            
            case KeyCode.Keypad6:
                return (int)VirtualKey.NUMPAD6;
                
            case KeyCode.Keypad7:
                return (int)VirtualKey.NUMPAD7;
                    
            case KeyCode.Keypad8:
                return (int)VirtualKey.NUMPAD8;
                
            case KeyCode.Keypad9:
                return (int)VirtualKey.NUMPAD9;
            
            case KeyCode.KeypadPeriod:
                return (int)VirtualKey.DECIMAL;
            
            case KeyCode.KeypadDivide:
                return (int)VirtualKey.DIVIDE;
            
            case KeyCode.KeypadMultiply:
                return (int)VirtualKey.MULTIPLY;
            
            case KeyCode.KeypadMinus:
                return (int)VirtualKey.SUBTRACT;
            
            case KeyCode.KeypadPlus:
                return (int)VirtualKey.ADD;
            
            case KeyCode.KeypadEnter:
                return (int)VirtualKey.SEPARATOR;
            
            case KeyCode.KeypadEquals:
                return (int)VirtualKey.UNKNOWN;
            
            case KeyCode.UpArrow:
                return (int)VirtualKey.UP;
            
            case KeyCode.DownArrow:
                return (int)VirtualKey.DOWN;
            
            case KeyCode.RightArrow:
                return (int)VirtualKey.RIGHT;
            
            case KeyCode.LeftArrow:
                return (int)VirtualKey.LEFT;
            
            case KeyCode.Insert:
                return (int)VirtualKey.INSERT;
            
            case KeyCode.Home:
                return (int)VirtualKey.HOME;
            
            case KeyCode.End:
                return (int)VirtualKey.END;
            
            case KeyCode.PageUp:
                return (int)VirtualKey.PRIOR;
            
            case KeyCode.PageDown:
                return (int)VirtualKey.NEXT;
            
            case KeyCode.F1:
                return (int)VirtualKey.F1;
                        
            case KeyCode.F2:
                return (int)VirtualKey.F2;
                                    
            case KeyCode.F3:
                return (int)VirtualKey.F3;
            
            case KeyCode.F4:
                return (int)VirtualKey.F4;
                
            case KeyCode.F5:
                return (int)VirtualKey.F5;
                    
            case KeyCode.F6:
                return (int)VirtualKey.F6;
            
            case KeyCode.F7:
                return (int)VirtualKey.F7;
            
            case KeyCode.F8:
                return (int)VirtualKey.F8;
            
            case KeyCode.F9:
                return (int)VirtualKey.F9;
            
            case KeyCode.F10:
                return (int)VirtualKey.F10;
                        
            case KeyCode.F11:
                return (int)VirtualKey.F11;
                                    
            case KeyCode.F12:
                return (int)VirtualKey.F12;
                                                
            case KeyCode.F13:
                return (int)VirtualKey.F13;
                                                            
            case KeyCode.F14:
                return (int)VirtualKey.F14;
                                                                        
            case KeyCode.F15:
                return (int)VirtualKey.F15;
            
            case KeyCode.Alpha0:
                return (int)VirtualKey.NUM_0;
                        
            case KeyCode.Alpha1:
                return (int)VirtualKey.NUM_1;
                        
            case KeyCode.Alpha2:
                return (int)VirtualKey.NUM_2;
                        
            case KeyCode.Alpha3:
                return (int)VirtualKey.NUM_3;
                        
            case KeyCode.Alpha4:
                return (int)VirtualKey.NUM_4;
                        
            case KeyCode.Alpha5:
                return (int)VirtualKey.NUM_5;
                        
            case KeyCode.Alpha6:
                return (int)VirtualKey.NUM_6;
                        
            case KeyCode.Alpha7:
                return (int)VirtualKey.NUM_7;
                            
            case KeyCode.Alpha8:
                return (int)VirtualKey.NUM_8;
                
            case KeyCode.Alpha9:
                return (int)VirtualKey.NUM_9;
                            
            case KeyCode.Exclaim:
                return (int)VirtualKey.NUM_1;
                            
            case KeyCode.DoubleQuote:
                return (int)VirtualKey.OEM_7;
                            
            case KeyCode.Hash:
                return (int)VirtualKey.NUM_3;
            
            case KeyCode.Dollar:
                return (int)VirtualKey.NUM_4;
                            
            case KeyCode.Ampersand:
                return (int)VirtualKey.NUM_7;
                            
            case KeyCode.Quote:
                return (int)VirtualKey.OEM_7;
            
            case KeyCode.LeftParen:
                return (int)VirtualKey.NUM_9;
                            
            case KeyCode.RightParen:
                return (int)VirtualKey.NUM_0;
                            
            case KeyCode.Asterisk:
                return (int)VirtualKey.NUM_8;
            
            case KeyCode.Plus:
                return (int)VirtualKey.OEM_PLUS;
                            
            case KeyCode.Comma:
                return (int)VirtualKey.OEM_COMMA;
                            
            case KeyCode.Minus:
                return (int)VirtualKey.OEM_MINUS;
            
            case KeyCode.Period:
                return (int)VirtualKey.OEM_PERIOD;    
            
            case KeyCode.Slash:
                return (int)VirtualKey.OEM_2;
            
            case KeyCode.Colon:
                return (int)VirtualKey.OEM_1;
            
            case KeyCode.Semicolon:     
                return (int)VirtualKey.OEM_1;
            
            case KeyCode.Less:
                return (int)VirtualKey.OEM_COMMA;     
            
            case KeyCode.Equals:
                return (int)VirtualKey.OEM_PLUS;    
            
            case KeyCode.Greater:    
                return (int)VirtualKey.OEM_PERIOD; 
            
            case KeyCode.Question:
                return (int)VirtualKey.OEM_2;    
            
            case KeyCode.At:    
                return (int)VirtualKey.NUM_2; 
            
            case KeyCode.LeftBracket:
                return (int)VirtualKey.OEM_4;    
            
            case KeyCode.Backslash:    
                return (int)VirtualKey.OEM_102;
            
            case KeyCode.RightBracket:
                return (int)VirtualKey.OEM_6;
            
            case KeyCode.Caret: 
                return (int)VirtualKey.NUM_6;
            
            case KeyCode.Underscore:    
                return (int)VirtualKey.OEM_MINUS; 
            
            case KeyCode.BackQuote:     
                return (int)VirtualKey.OEM_3;
            
            case KeyCode.A:    
                return (int)VirtualKey.A; 
            
            case KeyCode.B:     
                return (int)VirtualKey.B;
            
            case KeyCode.C:    
                return (int)VirtualKey.C;
            
            case KeyCode.D:     
                return (int)VirtualKey.D;
            
            case KeyCode.E:    
                return (int)VirtualKey.E;
            
            case KeyCode.F:    
                return (int)VirtualKey.F;
            
            case KeyCode.G:    
                return (int)VirtualKey.G;
            
            case KeyCode.H:     
                return (int)VirtualKey.H;
            
            case KeyCode.I:     
                return (int)VirtualKey.I;
            
            case KeyCode.J:    
                return (int)VirtualKey.J;
            
            case KeyCode.K:    
                return (int)VirtualKey.K;
            
            case KeyCode.L:     
                return (int)VirtualKey.L;
            
            case KeyCode.M:    
                return (int)VirtualKey.M;
            
            case KeyCode.N:    
                return (int)VirtualKey.N;
            
            case KeyCode.O: 
                return (int)VirtualKey.O;
            
            case KeyCode.P:     
                return (int)VirtualKey.P;
            
            case KeyCode.Q:    
                return (int)VirtualKey.Q;
            
            case KeyCode.R:
                return (int)VirtualKey.R;
            
            case KeyCode.S:    
                return (int)VirtualKey.S;
            
            case KeyCode.T:    
                return (int)VirtualKey.T;
            
            case KeyCode.U:    
                return (int)VirtualKey.U;
            
            case KeyCode.V:     
                return (int)VirtualKey.V;
            
            case KeyCode.W:
                return (int)VirtualKey.W;
            
            case KeyCode.X:     
                return (int)VirtualKey.X;
            
            case KeyCode.Y:
                return (int)VirtualKey.Y;
            
            case KeyCode.Z:    
                return (int)VirtualKey.Z;
            
            case KeyCode.Numlock:
                return (int)VirtualKey.NUMLOCK;
            
            case KeyCode.CapsLock:
                return (int)VirtualKey.CAPITAL;
            
            case KeyCode.ScrollLock:
                return (int)VirtualKey.SCROLL;
            
            case KeyCode.RightShift:
                return (int)VirtualKey.RSHIFT;
            
            case KeyCode.LeftShift:    
                return (int)VirtualKey.LSHIFT;
            
            case KeyCode.RightControl:
                return (int)VirtualKey.RCONTROL;
            
            case KeyCode.LeftControl:
                return (int)VirtualKey.LCONTROL;
            
            case KeyCode.RightAlt:    
                return (int)VirtualKey.RMENU;
            
            case KeyCode.LeftAlt:    
                return (int)VirtualKey.LMENU;
            
            case KeyCode.LeftApple:    
                return (int)VirtualKey.LWIN;
            
            case KeyCode.LeftWindows:
                return (int)VirtualKey.LWIN;
            
            case KeyCode.RightApple:     
                return (int)VirtualKey.RWIN;
            
            case KeyCode.RightWindows:     
                return (int)VirtualKey.RWIN;
            
            case KeyCode.AltGr:    
                return (int)VirtualKey.UNKNOWN;
            
            case KeyCode.Help:
                return (int)VirtualKey.HELP;
            
            case KeyCode.Print:    
                return (int)VirtualKey.PRINT;
            
            case KeyCode.SysReq:
                return (int)VirtualKey.UNKNOWN;
            
            case KeyCode.Break:
                return (int)VirtualKey.PAUSE;
            
            case KeyCode.Menu:
                return (int)VirtualKey.MENU;
        
            default:
                return 0;
        }
    
    }

}