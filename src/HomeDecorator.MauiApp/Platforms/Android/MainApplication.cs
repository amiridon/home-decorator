using Android.App;
using Android.Runtime;
using Microsoft.Maui.Hosting;

namespace HomeDecorator.MauiApp;

[Application]
public class MainApplication : Microsoft.Maui.MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
