namespace SimpleType.Absyn
{
    public partial class ParsingContext
    {
        public void CheckTypeCtorKind(ParsingContext ctx, TypeCtorSig sig)
        {
            //TODO sig.Name != Int , Double, String
        }

        public void CheckValCtorType(ParsingContext ctx, SimpleName valCtorName, TypeExpr valTy)
        {
            switch (valTy)
            {
                case TyStr tyStr:
                case TyFloat tyFloat:
                case TyInt tyInt:
                    throw new InvalidTypeForValueCtor(valCtorName,valTy);
                case TyQName tyQName:
                    if (!tyQName.IsNamed(_definingCtor.Name,_moduleQName))
                        throw new InvalidTypeForValueCtor(valCtorName,valTy);
                    break;
                case TyApp tyApp:
                    //TODO tyApp.TypeExpr_
                    break;
                case TyArr tyArr:
                    break;
                case TyNamedArr tyNamedArr:
                    break;
            }
        }
    }
}