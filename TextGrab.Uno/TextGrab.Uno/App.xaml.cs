using System.Diagnostics.CodeAnalysis;
using Uno.Resizetizer;

namespace TextGrab;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    public static Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)
                        .CoreLogLevel(LogLevel.Warning);
                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                        .Section<AppSettings>()
                )
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IFileService, FileService>();

                    // OCR engines (platform-specific)
#if WINDOWS
                    services.AddSingleton<IOcrEngine, WindowsOcrEngine>();
                    services.AddSingleton<IOcrEngine, TesseractOcrEngine>();
                    services.AddSingleton<IOcrEngine, WindowsAiOcrEngine>();
#endif

                    // OCR services
                    services.AddSingleton<ILanguageService, LanguageService>();
                    services.AddSingleton<IOcrService, OcrService>();

                    // TODO: Register remaining services as we port them
                    // services.AddSingleton<IHistoryService, HistoryService>();
                })
                .UseNavigation(RegisterRoutes)
            );

        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellModel)),
            new ViewMap<ShellPage>(),
            new ViewMap<EditTextPage, EditTextModel>(),
            new ViewMap<GrabFramePage, GrabFrameModel>(),
            new ViewMap<QuickLookupPage, QuickLookupModel>(),
            new ViewMap<SettingsPage, SettingsModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellModel>(),
                Nested:
                [
                    new("Shell", View: views.FindByView<ShellPage>(),
                        Nested:
                        [
                            new("EditText", View: views.FindByViewModel<EditTextModel>(), IsDefault: true),
                            new("GrabFrame", View: views.FindByViewModel<GrabFrameModel>()),
                            new("QuickLookup", View: views.FindByViewModel<QuickLookupModel>()),
                            new("Settings", View: views.FindByViewModel<SettingsModel>()),
                        ]),
                ])
        );
    }
}
