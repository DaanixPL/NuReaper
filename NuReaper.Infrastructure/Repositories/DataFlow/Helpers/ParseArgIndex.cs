namespace NuReaper.Infrastructure.Repositories.DataFlow.Helpers
{
    public static class ParseArgIndex
    {
        public static int Execute(string argName)
        {
            // "arg_0" -> 0, "arg_12" -> 12
            var span = argName.AsSpan("arg_".Length);
            return int.TryParse(span, out int idx) ? idx : -1;
        }
    }
}
