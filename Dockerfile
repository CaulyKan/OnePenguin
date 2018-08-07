FROM microsoft/dotnet:2.1-sdk as build
WORKDIR /app

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.1-sdk as runtime
WORKDIR /app
COPY --from=build /app/OnePenguin.Service.WebApi/out .
ENTRYPOINT [ "dotnet", "OnePenguin.Service.WebApi.dll" ]