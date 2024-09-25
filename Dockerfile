FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY ./Orbitax.K8sClient.Api/*.csproj ./
RUN dotnet add package System.Security.Cryptography.X509Certificates
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/out ./

ENTRYPOINT ["dotnet", "Orbitax.K8sClient.Api.dll"]
