FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /workspace

EXPOSE 8080

CMD ["/bin/sh", "-c", "cd /workspace/src/TestJob.Api && dotnet restore --disable-parallel && dotnet build --no-restore && dotnet run --no-build --urls http://0.0.0.0:8080"]
