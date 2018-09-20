param ([switch] $update)

if ($update) {
  .paket\paket.exe update
}

dotnet build src
if ($LastExitCode -ne 0) { throw }
