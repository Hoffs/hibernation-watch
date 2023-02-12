using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;

namespace HibernationWatch;

public class BasicPromptCodeReceiver : ICodeReceiver
{
    public string RedirectUri => "http://localhost:1111/auth";

    public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken)
    {
        Console.WriteLine($"Visit: '{url.Build().AbsoluteUri}'");
        Console.Write("Received query string:");
        var query = await Console.In.ReadLineAsync(taskCancellationToken);
        Console.WriteLine(string.Empty);
        return new AuthorizationCodeResponseUrl(query);
    }
}