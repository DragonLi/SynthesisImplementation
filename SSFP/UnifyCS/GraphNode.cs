using System.Collections.Generic;

namespace UnifyCS
{
    public class GraphNode
    {
        //public List<GraphNode> ParentLst;
        public TermNode Symbol;
        public List<GraphNode> Children;
        public bool IsVisited;
        public GraphNode EqClsTag;
    }

    public class TermNode
    {
        
    }

    public class VariableNode : TermNode
    {
        public int VarIndex;        
    }

    public class FuncSym : TermNode
    {
        public string FunctionName;
    }

    public enum UnificationErrorCode
    {
        Success,
        Loop,
        Clash,
    }
    
    public class UnificationResult
    {
        public UnificationErrorCode ErrorCode;
        public Dictionary<int, GraphNode> Sub;
    }
}