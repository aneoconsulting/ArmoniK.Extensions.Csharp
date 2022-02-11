trap "vim -c :q" ERR EXIT

set -e

mkdir -p /tmp/streamworker_test_client
rm -rf /tmp/streamworker_test_client/*

dotnet publish -c Release Common/StreamWrapper/tests/Client/ArmoniK.Extensions.Common.StreamWrapper.Tests.Client.csproj -o /tmp/streamworker_test_client
docker build -t dockerhubaneo/armonik_worker_streamworker_test_client:test -f Common/StreamWrapper/tests/Client/Dockerfile.Copy /tmp/streamworker_test_client
