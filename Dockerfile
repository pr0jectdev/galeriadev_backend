FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish ImageGallery.Api/ImageGallery.Api.csproj -c Release -o out