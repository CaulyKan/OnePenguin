using Neo4j.Driver.V1;
using OnePenguin.Essentials;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace OnePenguin.Service.Persistence.Neo4jPersistenceDriver
{
    public class Neo4jConverter
    {
        public static BasePenguin ConvertToPenguin(IStatementResult result)
        {
            var record = result.First();
            var obj = record["obj"].As<INode>();
            var relationsOut = record["relations_out"].As<IRelationship>();
            var relationsIn = record["relations_in"].As<IRelationship>();

            return null;
        }
    }
}
