using UnityEngine;
using System.Collections;
using AwesomiumMono;

public class Example_CustomHTML : WebTexture
{

    new void Start()
    {
        base.initialURL = "about:blank";
        base.Start();

        CreateJSObject("App");

        SetJSObjectProperty("App", "unityVersion", new JSValue(Application.unityVersion));
        SetJSObjectProperty("App", "unityPlatform", new JSValue(Application.platform.ToString()));
        SetJSObjectProperty("App", "insideEditor", new JSValue(Application.isEditor));

        BindJSObjectCallback("App", "quit", OnQuitPressed);

        LoadHTML(@"
		<html>
		<body style='font-family: Helvetica, Arial, sans-serif;'>
		<h1>Awesomium Rocks!</h1>
		<p>This web-page was loaded with HTML directly from within Unity3D.</p>
		
		<h2>Environment Info</h2>
		<dl>
		<dt>Unity Version</dt><dd id='stat1'></dd>
		<dt>Unity Platform</dt><dd id='stat2'></dd>
		<dt>Inside Editor?</dt><dd id='stat3'></dd>
		</dl>
		
		<script type='text/javascript'>
		document.getElementById('stat1').innerText = App.unityVersion;
		document.getElementById('stat2').innerText = App.unityPlatform;
		document.getElementById('stat3').innerText = App.insideEditor;
		</script>

		<p>The above info was passed to the web-page via a shared JS object.</p>

		<h2>Callback Example</h2>

		<input type='button' value='Quit App' onclick='App.quit()' />

		<p>The above button is hooked up to a Unity script via a shared JS callback.</p>
		</body>
		</html>
		");
    }

    public void OnQuitPressed(object sender, JSCallbackEventArgs e)
    {
#if UNITY_EDITOR
			UnityEditor.EditorUtility.DisplayDialog("Quit Requested", 
			                           "If you ran this in an actual player, it would quit. :-)", "Ok");
#else
        Application.Quit();
#endif
    }

}
