ARG WORKER_DLL_IMAGE=dockerhubaneo/armonik_worker_dll:0.11.2-011133g422dad9.33.422dad9
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .


WORKDIR "/src/Tests/ArmoniK.EndToEndTests/ArmoniK.EndToEndTests.Worker"

RUN dotnet publish --self-contained -c Release -r linux-x64 -f net6.0 .


FROM ${WORKER_DLL_IMAGE} AS final

ENV ServiceAssemblyPath=/guest
COPY --from=build /src/Tests/ArmoniK.EndToEndTests/publish/ArmoniK.EndToEndTests.Worker/1.0.0-700/ /guest
