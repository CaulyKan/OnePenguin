FROM neo4j:3.4.0 as neo4j

WORKDIR /plugins
RUN wget https://github.com/neo4j-contrib/neo4j-apoc-procedures/releases/download/3.4.0.1/apoc-3.4.0.1-all.jar

ENV NEO4J_AUTH none