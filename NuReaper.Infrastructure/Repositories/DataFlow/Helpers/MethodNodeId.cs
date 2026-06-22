namespace NuReaper.Infrastructure.Repositories.DataFlow.Helpers
{
    public static class MethodNodeId
    {
        public static string Execute(int packageId, string methodFullName)
            => $"method:{packageId}:{methodFullName}";
    }
}
