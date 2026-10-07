
using System.Text.Json;

namespace Alpitronic.TestSystem.Localization;

/// <summary>
/// Provider temporaneo a file. Mantiene lo stesso contratto che userà un futuro
/// SqlTranslationDataProvider, evitando dipendenze dal database nella UI.
/// </summary>
public sealed class JsonTranslationDataProvider : ITranslationDataProvider
{
    private readonly string _filePath;

    public JsonTranslationDataProvider(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("A translation file path is required.", nameof(filePath));
        _filePath = filePath;
    }

    public async Task<TranslationCatalog> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            throw new FileNotFoundException("Translation catalog was not found.", _filePath);

        await using var stream = File.OpenRead(_filePath);
        var catalog = await JsonSerializer.DeserializeAsync<TranslationCatalog>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return catalog ?? throw new InvalidDataException("Translation catalog is empty or invalid JSON.");
    }
}
