﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using QUT.Gppg;
using Void = QUT.Gppg.Void;

namespace SimpleType.Absyn
{
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
        private QName _moduleQName;

        private Dictionary<string,ADType> TypeCtorMap=new Dictionary<string, ADType>();
        
        private Dictionary<string,ConstructorTypeDecl> ValueCtorMap = new Dictionary<string, ConstructorTypeDecl>();
        
        private TypeCtorSig _definingCtor;

        #region TODO check name helper functions
        
        partial void CheckTypeCtorNameEnv(string name, LexLocation loc);

        partial void CheckGlobalName(string name, LexLocation loc);
        partial void CheckGlobalTypeCtorName(string name, LexLocation loc);
        partial void CheckGlobalValueCtorName(string name, LexLocation loc);
        partial void CheckGlobalFuncName(string name, LexLocation loc);
        
        partial void CheckQName(QName qname);
        partial void CheckQName(string name, QName prefix, LexLocation loc);
        
        #endregion

        public TypeExpr DefiningCtorAsType()
        {
            return _definingCtor.AsTyExpr;
        } 
        
        public void StartDefiningCtor(string name, List<string> paramLst, LexLocation lexLocation)
        {
            CheckTypeCtorNameEnv(name, lexLocation);
            if (TypeCtorMap.TryGetValue(name,out var ty))
                throw new ADTDuplicatedException(lexLocation,ty);
            _definingCtor = TypeCtorSig.Create(this,name, paramLst,lexLocation);
            CheckTypeCtorKind(_definingCtor);
        }

        public void StopDefiningCtor(string name, ADType tydef)
        {
            //TODO add module name to ADT
            tydef.SetValCtor(_definingCtor.ValCtorNames, _definingCtor.ValCtorTys,_definingCtor.Kind);
            TypeCtorMap.Add(name,tydef);
            _definingCtor = null;
            _definingCtorEnv.Clear();
        }

        public void PrintTypeCtorDef(TextWriter writer)
        {
            writer.WriteLine($"{nameof(TypeCtorMap)} size: {TypeCtorMap.Count}");
            foreach (var pair in TypeCtorMap)
            {
                var adt = pair.Value;
                writer.WriteLine($"type constructor {pair.Key}, type:{adt.GetType()},kind:{PrettyPrinter.Print(adt.Kind)}");
                for (var i = 0; i < adt.CtorNmLst.Length; i++)
                {
                    writer.Write($"  {adt.CtorNmLst[i]} :: ");
                    writer.WriteLine($"{PrettyPrinter.Print(adt.CtorTyLst[i])}");
                }
                if (adt.CtorNmLst.Length == 0)
                    writer.WriteLine("  no value constructor");
            }
        }

        public void SetModuleName(QName qname)
        {
            foreach (var nm in qname.ToList)
            {
                NameSpaceStack.Push(nm);
            }
            _moduleName = qname.ToString();
            _moduleQName = qname;
        }

        public void AddDefiningValCtor(SimpleName valCtorName, ConstructorTypeDecl sig)
        {
            AddDefiningValCtor(valCtorName, sig.ValueTypeSig);
        }
        
        public void AddDefiningValCtor(SimpleName valCtorName, TypeExpr final)
        {
            var loc = valCtorName._lexLocation;
            var name = valCtorName.ToString();
            if (ValueCtorMap.TryGetValue(name,out var ty))
                throw new ValueCtorDuplicatedException(ty._lexLocation,loc);
            _definingCtor.CheckAddValCtor(name, loc, final);
            CheckValCtorType(valCtorName,final);
        }

        private int autonameCount = 0;

        public string AutoGenerateNames()
        {
            autonameCount++;
            return "$auto_" +autonameCount;
        }
    }

    public class TypeCtorSig
    {
        public string Name;
        private string _qname;

        public string[] ParamList;
        public LexLocation Loc;

        public TypeExpr Kind;
        
        private TypeExpr _definingTypeAsTyExpr;
        public TypeExpr AsTyExpr => _definingTypeAsTyExpr;

        private readonly Dictionary<string, TypeExpr> _valCtorMap=new Dictionary<string, TypeExpr>();
        public string[] ValCtorNames => _valCtorMap.Keys.ToArray();
        public TypeExpr[] ValCtorTys => _valCtorMap.Values.ToArray();

        private TypeCtorSig(ParsingContext ctx, string name, string[] pList, LexLocation lexLocation)
        {
            Name = name;
            ParamList = pList;
            Loc = lexLocation;
            _definingTypeAsTyExpr = TyQName.Create(Name, Loc, ctx);
            if (ParamList.Length > 0)
            {
                ListTypeExpr pLst = new ListTypeExpr();
                foreach (var pNm in ParamList)
                {
                    //TODO improve location of parameter
                    pLst.AddLast(TyQName.Create(pNm,Loc, ctx));
                }
                _definingTypeAsTyExpr = new TyApp(_definingTypeAsTyExpr,pLst,Loc,ctx);
            }
        }

        //TODO possible optimization, at most one type is defining!
        public static TypeCtorSig Create(ParsingContext ctx,string name, List<string> paramLst, LexLocation lexLocation)
        {
            return new TypeCtorSig(ctx,
               name,
                paramLst.ToArray(),
                lexLocation
            );
        }

        public void CheckAddValCtor(string valCtorName, LexLocation loc, TypeExpr ty)
        {
            if (_valCtorMap.TryGetValue(valCtorName,out var other))
                throw new ValueCtorDuplicatedException(other._lexLocation,loc);
            _valCtorMap.Add(valCtorName,ty);
        }
    }

    public partial class NameIdent
    {
        public override string ToString()
        {
            return ident_;
        }
        
        public static NameIdent Create(string name, LexLocation loc, ParsingContext ctx)
        {
            return new NameIdent(name, loc, ctx);
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
            foreach (var str in ListIdent_)
            {
                b.Append(str).Append(".");
            }
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
            var listIdent = new ListIdent();
            listIdent.AddFirst(name);
            return new TyQName(new QNameList(listIdent, loc,ctx),loc,ctx);
        }
    }

    public partial class TyArr
    {
        public static TypeExpr Create(TypeExpr p, TypeExpr r,ParsingContext ctx)
        {
            //TODO merge location info
            return new TyArr(p, r, p._lexLocation, ctx);
        }
    }

    public partial class TyNamedHead
    {
        public static TyNamedHead Create(SimpleName name, TypeExpr p, ParsingContext ctx)
        {
            return new TyNamedHead(name, p, name._lexLocation, ctx);
        }

        public override TypeExpr Type => typeexpr_;
        public override SimpleName Name => simplename_;
    }

    public partial class TyNamedArr
    {
        public static TypeExpr Create(TyNamedHead head, TypeExpr r, ParsingContext ctx)
        {
            return new TyNamedArr(head, r, head._lexLocation, ctx);
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
        internal string[] CtorNmLst;
        internal TypeExpr[] CtorTyLst;
        internal TypeExpr Kind;

        public void SetValCtor(string[] nmLst, TypeExpr[] tyLst, TypeExpr kind)
        {
            CtorNmLst = nmLst;
            CtorTyLst = tyLst;
            Kind = kind;
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

    public abstract partial class NamedParamDecl
    {
        protected TyNamedHead ParamType;
        public TyNamedHead AsHead => ParamType;
    }

    public partial class FullTyDecl
    {
        partial void ScanPass(ParsingContext ctx)
        {
            ParamType = TyNamedHead.Create(simplename_,typeexpr_,ctx);
        }
    }

    public partial class ParamTyDecl
    {
        partial void ScanPass(ParsingContext ctx)
        {
            ParamType = TyNamedHead.Create(simplename_, ParamTypeDecl_.AsCurriedType(ctx), ctx);
        }
    }
    
    public partial class RecordType
    {
        partial void SemanticPass(ParsingContext ctx);
        
        partial void ScanPass(ParsingContext ctx)
        {
            SemanticPass(ctx);
            var final = ctx.DefiningCtorAsType();
            for (var i = listnamedparamdecl_.Count-1; i >=0; --i)
            {
                var namedParam = listnamedparamdecl_[i];
                final = TyNamedArr.Create(namedParam.AsHead, final, ctx);
            }
            ctx.AddDefiningValCtor(simplename_,final);
            
            ctx.StopDefiningCtor(indtyctorpre_.IndTypeParam.TyCtorDecl_.Name, this);
        }
    }
    
    public partial class CoType
    {
        partial void SemanticPass(ParsingContext ctx);
        
        internal SimpleType.Absyn.NamedParamDecl[] _lst;

        partial void ScanPass(ParsingContext ctx)
        {
            SemanticPass(ctx);
            var final = ctx.DefiningCtorAsType();
            for (var i = listnamedparamdecl_.Count-1; i >=0; --i)
            {
                var namedParam = listnamedparamdecl_[i];
                final = TyNamedArr.Create(namedParam.AsHead, final, ctx);
            }

            var autoNamed = NameIdent.Create(GenerateConstructorName(), coindtyctorpre_._lexLocation, ctx);
            ctx.AddDefiningValCtor(autoNamed,final);
            
            ctx.StopDefiningCtor(coindtyctorpre_.CoIndTypeParam.TyCtorDecl_.Name, this);
        }

        internal string GenerateConstructorName()
        {
            var tyCtorDecl = coindtyctorpre_.CoIndTypeParam.TyCtorDecl_;
            return $"@{tyCtorDecl.Name}${tyCtorDecl.ParamList.Count}";
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
            ValSig = ParamTypeDecl_.AttachFinalType(ctx);
        }
    }
    
    public partial class ParamTypeDecl
    {
        //TODO ParamTypeDecl (LastParamTy/ConsParamTy) curried form type expr as value constructor parameter
        //TODO need extra form for projection type!
        
        private class AddFinalTyVisitor:Visitor<TypeExpr,ParsingContext>
        {
            public static readonly AddFinalTyVisitor Instance = new AddFinalTyVisitor();
            public TypeExpr Visit(LastParamTy p, ParsingContext ctx)
            {
                var ty = new TyArr(p.TypeExpr_,ctx.DefiningCtorAsType(),p._lexLocation,ctx);
                return ty;
            }

            public TypeExpr Visit(ConsParamTy p, ParsingContext ctx)
            {
                var final = p.ParamTypeDecl_.Accept(this, ctx);
                var ty = TyArr.Create(p.TypeExpr_,final,ctx);
                return ty;
            }
        }

        public TypeExpr AttachFinalType(ParsingContext ctx) => Accept(AddFinalTyVisitor.Instance, ctx);
        
        private class AsCurriedTyVisitor:Visitor<TypeExpr,ParsingContext>
        {
            public static readonly AsCurriedTyVisitor Instance = new AsCurriedTyVisitor();
            public TypeExpr Visit(LastParamTy p, ParsingContext ctx)
            {
                return p.TypeExpr_;
            }

            public TypeExpr Visit(ConsParamTy p, ParsingContext ctx)
            {
                var final = p.ParamTypeDecl_.Accept(this, ctx);
                var ty = TyArr.Create(p.TypeExpr_,final,ctx);
                return ty;
            }
        }

        public TypeExpr AsCurriedType (ParsingContext ctx) => Accept(AsCurriedTyVisitor.Instance, ctx);
    }
    
    public partial class CtrDecl
    {
        partial void ScanPass(ParsingContext ctx)
        {
            ctx.AddDefiningValCtor(simplename_, constructortypedecl_);
        }
    }
    
}