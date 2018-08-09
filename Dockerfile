FROM neo4j:3.4.0 as penguin-neo4j

ADD https://github.com/neo4j-contrib/neo4j-apoc-procedures/releases/download/3.4.0.1/apoc-3.4.0.1-all.jar /plugins/
RUN chown -R neo4j:neo4j /plugins && chmod -R 777 /plugins
ENV NEO4J_AUTH none

FROM microsoft/dotnet:2.1-sdk as penguin-dotnet

WORKDIR /app
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.1-sdk as penguin-target

WORKDIR /app
COPY --from=penguin-dotnet /app/OnePenguin.Service.WebApi/out .
ENTRYPOINT [ "dotnet", "OnePenguin.Service.WebApi.dll" ]