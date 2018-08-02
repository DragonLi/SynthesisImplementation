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
    
    public class InvalidTypeAbs : InvalidTypeExpr
    {
        public InvalidTypeAbs(TypeExpr tyExpr, TypeExpr kind, TypeExpr expectingKind) : base(tyExpr, kind, expectingKind, "Invalid Function Type")
        {
        }
    }

    public class InvalidTypeApp : InvalidTypeExpr
    {
        public InvalidTypeApp(TypeExpr tyExpr, TypeExpr kind, TypeExpr expectingKind) : base(tyExpr, kind, expectingKind, "Invalid Type Application")
        {
        }
    }

    public class InvalidTypeAppNum : SemanticException
    {
        private TypeExpr _ty;
        private TypeExpr _rKind;
        private int _num;
        
        public InvalidTypeAppNum(TypeExpr ty, TypeExpr kind, int num) : base("Insufficient Numbers of Type Paramters",ty._lexLocation)
        {
            _ty = ty;
            _rKind = kind;
            _num = num;
        }
        
        public override void Print(TextWriter writer)
        {
            base.Print(writer);
            writer.WriteLine($"the kind of {PrettyPrinter.Print(_ty)} is {PrettyPrinter.Print(_rKind)}");
            writer.WriteLine($"expecting {_num} type parameters");
        }
    }

    public class InvalidTypeExpr : SemanticException
    {
        private TypeExpr _ty;
        private TypeExpr _rKind;
        private TypeExpr _eKind;

        public InvalidTypeExpr(TypeExpr tyExpr, TypeExpr kind, TypeExpr expectingKind,string msg) : base(msg,tyExpr._lexLocation)
        {
            _ty = tyExpr;
            _rKind = kind;
            _eKind = expectingKind;
        }

        public override void Print(TextWriter writer)
        {
            base.Print(writer);
            writer.WriteLine($"the kind of {PrettyPrinter.Print(_ty)} is {PrettyPrinter.Print(_rKind)}");
            writer.WriteLine($"expecting: {PrettyPrinter.Print(_eKind)}");
        }
    }
}