#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM base AS tools
WORKDIR /root/
RUN apt-get update && apt-get install -y curl
RUN curl -sSL https://aka.ms/getvsdbgsh > getvsdbg.sh
RUN bash getvsdbg.sh -v vs2019 -r linux-x64 -l .vs-debugger/vs2019
RUN bash getvsdbg.sh -v vs2022 -r linux-x64 -l .vs-debugger/vs2022


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DevelopmentKit/csharp/WorkerApi/ArmoniK.DevelopmentKit.WorkerApi/ArmoniK.DevelopmentKit.WorkerApi.csproj", "DevelopmentKit/csharp/WorkerApi/ArmoniK.DevelopmentKit.WorkerApi/"]
COPY ["DevelopmentKit/csharp/WorkerApi/ArmoniK.DevelopmentKit.WorkerApi.Common/ArmoniK.DevelopmentKit.WorkerApi.Common.csproj", "DevelopmentKit/csharp/WorkerApi/ArmoniK.DevelopmentKit.WorkerApi.Common/"]
COPY ["DevelopmentKit/csharp/Generated/src/ArmoniK.Csharp.csproj", "DevelopmentKit/csharp/Generated/src/"]
COPY ["DevelopmentKit/csharp/Common/ArmoniK.DevelopmentKit.Common/ArmoniK.DevelopmentKit.Common.csproj", "DevelopmentKit/csharp/Common/ArmoniK.DevelopmentKit.Common/"]
RUN dotnet restore "DevelopmentKit/csharp/WorkerApi/ArmoniK.DevelopmentKit.WorkerApi/ArmoniK.DevelopmentKit.WorkerApi.csproj"
COPY . .
WORKDIR "/src/DevelopmentKit/csharp/WorkerApi/ArmoniK.DevelopmentKit.WorkerApi"
RUN dotnet build "ArmoniK.DevelopmentKit.WorkerApi.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "ArmoniK.DevelopmentKit.WorkerApi.csproj" -c Debug -r linux-x64 --self-contained true -o /app/publish

FROM base AS final
WORKDIR /root
COPY --from=tools /root/.vs-debugger .vs-debugger
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArmoniK.DevelopmentKit.WorkerApi.dll"]