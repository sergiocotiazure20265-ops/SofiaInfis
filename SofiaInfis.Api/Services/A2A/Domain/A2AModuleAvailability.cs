using SofiaInfis.Api.Services.A2A.Configuration;
namespace SofiaInfis.Api.Services.A2A.Domain;
public sealed class A2AModuleAvailability(A2AOptions options)
{
    public bool Enabled => options.Enabled;
    public bool Valid => A2AOptionsValidator.IsValid(options);
    public volatile bool Ready;
}
