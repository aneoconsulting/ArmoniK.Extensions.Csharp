#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM base AS tools

RUN mkdir -p /root/.vs-debugger
WORKDIR /root/.vs-debugger

RUN apt-get update && apt-get install -y curl
RUN curl -sSL https://aka.ms/getvsdbgsh > GetVsDbg.sh
RUN bash GetVsDbg.sh -v vs2019 -r linux-x64 -l vs2019
RUN bash GetVsDbg.sh -v vs2022 -r linux-x64 -l vs2022

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WorkerApi/ArmoniK.DevelopmentKit.WorkerApi/ArmoniK.DevelopmentKit.WorkerApi.csproj", "WorkerApi/ArmoniK.DevelopmentKit.WorkerApi/"]
COPY ["WorkerApi/ArmoniK.DevelopmentKit.WorkerApi.Common/ArmoniK.DevelopmentKit.WorkerApi.Common.csproj", "WorkerApi/ArmoniK.DevelopmentKit.WorkerApi.Common/"]
RUN dotnet restore "WorkerApi/ArmoniK.DevelopmentKit.WorkerApi/ArmoniK.DevelopmentKit.WorkerApi.csproj"
COPY . .
WORKDIR "/src/WorkerApi/ArmoniK.DevelopmentKit.WorkerApi"
RUN dotnet build "ArmoniK.DevelopmentKit.WorkerApi.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "ArmoniK.DevelopmentKit.WorkerApi.csproj" -c Debug -o /app/publish -r linux-x64 --self-contained true

FROM base AS final
WORKDIR /root
COPY --from=tools /root/.vs-debugger .vs-debugger
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArmoniK.DevelopmentKit.WorkerApi.dll"]
