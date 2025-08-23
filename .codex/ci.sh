set -euo pipefail
dotnet --info
dotnet restore
dotnet build -c Release --nologo
dotnet test  -c Release --no-build --nologo || true
