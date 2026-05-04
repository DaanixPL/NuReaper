namespace NuReaper.Domain.Enums
{
    public enum DataFlowEdgeType
    {
        Calls,
        Targets,
        FlowInto,
        Returns,
        ParameterBinding,
        WritesField,
        ReadsField,
        Contains,
        Assigns
    }
}