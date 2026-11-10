using System.Net;
using System.Net.Sockets;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Security;

namespace SofiaInfis.Api.Services.A2A.Transport;

public sealed class A2AHttpClientFactory(IHttpClientFactory clients)
{
    public HttpClient Create(AgentOptions agent) => clients.CreateClient("SofiaA2A:" + agent.IdAgente);
    public static HttpMessageHandler Handler(AgentOptions agent, bool allowHttp)
    {
        var sockets = new SocketsHttpHandler
        {
            AllowAutoRedirect = false, UseProxy = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PooledConnectionLifetime = TimeSpan.FromMinutes(1)
        };
        sockets.ConnectCallback = async (context, ct) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            if (addresses.Length == 0 || addresses.Any(ip => !AgentDestinationPolicy.IsAllowedAddress(agent, ip)))
                throw new A2AException("autorizacao", "DESTINATION_ADDRESS_NOT_ALLOWED", 403);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(addresses[0], context.DnsEndPoint.Port), ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch { socket.Dispose(); throw; }
        };
        return new DestinationGuard(agent, allowHttp) { InnerHandler = sockets };
    }
    private sealed class DestinationGuard(AgentOptions agent, bool allowHttp) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is null || !AgentDestinationPolicy.IsAllowed(agent, request.RequestUri, allowHttp))
                throw new A2AException("autorizacao", "DESTINATION_NOT_ALLOWED", 403);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
