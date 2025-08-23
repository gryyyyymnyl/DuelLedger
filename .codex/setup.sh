# .codex/setup.sh
set -euo pipefail
mkdir -p "$HOME/.dotnet"
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
echo 'export PATH="$HOME/.dotnet:$PATH"' >> "$HOME/.profile"
export PATH="$HOME/.dotnet:$PATH"

# オプション: NuGet キャッシュ最適化
echo 'export NUGET_PACKAGES=$HOME/.nuget/packages' >> "$HOME/.profile"
