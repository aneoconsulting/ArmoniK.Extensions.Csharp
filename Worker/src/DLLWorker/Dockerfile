#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
ARG TARGETARCH


FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Worker/src/DLLWorker/ArmoniK.DevelopmentKit.Worker.DLLWorker.csproj", "Worker/src/DLLWorker/"]
COPY ["Common/src/Common/ArmoniK.DevelopmentKit.Common.csproj", "Common/src/Common/"]
COPY ["Client/src/Common/ArmoniK.DevelopmentKit.Client.Common.csproj", "Client/src/Common/"]
COPY ["Worker/src/Common/ArmoniK.DevelopmentKit.Worker.Common.csproj", "Worker/src/Common/"]
RUN dotnet restore "Worker/src/DLLWorker/ArmoniK.DevelopmentKit.Worker.DLLWorker.csproj"
COPY . .
WORKDIR "/src/Worker/src/DLLWorker"
ARG BUILD_CONFIG=Release
RUN dotnet build "ArmoniK.DevelopmentKit.Worker.DLLWorker.csproj" -c "${BUILD_CONFIG}" -f net6.0 -o /app/build

FROM build AS publish
ARG BUILD_CONFIG=Release
RUN case "$TARGETARCH" in \
        amd64) DOTNET_ARCH=linux-x64 ;; \
        arm64) DOTNET_ARCH=linux-arm64 ;; \
        "") case "$(uname -m)" in \
            x86_64) DOTNET_ARCH=linux-x64 ;; \
            aarch64) DOTNET_ARCH=linux-arm64 ;; \
            *) exit 1 ;; \
        esac ;; \
        *) exit 1 ;; \
    esac ; \
    dotnet publish "ArmoniK.DevelopmentKit.Worker.DLLWorker.csproj" -c "${BUILD_CONFIG}" -f net6.0 -o /app/publish -r "${DOTNET_ARCH}" --self-contained true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV LD_LIBRARY_PATH=.:$LD_LIBRARY_PATH
RUN groupadd --gid 5000 armonikuser && useradd --home-dir /home/armonikuser --create-home --uid 5000 --gid 5000 --shell /bin/sh --skel /dev/null armonikuser
RUN mkdir /cache && chown armonikuser: /cache
USER armonikuser

ENTRYPOINT ["dotnet", "ArmoniK.DevelopmentKit.Worker.DLLWorker.dll"]
