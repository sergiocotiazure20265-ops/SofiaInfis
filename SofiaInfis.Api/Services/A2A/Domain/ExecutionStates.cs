namespace SofiaInfis.Api.Services.A2A.Domain;

public static class ExecutionStates
{
    public const string Running = "em_andamento";
    public const string Completed = "concluida";
    public const string Partial = "parcial";
    public const string Failed = "falhou";
    public const string Canceled = "cancelada";
    public const string Interrupted = "interrompida";
    public static bool IsTerminal(string state) => state != Running && state != "aguardando";
}
