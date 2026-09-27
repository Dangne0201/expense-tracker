namespace ExpenseTracker.Core;

public static class DatabaseFailureMessages
{
    public static string ForOperation(string operation, int? sqlErrorNumber)
    {
        return sqlErrorNumber switch
        {
            2601 or 2627 => $"{operation} failed because a value must be unique.",
            547 => $"{operation} failed because the data conflicts with a database rule.",
            -2 or 2 or 53 or 121 or 233 or 258 or 10053 or 10054 or 10060 or 10061 or 11001 =>
                $"{operation} failed because the database is unavailable. Check SQL Server and try again.",
            _ => $"{operation} failed. Try again; if the problem continues, check the application log."
        };
    }
}
