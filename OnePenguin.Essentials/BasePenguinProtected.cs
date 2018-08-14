using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OnePenguin.Essentials.Utilities;

namespace OnePenguin.Essentials
{
    public partial class BasePenguin
    {
        protected void SetRelation(string relation, long id) { }

        protected void SetRelations(string relation, List<long> ids) { }

        protected PenguinReference GetRelation(string relation) { throw new NotImplementedException(); }

        protected List<PenguinReference> GetRelations(string relation) { throw new NotImplementedException(); }

        protected object GetAttribute(string attribute) { throw new NotImplementedException(); }

        protected void SetAttribute(string attribute, object value) { }
    }
}