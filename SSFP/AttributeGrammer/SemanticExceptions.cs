using System;
using System.IO;
using QUT.Gppg;

namespace SimpleType.Absyn
{
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

    public class InvalidTypeForValueCtor : SemanticException
    {
        private TypeExpr _ty;

        public InvalidTypeForValueCtor(SimpleName valCtorName, TypeExpr valTy) : base($"Invalid Type For Value Constructor {valCtorName}", valCtorName._lexLocation)
        {
            _ty = valTy;
        }

        public override void Print(TextWriter writer)
        {
            base.Print(writer);
            writer.WriteLine($"infer type is: {PrettyPrinter.Print(_ty)}");
        }
    }
}