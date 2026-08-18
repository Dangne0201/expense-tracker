param(
    [string]$saPassword = "Your_password123"
)

# Start docker-compose in repo root
Write-Output "Starting development SQL Server container (docker-compose up -d)..."
docker-compose up -d

$container = "expense-mssql"
Write-Output "Waiting for SQL Server container '$container' to be ready..."
$max = 60
$i = 0
while ($i -lt $max) {
    try {
        docker exec $container /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P $saPassword -Q "SELECT 1" > $null 2>&1
        Write-Output "SQL Server is ready."
        break
    } catch {
        Start-Sleep -Seconds 2
        $i++
    }
}

if ($i -ge $max) {
    Write-Error "SQL Server did not become ready in time (waited $($max*2) seconds)."
    exit 1
}

Write-Output "Initializing database from data/init.sql..."
docker exec -i $container /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P $saPassword -i /init/init.sql
Write-Output "Database initialization complete."

Write-Output "You can now run the WinForms app. If you want the app to use this SQL Server, set the environment variable SQL_CONN with a connection string, for example:"
Write-Output "  $env:SQL_CONN = 'Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=Your_password123;'"
