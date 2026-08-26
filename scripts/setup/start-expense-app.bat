@echo off
set "SQL_CONN=Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=Your_password123;Encrypt=False;TrustServerCertificate=True;"
start "" "F:\SQL\expense_tracker\src\ExpenseTracker.WinForms\bin\Debug\net10.0-windows\ExpenseTracker.WinForms.exe"
