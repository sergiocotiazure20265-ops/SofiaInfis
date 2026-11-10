using System.Net;
using SofiaInfis.Api.Services.A2A.Configuration;
namespace SofiaInfis.Api.Services.A2A.Security;

public static class AgentDestinationPolicy
{
    public static bool IsAllowed(AgentOptions agent, Uri uri, bool allowHttp)
    {
        if (!uri.IsAbsoluteUri || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || (uri.Scheme != "https" && !(allowHttp && uri.Scheme == "http"))) return false;
        return agent.AllowedDestinations.Any(s => Uri.TryCreate(s, UriKind.Absolute, out var approved) &&
            approved.Scheme == uri.Scheme && approved.IdnHost == uri.IdnHost && approved.Port == uri.Port &&
            approved.AbsolutePath == uri.AbsolutePath && string.IsNullOrEmpty(approved.Query) && string.IsNullOrEmpty(approved.UserInfo));
    }
    public static bool IsAllowedAddress(AgentOptions agent, IPAddress actual) =>
        agent.AllowedAddresses.Any(s => IPAddress.TryParse(s, out var allowed) &&
            allowed.MapToIPv6().Equals(actual.MapToIPv6()));
}
