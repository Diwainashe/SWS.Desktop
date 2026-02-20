using Microsoft.Data.SqlClient;

namespace SWS.Desktop.Services;

/// <summary>
/// Utility for testing SQL Server connectivity.
/// </summary>
public static class SqlConnectionTester
{
    public static async Task<(bool Ok, string Message)> TestAsync(string connectionString)
    {
        try
        {
            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            return (true, "Connection OK.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
