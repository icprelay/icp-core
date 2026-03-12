using System.Text;
using Microsoft.Data.SqlClient;

static IEnumerable<string> SplitOnGo(string sql)
{
    var sb = new StringBuilder();
    using var sr = new StringReader(sql);
    string? line;
    while ((line = sr.ReadLine()) != null)
    {
        if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
        {
            var batch = sb.ToString().Trim();
            if (batch.Length > 0) yield return batch;
            sb.Clear();
            continue;
        }
        sb.AppendLine(line);
    }
    var last = sb.ToString().Trim();
    if (last.Length > 0) yield return last;
}

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: sqlrunner <serverFqdn> <database> <token> <sqlFile>");
    return 2;
}

var server = args[0];
var database = args[1];
var token = args[2];
var sqlFile = args[3];

var sqlText = await File.ReadAllTextAsync(sqlFile);

var cs = new SqlConnectionStringBuilder
{
    DataSource = server,
    InitialCatalog = database,
    Encrypt = true,
    TrustServerCertificate = false,
    ConnectTimeout = 30
}.ConnectionString;

using var conn = new SqlConnection(cs) { AccessToken = token };
await conn.OpenAsync();

foreach (var batch in SplitOnGo(sqlText))
{
    using var cmd = conn.CreateCommand();
    cmd.CommandTimeout = 0;
    cmd.CommandText = batch;
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine($"{sqlFile} executed successfully ({database}).");
return 0;