using SimpleType.Absyn;

namespace SimpleType
{
    public partial class PrettyPrinter
    {
        static PrettyPrinter()
        {
            checking = new[] {".", "^"};
        }
        static partial void CustomRender(string s)
        {
            Backup();
            buffer.Append(s);
        }

        static partial void PrintExt(TypeExpr p, int _i_)
        {
            if (p is UType)
            {
                if(_i_ > 2) Render(LEFT_PARENTHESIS);
                Render("TYPE");
                if(_i_ > 2) Render(RIGHT_PARENTHESIS);
            }
        }
    }
}