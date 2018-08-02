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

        private static void PrintInternal(SimpleType.Absyn.UType p, int _i_)
        {
            if(_i_ > 2) Render(LEFT_PARENTHESIS);
            Render("TYPE");
            if(_i_ > 2) Render(RIGHT_PARENTHESIS);
        }
    }
}