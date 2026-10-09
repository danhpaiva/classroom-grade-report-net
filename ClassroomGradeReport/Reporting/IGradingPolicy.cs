using ClassroomGradeReport.Domain;

namespace ClassroomGradeReport.Reporting;

/// <summary>
/// Extension point for weights/categories. The default policy sums raw points;
/// a future policy can scale each assignment's grade and max points by a weight.
/// </summary>
public interface IGradingPolicy
{
    /// <summary>Multiplier applied to both the grade and the max points of an assignment.</summary>
    double WeightFor(Assignment assignment);
}

public sealed class UnweightedGradingPolicy : IGradingPolicy
{
    public double WeightFor(Assignment assignment) => 1.0;
}
