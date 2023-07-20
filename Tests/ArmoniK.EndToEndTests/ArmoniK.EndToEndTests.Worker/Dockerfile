FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Tests/ArmoniK.EndToEndTests/ArmoniK.EndToEndTests.Common/ArmoniK.EndToEndTests.Common.csproj", "Tests/ArmoniK.EndToEndTests/ArmoniK.EndToEndTests.Common/"]
COPY ["Tests/ArmoniK.EndToEndTests/ArmoniK.EndToEndTests.Worker/ArmoniK.EndToEndTests.Worker.csproj", "Tests/ArmoniK.EndToEndTests/ArmoniK.EndToEndTests.Worker/"]
COPY ["Worker/src/Symphony/ArmoniK.DevelopmentKit.Worker.Symphony.csproj","Worker/src/Symphony/" ]
COPY ["Worker/src/Unified/ArmoniK.DevelopmentKit.Worker.Unified.csproj","Worker/src/Unified/"]

RUN dotnet restore "Tests/ArmoniK.EndToEndTests/ArmoniK.EndToEndTests.Worker/ArmoniK.EndToEndTests.Worker.csproj"
COPY . .

RUN dotnet build "Tests/ArmoniK.EndToEndTests/ArmoniK.EndToEndTests.Worker/ArmoniK.EndToEndTests.Worker.csproj" -c Release -f net6.0 -o /app/build

WORKDIR "/src/Tests/ArmoniK.EndToEndTests/ArmoniK.EndToEndTests.Worker"

RUN dotnet publish "ArmoniK.EndToEndTests.Worker.csproj" -c Release -f net6.0 -o /app/publish --self-contained true


FROM dockerhubaneo/armonik_worker_dll:0.11.2-011133g422dad9.33.422dad9 AS final

WORKDIR /guest

COPY --from=build /app/publish .