param ([switch] $update)

if ($update) {
  .paket\paket.exe update
}

dotnet build src
if ($LastErrorCode -ne 0) { throw }
