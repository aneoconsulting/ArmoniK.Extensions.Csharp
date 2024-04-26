ARG WORKER_DLL_IMAGE=dockerhubaneo/armonik_worker_dll:0.14.6

FROM mcr.microsoft.com/dotnet/sdk:6.0-bookworm-slim AS build
WORKDIR /src
COPY . .


WORKDIR "/src/Tests/ArmoniK.EndToEndTests/ArmoniK.EndToEndTests.Worker"

RUN dotnet publish --self-contained -c Release -r linux-x64 -f net6.0 .


FROM ${WORKER_DLL_IMAGE} AS final

ENV ServiceAssemblyPath=/guest
COPY --from=build /src/Tests/ArmoniK.EndToEndTests/publish/ArmoniK.EndToEndTests.Worker/1.0.0-700/ /guest
