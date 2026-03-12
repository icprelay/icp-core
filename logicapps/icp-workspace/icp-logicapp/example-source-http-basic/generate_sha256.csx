// Add the required libraries
#r "Newtonsoft.Json"
#r "Microsoft.Azure.Workflows.Scripting"
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Workflows.Scripting;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.IO.Pipes;

/// <summary>
/// Executes the inline csharp code.
/// </summary>
/// <param name="context">The workflow context.</param>
/// <remarks> This is the entry-point to your code. The function signature should remain unchanged.</remarks>
public static async Task<string> Run(WorkflowContext context, ILogger log)
{
  JToken triggerOutputs = (await context.GetTriggerResults().ConfigureAwait(false)).Outputs;

  // use body from trigger payload.
  var payload = triggerOutputs?["body"]?.ToString();

  return Sha256Hex(payload);
}

public static string Sha256Hex(string payload)
{
    var bytes = Encoding.UTF8.GetBytes(payload);
    var hash = SHA256.HashData(bytes);              // .NET 6+
    return Convert.ToHexString(hash).ToLowerInvariant();
}