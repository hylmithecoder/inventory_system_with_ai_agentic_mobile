using Microsoft.Extensions.Logging;

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

		// Load .env file
		DotEnv.Load(Path.Combine(FileSystem.AppDataDirectory, ".env"));

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
