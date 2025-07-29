internal static class StringExtensions
{
    extension(string str)
    {
        public string ToPascalCase()
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }
    }
}
