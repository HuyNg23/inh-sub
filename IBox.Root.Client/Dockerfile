FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "IBox.Root.Client/IBox.Root.Client.csproj"
RUN dotnet publish "IBox.Root.Client/IBox.Root.Client.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgdiplus \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 5000
ENTRYPOINT ["dotnet", "IBox.Root.Client.dll"]
