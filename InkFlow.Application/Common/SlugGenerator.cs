using System.Globalization;
using System.Text;

namespace InkFlow.Application.Common;

public static class SlugGenerator
{
    public static string Create(string value, int maximumLength, string fallback)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        var slug = builder.ToString().Normalize(NormalizationForm.FormC);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        slug = slug.Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
            slug = fallback;

        return slug[..Math.Min(slug.Length, maximumLength)].TrimEnd('-');
    }
}