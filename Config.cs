using System.Text.Json.Serialization;

namespace HibernationWatch;

public record Config(
    [property: JsonRequired] string Ip,
    [property: JsonRequired] int Port,
    [property: JsonRequired] string Mode,
    [property: JsonRequired] string DeviceModelId,
    [property: JsonRequired] string SecretKey,
    [property: JsonRequired] string ActionResume,
    [property: JsonRequired] string ActionSuspend,
    [property: JsonRequired] bool Debug
);