# Web-Browsing in Unity3D

UnityAwe is a Unity3D wrapper for [Awesomium](http://awesomium.com). It allows you to render and interact with web-pages within any Unity3D scene.

## Dependencies

This version depends on Awesomium 1.6.2 and Unity 3.4.

## How Do I Use This?

1. Copy the "release" files from the Awesomium SDK to your Unity Editor's path. (on Windows: Awesomium.dll, AwesomiumProcess.exe, icudt42.dll, locales folder -- on Mac OSX: Awesomium.framework)

2. Add `WebTexture.cs`, `WebCoreHelper.cs`, and `AwesomiumMono.dll` to your assets folder.

3. Drag the "WebTexture" script onto any game object or GUI texture to make it display a web-page.

We've also provided an example script (Example_CustomHTML.cs) that demonstrates how to load custom HTML, bind properties/callbacks to Javascript, and more. Just add it to your asset folder and drag it to a GameObject or GUITexture to make it display.

### Common Unity3D Editor folder paths:

Windows = `C:\Program Files(x86)\Unity\Editor`

Mac = `/Applications/Unity/Unity.app/Contents/Frameworks`

When copying to the Mac application, you may need to right-click the Unity app, and select `Show Package Contents`.

### Displaying with GUITexture

Make sure your GUITexture pixel inset size matches the size specified in the WebTexture script, otherwise mouse input won't work correctly.

### Deploying Your Application

Make sure to copy over the Awesomium SDK files to your respective application.

On Mac, you will need to copy `Awesomium.framework` to your application bundle's `Frameworks` folder (right-click, show package contents).

## Known Issues

Awesomium doesn't like to be initialized more than once per-process, so some web-pages will not display twice in a row inside the Editor (eg, pushing play, stop, play, stop).

## AwesomiumMono

We've provided a copy of AwesomiumMono.dll for Windows and Mac OSX inside the `bin` folder.

To build AwesomiumMono, please check out khrona/AwesomiumSharp on GitHub.

## Licensing

Awesomium is free for evaluation, non-commercial use, and independent use (by companies who made less than $100K in revenue last year).

If you're a larger company and would like to use Awesomium for commercial use, you can purchase a Pro License at http://awesomium.com/buy/