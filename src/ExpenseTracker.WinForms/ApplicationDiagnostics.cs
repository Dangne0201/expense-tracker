using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.WinForms;

// Writes application failures to the trace listener and a per-user local log file.
internal static class ApplicationDiagnostics
{
    // Records operation and exception type while avoiding logging connection strings or passwords.
    public static void LogFailure(string operation, Exception exception)
    {
        var sqlError = exception is SqlException sqlException
            ? sqlException.Number.ToString(CultureInfo.InvariantCulture)
            : "none";
        var entry =
            $"[{DateTimeOffset.UtcNow:O}] {operation} failed. ExceptionType={exception.GetType().Name}; SqlError={sqlError}";
        Trace.TraceError(entry);

        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ExpenseTracker",
            "logs",
            "application.log");
        try
        {
            // Keep runtime diagnostics under LocalAppData rather than inside the repository.
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, entry + Environment.NewLine);
        }
        catch (IOException logException)
        {
            Trace.TraceWarning(
                "Could not write the application log. ExceptionType={0}",
                logException.GetType().Name);
        }
        catch (UnauthorizedAccessException logException)
        {
            Trace.TraceWarning(
                "Could not write the application log. ExceptionType={0}",
                logException.GetType().Name);
        }
    }
}
