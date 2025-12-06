using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
namespace InventorySystem;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("MiSans-Medium.ttf", "MiSans" );
			});
		
		builder = builder.UseMauiCommunityToolkit();
		builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
