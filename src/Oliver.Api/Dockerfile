#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/windows/nanoserver:sac2016 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-1903 AS build
WORKDIR /src
COPY ./src .
WORKDIR "/src/Oliver.Api"
RUN dotnet publish "Oliver.Api.csproj" -c Release -r win10-x64 -p:PublishSingleFile=true -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN dir

HEALTHCHECK --interval=5s CMD powershell -command \
    try { \
     $response = Invoke-WebRequest http://localhost:5000/infrastructure/health; \
     if ($response.StatusCode -eq 200) { return 0 } \
     else { return 1 }; \
    } catch { return 1 };

ENTRYPOINT ["Oliver.Api.exe"]