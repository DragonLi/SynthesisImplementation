using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        public virtual void Print(TextWriter writer)
        {
            writer.WriteLine(Loc.HintLine());
            writer.WriteLine(Message);
        }
    }

    public class ADTDuplicatedException : SemanticException
    {
        private readonly ADType _type;
        public ADTDuplicatedException(LexLocation loc, ADType type) : base("type constructor overloading is not implemented", loc)
        {
            _type = type;
        }

        public override void Print(TextWriter writer)
        {
            base.Print(writer);
            writer.WriteLine("there is another definition here:");
            writer.WriteLine(_type._lexLocation.HintLine());
        }
    }

    public class ValueCtorDuplicatedException : SemanticException
    {
        private readonly ConstructorTypeDecl _ctor;

        public ValueCtorDuplicatedException(ConstructorTypeDecl ctor, LexLocation loc) : base("value constructor overloading is not implemented", loc)
        {
            _ctor = ctor;
        }

        public override void Print(TextWriter writer)
        {
            base.Print(writer);
            writer.WriteLine("there is another definition here:");
            writer.WriteLine(_ctor._lexLocation.HintLine());
        }
    }
    
/*
            parser.Context.PrintTypeCtorDef(Console.Out);
            
        catch (SemanticException se)
        {
          se.Print(Console.Out);
        }
        
*/
    
    public partial class ParsingContext
    {
        internal static readonly List<string> EmptyListString = new List<string>();
        
        private readonly Stack<string> NameSpaceStack=new Stack<string>();

        private string _moduleName;

        private Dictionary<string,ADType> TypeCtorMap=new Dictionary<string, ADType>();
        
        private Dictionary<string,ConstructorTypeDecl> ValueCtorMap = new Dictionary<string, ConstructorTypeDecl>();
        
        private TypeCtorSig _definingCtor;
        private TypeExpr _definingTypeAsTyExpr;
        public TypeCtorSig DefiningCtorSig => _definingCtor;
        
        partial void CheckTypeCtorNameEnv(string name, LexLocation loc);
        partial void CheckValueCtorNameEnv(string name, LexLocation loc);

        public TypeExpr DefiningCtorAsType()
        {
            if (_definingTypeAsTyExpr != null)
                return _definingTypeAsTyExpr;
            var bTy = TyQName.Create(_definingCtor.Name, _definingCtor.Loc, this);
            if (_definingCtor.ParamList.Length > 0)
            {
                ListTypeExpr pLst = new ListTypeExpr();
                foreach (var pNm in _definingCtor.ParamList)
                {
                    //TODO improve location of parameter
                    pLst.Add(TyQName.Create(pNm,_definingCtor.Loc, this));
                }
                _definingTypeAsTyExpr = new TyApp(bTy,pLst,_definingCtor.Loc,this);
            }
            else
            {
                _definingTypeAsTyExpr = bTy;
            }

            return _definingTypeAsTyExpr;
        } 
        
        public void StartDefiningCtor(string name, List<string> paramLst, LexLocation lexLocation)
        {
            CheckTypeCtorNameEnv(name, lexLocation);
            if (TypeCtorMap.TryGetValue(name,out var ty))
                throw new ADTDuplicatedException(lexLocation,ty);
            _definingCtor = TypeCtorSig.Create(name, paramLst,lexLocation);
        }

        public void StopDefiningCtor(string name, ADType tydef)
        {
            //TODO add module name to ADT
            tydef.SetValCtor(_definingCtor.ValCtorList.ToArray());
            TypeCtorMap.Add(name,tydef);
            _definingCtor = null;
            _definingTypeAsTyExpr = null;
        }

        public void PrintTypeCtorDef(TextWriter writer)
        {
            writer.WriteLine($"{nameof(TypeCtorMap)} size: {TypeCtorMap.Count}");
            foreach (var pair in TypeCtorMap)
            {
                writer.WriteLine($"type constructor {pair.Key}, type:{pair.Value.GetType()}");
                foreach (var tuple in pair.Value.ValCtorLst)
                {
                    writer.Write($"  {tuple.Item1} :: ");
                    writer.WriteLine($"{PrettyPrinter.Print(tuple.Item2)}");
                }
            }
        }

        public void SetModuleName(QName qname)
        {
            qname.ToList.ForEach(nm =>
            {
                NameSpaceStack.Push(nm);                
            });
            _moduleName = qname.ToString();
        }

        public void AddDefiningValCtor(string valCtorName, ConstructorTypeDecl sig)
        {
            CheckValueCtorNameEnv(valCtorName, sig._lexLocation);
            if (ValueCtorMap.TryGetValue(valCtorName,out var ty))
                throw new ValueCtorDuplicatedException(ty,sig._lexLocation);
            _definingCtor.AddValCtor(valCtorName, sig.ValueTypeSig);
        }
    }

    public class TypeCtorSig
    {
        public string Name;
        public string[] ParamList;
        public LexLocation Loc;
        public List<Tuple<string, TypeExpr>> ValCtorList=new List<Tuple<string, TypeExpr>>();
        
        //TODO possible optimization, at most one type is defining!
        public static TypeCtorSig Create(string name, List<string> paramLst, LexLocation lexLocation)
        {
            return new TypeCtorSig
            {
                Name = name,
                ParamList = paramLst.ToArray(),
                Loc = lexLocation
            };
        }

        public void AddValCtor(string valCtorName, TypeExpr ty)
        {
            ValCtorList.Add(Tuple.Create(valCtorName,ty));
        }
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

    public partial class TyQName
    {
        public static TypeExpr Create(string name, LexLocation loc, ParsingContext ctx)
        {
            return new TyQName(new QNameList(new ListIdent {name}, loc,ctx),loc,ctx);
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
            ctx.SetModuleName(qname_);
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
            ctx.StartDefiningCtor(tyctordecl_.Name, tyctordecl_.ParamList,tyctordecl_.NameLoc);
        }
    }

    public partial class CoIndDeclTy
    {
        partial void ScanPass(ParsingContext ctx)
        {
            ctx.StartDefiningCtor(tyctordecl_.Name, tyctordecl_.ParamList,tyctordecl_.NameLoc);
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

    public abstract partial class CoIndTyCtorPre
    {
        private class CoIndTypeVisitor:Visitor<CoIndDeclTy, Void>
        {
            public static readonly CoIndTypeVisitor Instance = new CoIndTypeVisitor();
            public CoIndDeclTy Visit(CoIndDeclTy p, Void arg) => p;
        }

        public CoIndDeclTy CoIndTypeParam => Accept(CoIndTypeVisitor.Instance, new Void());
    }
    
    public abstract partial class ADType
    {
        public Tuple<string, TypeExpr>[] ValCtorLst { get; private set; }

        public void SetValCtor(Tuple<string,TypeExpr>[] valCtorList)
        {
            ValCtorLst = valCtorList;
        }
    }

    public partial class UnionType
    {
        partial void SemanticPass(ParsingContext ctx);
        
        partial void ScanPass(ParsingContext ctx)
        {
            //TODO handle two cases "of" ParamTypeDecl or ":" TypeExpr
            //TODO all names must defined before the current ADT(except the name and parameter type names of the defining ADT)
            //TODO type construtor parameter/arity checking(no GADT or dependent type)
            //TODO value constructors conform to same final type
            SemanticPass(ctx);
            ctx.StopDefiningCtor(indtyctorpre_.IndTypeParam.TyCtorDecl_.Name, this);
        }
    }
    
    public partial class RecordType
    {
        partial void SemanticPass(ParsingContext ctx);
        
        partial void ScanPass(ParsingContext ctx)
        {
            SemanticPass(ctx);
            ctx.StopDefiningCtor(indtyctorpre_.IndTypeParam.TyCtorDecl_.Name, this);
        }
    }
    
    public partial class CoType
    {
        partial void SemanticPass(ParsingContext ctx);
        
        partial void ScanPass(ParsingContext ctx)
        {
            SemanticPass(ctx);
            ctx.StopDefiningCtor(coindtyctorpre_.CoIndTypeParam.TyCtorDecl_.Name, this);
        }
    }

    public abstract partial class ConstructorTypeDecl
    {
        protected TypeExpr ValSig;
        public TypeExpr ValueTypeSig => ValSig;
    }

    public partial class EmptyCtrTyD
    {
        partial void ScanPass(ParsingContext ctx)
        {
            ValSig = ctx.DefiningCtorAsType();
        }
    }
    
    public partial class FullCtrTyD
    {
        partial void ScanPass(ParsingContext ctx)
        {
            ValSig = typeexpr_;
        }
    }
    
    public partial class ParamCtrTyD
    {
        partial void ScanPass(ParsingContext ctx)
        {
            ValSig = ParamTypeDecl_.AsCurriedHeadForm(ctx.DefiningCtorAsType());
        }
    }
    
    public partial class ParamTypeDecl
    {
        //TODO ParamTypeDecl (LastParamTy/ConsParamTy) curried form type expr as value constructor parameter
        //TODO need extra form for projection type!
        
        private class CurriedHeadFormVisitor:Visitor<TypeExpr,TypeExpr>
        {
            public static readonly CurriedHeadFormVisitor Instance = new CurriedHeadFormVisitor();
            public TypeExpr Visit(LastParamTy p, TypeExpr finalTy)
            {
                var ty = new TyArr(p.TypeExpr_,finalTy,p._lexLocation,null);
                return ty;
            }

            public TypeExpr Visit(ConsParamTy p, TypeExpr finalTy)
            {
                var final = p.ParamTypeDecl_.Accept(this, finalTy);
                //TODO merge location info 
                var ty = new TyArr(p.TypeExpr_,final,p._lexLocation,null);
                return ty;
            }
        }

        public TypeExpr AsCurriedHeadForm(TypeExpr finalTy) => Accept(CurriedHeadFormVisitor.Instance, finalTy);
    }
    
    public partial class CtrDecl
    {
        partial void ScanPass(ParsingContext ctx)
        {
            ctx.AddDefiningValCtor(simplename_.ToString(), constructortypedecl_);
        }
    }
    
}