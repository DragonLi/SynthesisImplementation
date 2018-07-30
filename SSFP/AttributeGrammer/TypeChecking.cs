using System;

namespace SimpleType.Absyn
{
    public partial class ParsingContext
    {
        public void CheckTypeCtorKind(TypeCtorSig sig)
        {
            //TODO sig.Name != Int , Double, String
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

        public void CheckValCtorType(SimpleName valCtorName, TypeExpr valTy)
        {
            CheckGlobalName(valCtorName.ToString(), valCtorName._lexLocation);
            CheckType(valTy);
            var final = valTy.FinalType;
            //var errType = final.Match(DefiningCtorAsShape(),this);
            //no gadt so simplify into the following
            var expectingCtorTy = DefiningCtorAsType();
            var errType = final.Match(expectingCtorTy, this);
            if (errType != null)
                throw new InvalidFinalTypeForValueCtor(valCtorName, expectingCtorTy, final);
        }

        public void CheckType(TypeExpr ty)
        {
            //TODO
        }
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

        /*public MetaTypVar[] Match(TypeExpr shape,ParsingContext ctx)
        {
        }*/
        
        private class MatchMutateVisitor:Visitor<TypeExpr,Tuple<TypeExpr,ParsingContext>>{
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