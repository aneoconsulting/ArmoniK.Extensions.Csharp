trap "vim -c :q" ERR EXIT

set -e

mkdir -p /tmp/worker
rm -rf /tmp/worker/*

dotnet publish -c Release Common/StreamWrapper/tests/Server/ArmoniK.Extensions.Common.StreamWrapper.Tests.Server.csproj -o /tmp/worker
docker build -t dockerhubaneo/armonik_worker_streamworker_test:test -f Common/StreamWrapper/tests/Server/Dockerfile.Copy /tmp/worker
