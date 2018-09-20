param ([switch] $update)

if ($update) {
  .paket\paket.exe update
}

dotnet build src
