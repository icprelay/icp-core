using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System.Net.Http.Headers;

namespace Icp.Ui;

internal sealed class BearerTokenHandler(
    ITokenAcquisition tokenAcquisition,
    IOptions<IcpApiOptions> apiOptions) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && apiOptions.Value.Environment is not "Development")
        {
            var scope = apiOptions.Value.Scope;
            if (string.IsNullOrWhiteSpace(scope))
                throw new InvalidOperationException($"{IcpApiOptions.SectionName}:Scope configuration is required.");

            try
            {
                var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync([scope.Trim()]);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            catch (MsalUiRequiredException ex)
            {
                throw new MicrosoftIdentityWebChallengeUserException(ex, [scope.Trim()]);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
