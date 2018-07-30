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
        private readonly LexLocation _other;

        public ValueCtorDuplicatedException(LexLocation other, LexLocation loc) : base("value constructor overloading is not implemented", loc)
        {
            _other = other;
        }

        public override void Print(TextWriter writer)
        {
            base.Print(writer);
            writer.WriteLine("there is another definition here:");
            writer.WriteLine(_other.HintLine());
        }
    }

    public class InvalidFinalTypeForValueCtor : SemanticException
    {
        private TypeExpr _ty,_expecting;

        public InvalidFinalTypeForValueCtor(SimpleName valCtorName,TypeExpr expecting, TypeExpr valTy) : base($"Invalid Type For Value Constructor {valCtorName}", valCtorName._lexLocation)
        {
            _expecting = expecting;
            _ty = valTy;
        }

        public override void Print(TextWriter writer)
        {
            base.Print(writer);
            writer.WriteLine($"infer type is: {PrettyPrinter.Print(_ty)}");
            writer.WriteLine($"expecting: {PrettyPrinter.Print(_expecting)}");
        }
    }
}