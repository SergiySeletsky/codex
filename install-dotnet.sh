#!/usr/bin/env bash
# install-dotnet.sh - Install .NET 8 SDK locally for Codex builds.
# Usage: ./install-dotnet.sh [version]
# If no version is specified the latest 8.0 channel is installed.

set -euo pipefail

VERSION="${1:-8.0}"
INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"

curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel "$VERSION" --install-dir "$INSTALL_DIR"
rm /tmp/dotnet-install.sh

echo "export DOTNET_ROOT=\"$INSTALL_DIR\"" >> "$HOME/.bashrc"
echo "export PATH=\"$INSTALL_DIR:$PATH\"" >> "$HOME/.bashrc"

echo ".NET $VERSION installed to $INSTALL_DIR"
