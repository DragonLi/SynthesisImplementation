using System;
using System.Collections.Generic;

namespace UnifyCS
{
    public class Unifier
    {
        public Dictionary<int, GraphNode> Mapping;
        
        public UnificationResult IsUnifiable(GraphNode r1,GraphNode r2)
        {
            Mapping = new Dictionary<int, GraphNode>();
            var testPariLst = new List<Tuple<GraphNode, GraphNode>> {Tuple.Create(r1, r2)};
            
        } 
    }
}