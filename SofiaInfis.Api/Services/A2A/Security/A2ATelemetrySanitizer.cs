using System.Text.RegularExpressions;
namespace SofiaInfis.Api.Services.A2A.Security;
public static partial class A2ATelemetrySanitizer
{
    public static string Code(string value) => CodePattern().IsMatch(value) ? value : "A2A_OPERATION_FAILED";
    public static string? ExecutionId(string? value) => Guid.TryParse(value, out var id) ? id.ToString() : null;
    [GeneratedRegex("^[A-Z0-9_]{1,80}$", RegexOptions.CultureInvariant)] private static partial Regex CodePattern();
}
