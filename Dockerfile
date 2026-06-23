FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:10000
ENTRYPOINT ["dotnet", "ImageGallery.Api.dll"]