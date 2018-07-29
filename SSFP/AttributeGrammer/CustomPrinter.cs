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
    }
}