using System.Text;
using System.Text.RegularExpressions;

namespace QueryAI.Api.IntegrationTests.Infrastructure;

internal sealed record EndpointInfo(string Method, string Template, IReadOnlyList<object> Metadata)
{
    public HttpRequestMessage CreateRequest()
    {
        var path = Regex.Replace(Template, "\\{[^}]+\\}", Guid.NewGuid().ToString());
        var request = new HttpRequestMessage(new HttpMethod(Method), path);
        if (Method is not "GET")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        return request;
    }
}
