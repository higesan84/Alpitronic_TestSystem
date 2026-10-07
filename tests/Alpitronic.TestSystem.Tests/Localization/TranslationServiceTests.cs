using Alpitronic.TestSystem.Localization;

namespace Alpitronic.TestSystem.Tests.Localization;

public sealed class TranslationServiceTests
{
    private static string CatalogPath => Path.Combine(AppContext.BaseDirectory, "Assets", "translations.json");

    [Fact]
    public async Task Json_catalog_exposes_all_requested_languages_and_defaults_to_italian()
    {
        var service = new TranslationService(new JsonTranslationDataProvider(CatalogPath));

        await service.InitializeAsync(CancellationToken.None);

        Assert.Equal("it-IT", service.CurrentLanguage.Code);
        Assert.Equal(new[] { "it-IT", "en-US", "de-DE", "ja-JP" },
            service.SupportedLanguages.Select(x => x.Code));
        Assert.Equal("Avvia sessione", service.Translate(TranslationKey.StartSessionButton));
    }

    [Fact]
    public async Task Switching_to_japanese_preserves_unicode_and_translates_visible_text()
    {
        var service = new TranslationService(new JsonTranslationDataProvider(CatalogPath));
        await service.InitializeAsync(CancellationToken.None);
        var languageChanged = false;
        service.LanguageChanged += (_, _) => languageChanged = true;

        service.SetLanguage("ja-JP");

        Assert.True(languageChanged);
        Assert.Equal("言語", service.Translate(TranslationKey.LanguageLabel));
        Assert.Equal("テストシステム — 充電セッション", service.Translate(TranslationKey.AppTitle));
        Assert.Equal("電力: 50.50 kW", service.Translate(TranslationKey.PowerFormat, 50.5m));
    }

    [Fact]
    public async Task Translation_service_uses_selected_culture_when_formatting_numbers()
    {
        var service = new TranslationService(new JsonTranslationDataProvider(CatalogPath));
        await service.InitializeAsync(CancellationToken.None);
        service.SetLanguage("de-DE");

        var text = service.Translate(TranslationKey.PowerFormat, 50.5m);

        Assert.Equal("Leistung: 50,50 kW", text);
    }
}
