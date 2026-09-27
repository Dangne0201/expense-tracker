if (-not ("ExpenseTracker.Security.DpapiUserScope" -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ExpenseTracker.Security
{
    public static class DpapiUserScope
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int Length;
            public IntPtr Data;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CryptProtectData(
            ref DataBlob input,
            string description,
            IntPtr optionalEntropy,
            IntPtr reserved,
            IntPtr prompt,
            uint flags,
            out DataBlob output);

        [DllImport("crypt32.dll", SetLastError = true)]
        private static extern bool CryptUnprotectData(
            ref DataBlob input,
            IntPtr description,
            IntPtr optionalEntropy,
            IntPtr reserved,
            IntPtr prompt,
            uint flags,
            out DataBlob output);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);

        public static byte[] Protect(byte[] input)
        {
            return Transform(input, true);
        }

        public static byte[] Unprotect(byte[] input)
        {
            return Transform(input, false);
        }

        private static byte[] Transform(byte[] input, bool protect)
        {
            var pinned = GCHandle.Alloc(input, GCHandleType.Pinned);
            var inputBlob = new DataBlob { Length = input.Length, Data = pinned.AddrOfPinnedObject() };
            DataBlob outputBlob;
            try
            {
                var succeeded = protect
                    ? CryptProtectData(ref inputBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out outputBlob)
                    : CryptUnprotectData(ref inputBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out outputBlob);
                if (!succeeded)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var output = new byte[outputBlob.Length];
                try
                {
                    Marshal.Copy(outputBlob.Data, output, 0, output.Length);
                }
                finally
                {
                    LocalFree(outputBlob.Data);
                }
                return output;
            }
            finally
            {
                pinned.Free();
            }
        }
    }
}
'@
}

function Get-ExpenseCredentialFolder {
    return Join-Path ([Environment]::GetFolderPath("LocalApplicationData")) "ExpenseTracker"
}

function Get-ExpenseCredentialFile {
    $path = [Environment]::GetEnvironmentVariable("EXPENSE_TRACKER_CREDENTIAL_FILE", "Process")
    if ([string]::IsNullOrWhiteSpace($path)) {
        return Join-Path (Get-ExpenseCredentialFolder) "app-db-password.bin"
    }

    return [IO.Path]::GetFullPath($path)
}

function Get-ExpenseSqlAdminCredentialFile {
    $projectName = [Environment]::GetEnvironmentVariable("COMPOSE_PROJECT_NAME", "Process")
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
    $defaultProjectName = [IO.Path]::GetFileName($repoRoot).ToLowerInvariant() -replace '[^a-z0-9_-]', ''
    if ([string]::IsNullOrWhiteSpace($projectName)) {
        $projectName = $defaultProjectName
    }
    $projectName = $projectName.ToLowerInvariant() -replace '[^a-z0-9_-]', ''
    $fileName = if ($projectName -eq $defaultProjectName) {
        "sql-admin-password.bin"
    }
    else {
        "sql-admin-$projectName-password.bin"
    }
    return Join-Path (Get-ExpenseCredentialFolder) $fileName
}

function Read-ExpenseProtectedSecret([string]$Path) {
    $protectedBytes = [IO.File]::ReadAllBytes($Path)
    $secretBytes = [ExpenseTracker.Security.DpapiUserScope]::Unprotect($protectedBytes)
    return [Text.Encoding]::UTF8.GetString($secretBytes)
}

function Save-ExpenseProtectedSecret([string]$Path, [string]$Secret) {
    $folder = [IO.Path]::GetDirectoryName($Path)
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $secretBytes = [Text.Encoding]::UTF8.GetBytes($Secret)
    $protectedBytes = [ExpenseTracker.Security.DpapiUserScope]::Protect($secretBytes)
    [IO.File]::WriteAllBytes($Path, $protectedBytes)
}

function New-ExpensePassword {
    $alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789"
    $randomBytes = New-Object byte[] 40
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $random.GetBytes($randomBytes)
    }
    finally {
        $random.Dispose()
    }

    $randomPart = -join ($randomBytes | ForEach-Object {
        $alphabet[[int]$_ % $alphabet.Length]
    })
    return "Aa7!$randomPart"
}
