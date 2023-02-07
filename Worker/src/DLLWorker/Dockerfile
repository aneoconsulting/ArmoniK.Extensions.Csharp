#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
ARG TARGETOS TARGETARCH
ENV LINARCH $TARGETARCH
RUN echo "LINARCH = [$LINARCH]"

FROM base AS tools
ARG TARGETOS TARGETARCH
ENV LINARCH $TARGETARCH
RUN echo "LINARCH = [$LINARCH]"

RUN mkdir -p /root/.vs-debugger
WORKDIR /root/.vs-debugger

RUN apt-get update && apt-get install -y curl
RUN curl -sSL https://aka.ms/getvsdbgsh > GetVsDbg.sh

RUN if [ "$LINARCH" = "amd64" ]; then bash GetVsDbg.sh -v vs2019 -r linux-x64 -l vs2019; else echo "not amd64"; fi
RUN if [ "$LINARCH" = "amd64" ]; then bash GetVsDbg.sh -v vs2022 -r linux-x64 -l vs2022; else echo "not amd64"; fi

RUN if [ "$LINARCH" = "arm64" ]; then bash GetVsDbg.sh -v vs2019 -r linux-arm64 -l vs2019; else echo "not arm64"; fi
RUN if [ "$LINARCH" = "arm64" ]; then bash GetVsDbg.sh -v vs2022 -r linux-arm64 -l vs2022; else echo "not arm64"; fi

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Worker/src/DLLWorker/ArmoniK.DevelopmentKit.Worker.DLLWorker.csproj", "Worker/src/DLLWorker/"]
COPY ["Common/src/Common/ArmoniK.DevelopmentKit.Common.csproj", "Common/src/Common/"]
COPY ["Client/src/Common/ArmoniK.DevelopmentKit.Client.Common.csproj", "Client/src/Common/"]
COPY ["Worker/src/Common/ArmoniK.DevelopmentKit.Worker.Common.csproj", "Worker/src/Common/"]
RUN dotnet restore "Worker/src/DLLWorker/ArmoniK.DevelopmentKit.Worker.DLLWorker.csproj"
COPY . .
RUN find \( -iname obj -o -iname bin \) -exec rm -rf {} +
WORKDIR "/src/Worker/src/DLLWorker"
RUN dotnet build "ArmoniK.DevelopmentKit.Worker.DLLWorker.csproj" -c Debug -f net6.0 -o /app/build

FROM build AS publish
ARG TARGETOS TARGETARCH
ENV LINARCH $TARGETARCH
RUN echo "LINARCH = [$LINARCH]"
RUN if [ "$LINARCH" = "amd64" ]; then dotnet publish "ArmoniK.DevelopmentKit.Worker.DLLWorker.csproj" -c Debug -f net6.0 -o /app/publish -r linux-x64 --self-contained true ; else echo "not amd64"; fi 
RUN if [ "$LINARCH" = "arm64" ]; then dotnet publish "ArmoniK.DevelopmentKit.Worker.DLLWorker.csproj" -c Debug -f net6.0 -o /app/publish -r linux-arm64 --self-contained true; else echo "not arm64"; fi

FROM base AS final
ARG TARGETOS TARGETARCH
ENV LINARCH $TARGETARCH
RUN echo "LINARCH = [$LINARCH]"

WORKDIR /app
COPY --from=publish /app/publish .
ENV LD_LIBRARY_PATH=.:$LD_LIBRARY_PATH

RUN groupadd --gid 5000 armonikuser && useradd --home-dir /home/armonikuser --create-home --uid 5000 --gid 5000 --shell /bin/sh --skel /dev/null armonikuser

COPY --from=tools /root/.vs-debugger /home/armonikuser/.vs-debugger
RUN mkdir /cache && chown armonikuser: /cache && chown -R armonikuser: /home/armonikuser/.vs-debugger
USER armonikuser

ENTRYPOINT ["dotnet", "ArmoniK.DevelopmentKit.Worker.DLLWorker.dll"]
