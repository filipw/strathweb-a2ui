#!/usr/bin/env bash
# Builds a throwaway console app against the packed packages, from a local feed only, and runs it.
#
# Packing is not proof of anything: a package can pack cleanly and still fail to restore because of a
# missing dependency, a bad target framework, or a file that never made it in.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
FEED="$(cd "${1:-$REPO_ROOT/artifacts}" && pwd)"

VERSION="$(ls "$FEED"/Strathweb.A2UI.AgentFramework.*.nupkg \
  | head -1 \
  | sed -E 's|.*Strathweb\.A2UI\.AgentFramework\.(.*)\.nupkg|\1|')"

if [[ -z "$VERSION" ]]; then
  echo "error: no Strathweb.A2UI.AgentFramework package found in $FEED" >&2
  exit 1
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

cat > "$WORK/nuget.config" <<EOM
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$FEED" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOM

cat > "$WORK/quickstart.csproj" <<EOM
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Strathweb.A2UI.AgentFramework" Version="$VERSION" />
  </ItemGroup>
</Project>
EOM

cat > "$WORK/Program.cs" <<'EOM'
using Strathweb.A2UI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Surfaces;

var catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);
var s = A2UISurface.Create("quickstart", catalog);
var ui = s.Components;

var surface = s
    .Root(ui.Card(ui.Column(
        ui.Text("How satisfied were you?").Variant(TextVariant.H3),
        ui.ChoicePicker()
            .Label("Your rating")
            .Options(("Very", "5"), ("Not at all", "1"))
            .Value(Bind.Path("/rating")),
        ui.Button("Submit").Primary().OnClick(Act.Event("submit", ("rating", Bind.Path("/rating")))))))
    .WithData(data => data["rating"] = null)
    .Build();

var part = A2UIParts.Create(surface.Messages);
var mimeType = part.Metadata!["mimeType"].GetString();

if (mimeType != "application/a2ui+json")
{
    Console.Error.WriteLine($"Unexpected MIME type: {mimeType}");
    return 1;
}

Console.WriteLine($"Built surface '{surface.SurfaceId}' as {surface.Messages.Count} messages, sent as {mimeType}.");
return 0;
EOM

echo "Restoring quickstart against $FEED (Strathweb.A2UI.* $VERSION) ..."
dotnet run --project "$WORK/quickstart.csproj"
