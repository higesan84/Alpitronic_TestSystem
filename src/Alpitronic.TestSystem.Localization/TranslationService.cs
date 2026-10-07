
using System.Globalization;

namespace Alpitronic.TestSystem.Localization;

public sealed class TranslationService : ITranslationService
{
    private readonly ITranslationDataProvider _dataProvider;
    private TranslationCatalog? _catalog;
    private SupportedLanguage? _currentLanguage;

    public TranslationService(ITranslationDataProvider dataProvider) => _dataProvider = dataProvider;

    public event EventHandler? LanguageChanged;
    public IReadOnlyList<SupportedLanguage> SupportedLanguages => _catalog?.Languages ?? [];
    public SupportedLanguage CurrentLanguage => _currentLanguage
        ?? throw new InvalidOperationException("TranslationService must be initialized before use.");

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var catalog = await _dataProvider.LoadAsync(cancellationToken);
        Validate(catalog);
        _catalog = catalog;
        _currentLanguage = catalog.Languages.Single(x => x.Code == catalog.DefaultLanguageCode);
    }

    public void SetLanguage(string languageCode)
    {
        EnsureInitialized();
        var language = _catalog!.Languages.SingleOrDefault(x =>
            string.Equals(x.Code, languageCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentOutOfRangeException(nameof(languageCode), "Unsupported language.");
        if (language.Code == CurrentLanguage.Code) return;

        _currentLanguage = language;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Translate(TranslationKey key, params object?[] arguments)
    {
        EnsureInitialized();
        var keyName = key.ToString();
        var resources = _catalog!.Resources[CurrentLanguage.Code];
        var template = resources[keyName];
        return arguments.Length == 0
            ? template
            : string.Format(CultureInfo.GetCultureInfo(CurrentLanguage.CultureName), template, arguments);
    }

    private static void Validate(TranslationCatalog catalog)
    {
        if (catalog.Languages.Count == 0)
            throw new InvalidDataException("Translation catalog must contain at least one language.");
        if (catalog.Languages.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != catalog.Languages.Count)
            throw new InvalidDataException("Translation catalog contains duplicate language codes.");
        if (!catalog.Languages.Any(x => x.Code == catalog.DefaultLanguageCode))
            throw new InvalidDataException("Default language is not declared in the catalog.");

        foreach (var language in catalog.Languages)
        {
            if (!catalog.Resources.TryGetValue(language.Code, out var resources))
                throw new InvalidDataException($"Missing resources for language '{language.Code}'.");
            foreach (var key in Enum.GetNames<TranslationKey>())
            {
                if (!resources.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                    throw new InvalidDataException($"Missing translation '{key}' for '{language.Code}'.");
            }
        }
    }

    private void EnsureInitialized()
    {
        if (_catalog is null || _currentLanguage is null)
            throw new InvalidOperationException("TranslationService must be initialized before use.");
    }
}
