using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using QUT.Gppg;

namespace SimpleType.Absyn
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct Void
    {
    }

    public class SemanticException : Exception
    {
        public LexLocation Loc { get; }

        public SemanticException(string message, LexLocation loc) : base(message)
        {
            Loc = loc;
        }
    }
/*
        catch (SemanticException se)
        {
          Console.Out.WriteLine(parser.HintAt(se.Loc));
          Console.Out.WriteLine(se.Message);
        }
*/
    
    public partial class ParsingContext
    {
        internal static readonly List<string> EmptyListString = new List<string>();
        
        public readonly Stack<string> NameSpaceStack=new Stack<string>();
        public Dictionary<string,ADType> TypeCtorMap=new Dictionary<string, ADType>();
        public Dictionary<string,TypeCtorSig> TypeCtorSigMap = new Dictionary<string, TypeCtorSig>();

        public void AddCtorName(string name, List<string> paramLst, LexLocation lexLocation)
        {
            if (TypeCtorSigMap.ContainsKey(name))
                throw new SemanticException("type constructor overloading is not implemented",lexLocation);
            TypeCtorSigMap.Add(name,new TypeCtorSig
            {
                Name = name,
                ParamList = paramLst.ToArray()
            });
        }
    }

    public class TypeCtorSig
    {
        public string Name;
        public string[] ParamList;
    }

    public partial class NameIdent
    {
        public override string ToString()
        {
            return ident_;
        }
    }
    
    public abstract partial class QName
    {
        public abstract ListIdent ToList { get; }
    }
    
    public partial class QNameList
    {
        private string _str;
        partial void ScanPass(ParsingContext ctx)
        {
            var b = new StringBuilder();
            ListIdent_.ForEach(str=>b.Append(str).Append("."));
            _str = b.Length <= 0 ? string.Empty : b.ToString(0, b.Length - 1);
        }

        public override ListIdent ToList => listident_;
        
        public override string ToString()
        {
            return _str;
        }
    }

    public partial class TyDeclName
    {
        public override LexLocation NameLoc => simplename_._lexLocation;
        public override string Name => simplename_.ToString();
        public override List<string> ParamList => typeparamlist_.ToList;        
    }

    public abstract partial class TyCtorDecl
    {
        private class GetCtorVisitor:Visitor<TyDeclName,Void>
        {
            public static readonly GetCtorVisitor Instance = new GetCtorVisitor();
            public TyDeclName Visit(TyDeclName p, Void arg) => p;
        }

        public TyDeclName DeclarionParam => Accept(GetCtorVisitor.Instance, new Void());
        public abstract string Name { get; }
        public abstract List<string> ParamList { get; }
        public abstract LexLocation NameLoc { get; }
    }
    
    public partial class ModuleBindName
    {
        partial void ScanPass(ParsingContext ctx)
        {
            qname_.ToList.ForEach(nm =>
            {
                ctx.NameSpaceStack.Push(nm);                
            });
        }
    }

    public partial class ImportModule
    {
        partial void ScanPass(ParsingContext ctx)
        {
            Console.WriteLine($"TODO: open module {qname_}");
        }
    }

    public partial class ImportModuleAs
    {
        partial void ScanPass(ParsingContext ctx)
        {
            Console.WriteLine($"TODO: open module {qname_} as {simplename_}");
        }
    }

    public partial class TypeParamList
    {
        private class PListVisitor:Visitor<List<string>,Void>
        {
            public static readonly PListVisitor Instance = new PListVisitor();
            public List<string> Visit(EmptyTyP p, Void arg)
            {
                return ParsingContext.EmptyListString;
            }

            public List<string> Visit(ConsTyP p, Void arg)
            {
                var list = p.TypeParamList_.ToList;
                if (list == ParsingContext.EmptyListString)
                    list = new List<string>();
                list.Add(p.Ident_);
                return list;
            }
        }

        public List<string> ToList => Accept(PListVisitor.Instance, new Void());
    }
    
    public partial class IndDeclTy
    {
        partial void ScanPass(ParsingContext ctx)
        {
            ctx.AddCtorName(tyctordecl_.Name, tyctordecl_.ParamList,tyctordecl_.NameLoc);
        }
    }

    public abstract partial class IndTyCtorPre
    {
        private class IndTypeVisitor:Visitor<IndDeclTy, Void>
        {
            public static readonly IndTypeVisitor Instance = new IndTypeVisitor();
            public IndDeclTy Visit(IndDeclTy p, Void arg) => p;
        }

        public IndDeclTy IndTypeParam => Accept(IndTypeVisitor.Instance, new Void());
    }
    
    public partial class UnionType
    {
        partial void SemanticPass(ParsingContext ctx);
        
        partial void ScanPass(ParsingContext ctx)
        {
            SemanticPass(ctx);    
            ctx.TypeCtorMap.Add(indtyctorpre_.IndTypeParam.TyCtorDecl_.Name,this);
        }
    }
}