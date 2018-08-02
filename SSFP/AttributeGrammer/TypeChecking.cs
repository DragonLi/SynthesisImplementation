using System;
using System.Collections.Generic;
using SimpleType.Absyn;

namespace SimpleType.Absyn
{
    public partial class ParsingContext
    {
        private Dictionary<string, TypeExpr> _definingCtorEnv = new Dictionary<string,TypeExpr>();

        private void CheckTypeCtorKind(TypeCtorSig sig)
        {
            //TODO _definingCtor.Name != Int , Double, String
            
            TypeExpr ctorKind = UType.Instance;
            for (var i = 0; i < _definingCtor.ParamList.Length; i++)
            {
                _definingCtorEnv.Add(_definingCtor.ParamList[i], UType.Instance);
                ctorKind = TyArr.Create(UType.Instance, ctorKind,this);
            }

            _definingCtorEnv.Add(_definingCtor.Name,ctorKind);
            _definingCtor.Kind = ctorKind;
        }

        private TypeExpr DefiningCtorAsShape()
        {
            var bTy = TyQName.Create(_definingCtor.Name, _definingCtor.Loc, this);
            if (_definingCtor.ParamList.Length <= 0) return bTy;
            var pLst = new ListTypeExpr();
            foreach (var pNm in _definingCtor.ParamList)
                pLst.AddLast(new MetaTypVar(this));
            bTy = new TyApp(bTy,pLst,_definingCtor.Loc,this);
            return bTy;
        }

        private void CheckValCtorType(SimpleName valCtorName, TypeExpr valTy)
        {
            CheckGlobalName(valCtorName.ToString(), valCtorName._lexLocation);
            var kind = TypeofTypExpr(_definingCtorEnv, valTy);
            if (kind == null)
                throw new SemanticException("Invalid Type of Value Constructor",valCtorName._lexLocation);
            var final = valTy.FinalType;
            //var errType = final.Match(DefiningCtorAsShape(),this);
            //no gadt so simplify into the following
            var expectingCtorTy = DefiningCtorAsType();
            var errType = final.Match(expectingCtorTy, this);
            if (errType != null)
                throw new InvalidFinalTypeForValueCtor(valCtorName, expectingCtorTy, final);
        }

        private TypeExpr TypeofTypExpr(Dictionary<string,TypeExpr> env,TypeExpr ty)
        {
            if (ty == null)
                return null;
            switch (ty)
            {
                case MetaTypVar metaTypVar:
                    return TypeofTypExpr(env,metaTypVar.Body);
                case UType uType:
                case TyFloat tyFloat:
                case TyStr tyStr:
                case TyInt tyInt:
                    return UType.Instance;
                case TyQName tyQName:
                    return MapNameToType(env, tyQName);
                case TyApp tyApp:
                {
                    var appTy = TypeofTypExpr(env,tyApp.TypeExpr_);
                    var lastTy = appTy;
                    var n = tyApp.ListTypeExpr_.Count;
                    for (var i = 0; i < n; i++)
                    {
                        var spine = lastTy as TyArr;
                        if (spine == null)
                            throw new InvalidTypeAppNum(tyApp.TypeExpr_, appTy,n);
                        var PA = TypeofTypExpr(env, tyApp.ListTypeExpr_[i]);
                        var errType = spine.TypeExpr_1.Match(PA,this);
                        if (errType != null)
                            throw new InvalidTypeApp(tyApp.ListTypeExpr_[i],PA,spine.TypeExpr_1);
                        
                        lastTy = spine.TypeExpr_2;
                    }
                    return lastTy;
                }
                case TyArr tyArr:
                {
                    var A = TypeofTypExpr(env, tyArr.TypeExpr_1);
                    if (!(A is UType))
                        throw new InvalidTypeAbs(tyArr.TypeExpr_1,A,UType.Instance);
                    var B = TypeofTypExpr(env, tyArr.TypeExpr_2);
                    if (!(B is UType))
                        throw new InvalidTypeAbs(tyArr.TypeExpr_2,B,UType.Instance);
                    return UType.Instance;
                }
                case TyNamedArr tyNamedArr:
                {
                    var A = TypeofTypExpr(env, tyNamedArr.TypeArrHead_.Type);
                    if (!(A is UType))
                        throw new InvalidTypeAbs(tyNamedArr.TypeArrHead_.Type,A,UType.Instance);
                    var B = TypeofTypExpr(env, tyNamedArr.TypeExpr_);
                    if (!(B is UType))
                        throw new InvalidTypeAbs(tyNamedArr.TypeExpr_,B,UType.Instance);
                    return UType.Instance;
                }
            }
            return null;
        }

        private TypeExpr MapNameToType(Dictionary<string,TypeExpr> env,TyQName nm)
        {
            var name = nm.QName_.ToString();
            env.TryGetValue(name, out var result);
            result = result ?? (result = FindTypeofCtorName(name));
            if (result == null)
                throw new SemanticException($"Type Constructor {name} not found",nm._lexLocation);
            return result;
        }

        private TypeExpr FindTypeofCtorName(string name)
        {
            if (!TypeCtorMap.TryGetValue(name, out var tyCtor))
            {
                return FindTypeofCtorNameFromImports(name);
            }
            return tyCtor.Kind;
        }

        private TypeExpr FindTypeofCtorNameFromImports(string name)
        {
            return null;
        }
    }

    //universal type, Type :: Type
    public partial class UType : TypeExpr
    {
        private UType()
        {
            _finalTy = this;
        }
        
        public static UType Instance = new UType();
        public override R Accept<R, A>(Visitor<R, A> v, A arg)
        {
            return default(R);
        }
        /*
         *

      if(p is SimpleType.Absyn.UType)
      {
        if(_i_ > 2) Render(LEFT_PARENTHESIS);
        Render("TYPE");
        if(_i_ > 2) Render(RIGHT_PARENTHESIS);
      }else

         * 
         */
    }
    
    public partial class MetaTypVar : TypeExpr
    {
        private string _metaVarName;
        private TypeExpr _result;

        public MetaTypVar(ParsingContext ctx)
        {
            _metaVarName = ctx.AutoGenerateNames();
        }
        
        public MetaTypVar(string metaVarName)
        {
            _metaVarName = metaVarName;
        }

        public override R Accept<R, A>(Visitor<R, A> v, A arg)
        {
            return _result.Accept(v, arg);
        }
        
        public TypeExpr Body
        {
            get => _result;
            set => _result = value;
        }

        public string Name => _metaVarName;
    }

    public abstract partial class TypeExpr
    {
        protected TypeExpr _finalTy;
        public TypeExpr FinalType => _finalTy;

        public partial interface Visitor<R, A>
        {
            R Visit(UType p, A arg);
        }

        /*public MetaTypVar[] Match(TypeExpr shape,ParsingContext ctx)
        {
        }*/
        
        private class MatchMutateVisitor:Visitor<TypeExpr,Tuple<TypeExpr,ParsingContext>>{
            public TypeExpr Visit(UType p, Tuple<TypeExpr, ParsingContext> arg)
            {
                var pat = arg.Item1;
                switch (pat)
                {
                    case MetaTypVar meta:
                        meta.Body = p;
                        return null;
                    case UType uTy:
                        return null;
                }
                return p;
            }

            public TypeExpr Visit(TyInt p, Tuple<TypeExpr, ParsingContext> arg)
            {
                var pat = arg.Item1;
                switch (pat)
                {
                    case MetaTypVar meta:
                        meta.Body = p;
                        return null;
                    case TyInt _:
                        return null;
                }
                return p;
            }

            public TypeExpr Visit(TyFloat p, Tuple<TypeExpr, ParsingContext> arg)
            {
                var pat = arg.Item1;
                switch (pat)
                {
                    case MetaTypVar meta:
                        meta.Body = p;
                        return null;
                    case TyFloat _:
                        return null;
                }
                return p;
            }

            public TypeExpr Visit(TyStr p, Tuple<TypeExpr, ParsingContext> arg)
            {
                var pat = arg.Item1;
                switch (pat)
                {
                    case MetaTypVar meta:
                        meta.Body = p;
                        return null;
                    case TyStr _:
                        return null;
                }
                return p;
            }

            public TypeExpr Visit(TyQName p, Tuple<TypeExpr, ParsingContext> arg)
            {
                var pat = arg.Item1;
                switch (pat)
                {
                    case MetaTypVar meta:
                        meta.Body = p;
                        return null;
                    case TyQName other when other.IsNamed(p,arg.Item2):
                        return null;
                }
                return p;
            }

            public TypeExpr Visit(TyApp p, Tuple<TypeExpr, ParsingContext> arg)
            {
                var pat = arg.Item1;
                TypeExpr lastErr = p;
                switch (pat)
                {
                    case MetaTypVar meta:
                        meta.Body = p;
                        return null;
                    case TyApp tyApp:
                    {
                        var otherLst = tyApp.ListTypeExpr_;
                        if (otherLst.Count != p.ListTypeExpr_.Count)
                            break;
                        var ctx = arg.Item2;
                        lastErr = p.TypeExpr_.Accept(this, Tuple.Create(tyApp.TypeExpr_, ctx));
                        if (lastErr != null)
                            break;
                        for (var i = 0; i < otherLst.Count; i++)
                        {
                            lastErr = p.ListTypeExpr_[i].Accept(this, Tuple.Create(otherLst[i], ctx));
                            if (lastErr != null)
                                break;
                        }
                    }
                        break;
                }

                return lastErr;
            }

            public TypeExpr Visit(TyArr p, Tuple<TypeExpr, ParsingContext> arg)
            {
                var pat = arg.Item1;
                TypeExpr lastErr = p;
                switch (pat)
                {
                    case MetaTypVar meta:
                        meta.Body = p;
                        return null;
                    case TyArr tyAbs:
                    {
                        var ctx = arg.Item2;
                        lastErr = p.TypeExpr_1.Accept(this, Tuple.Create(tyAbs.TypeExpr_1, ctx));
                        if (lastErr != null)
                            break;
                        lastErr = p.TypeExpr_2.Accept(this, Tuple.Create(tyAbs.TypeExpr_2, ctx));
                    }
                        break;
                }
                return lastErr;
            }

            public TypeExpr Visit(TyNamedArr p, Tuple<TypeExpr, ParsingContext> arg)
            {
                var pat = arg.Item1;
                TypeExpr lastErr = p;
                switch (pat)
                {
                    case MetaTypVar meta:
                        meta.Body = p;
                        return null;
                    case TyNamedArr tyAbs:
                    {
                        var ctx = arg.Item2;
                        lastErr = p.TypeArrHead_.Type.Accept(this,Tuple.Create(tyAbs.TypeArrHead_.Type,arg.Item2));
                        if (lastErr != null)
                            break;
                        lastErr = p.TypeExpr_.Accept(this, Tuple.Create(tyAbs.TypeExpr_, ctx));
                    }
                        break;
                }
                return lastErr;
            }

            public static readonly Visitor<TypeExpr, Tuple<TypeExpr, ParsingContext>> Instance = new MatchMutateVisitor();
        }
        
        public TypeExpr Match(TypeExpr shape, ParsingContext ctx)
        {
            return Accept(MatchMutateVisitor.Instance, Tuple.Create(shape, ctx));
        }
    }

    public abstract partial class TypeArrHead
    {
        public abstract TypeExpr Type { get; }
        public abstract SimpleName Name { get; }
    }
    
    public partial class TyInt
    {
        partial void ScanPass(ParsingContext ctx)
        {
            _finalTy = this;
        }
    }

    public partial class TyFloat
    {
        partial void ScanPass(ParsingContext ctx)
        {
            _finalTy = this;
        }
    }

    public partial class TyStr
    {
        partial void ScanPass(ParsingContext ctx)
        {
            _finalTy = this;
        }
    }
    
    public partial class TyQName
    {
        partial void ScanPass(ParsingContext ctx)
        {
            _finalTy = this;
        }

        public bool IsNamed(string localName, QName moduleQName)
        {
            var thizList = qname_.ToList;
            var thizListCount = thizList.Count;
            if (thizListCount == 1)
                return thizList[0] == localName;
            var prefix = moduleQName.ToList;
            if (thizListCount != prefix.Count + 1)
                return false;
            if (thizList[thizListCount - 1] != localName)
                return false;
            for (var i = 0; i < prefix.Count; i++)
            {
                if (prefix[i] != thizList[i])
                    return false;
            }
            return true;
        }

        public bool IsNamed(TyQName localName, ParsingContext moduleQName)
        {
            //TODO
            return true;
        }
    }

    public partial class TyApp
    {
        partial void ScanPass(ParsingContext ctx)
        {
            _finalTy = this;
        }
    }
    
    public partial class TyArr
    {
        partial void ScanPass(ParsingContext ctx)
        {
            _finalTy = typeexpr_2.FinalType;
        }
    }
    
    public partial class TyNamedArr
    {
        partial void ScanPass(ParsingContext ctx)
        {
            _finalTy = typeexpr_.FinalType;
        }
    }
}

namespace SimpleType.VisitSkeleton
{
    public abstract partial class AbstractTypeExprVisitor<R, A>
    {
        public virtual R Visit(UType p, A arg)
        {
            return default(R);
        }
    }
}

