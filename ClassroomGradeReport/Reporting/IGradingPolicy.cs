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

/// <summary>A weight applied to every assignment whose title contains <see cref="Keyword"/>.</summary>
public record WeightRule(string Keyword, double Weight);

/// <summary>
/// Weights assignments by title keyword (case/accent-insensitive). The first matching rule wins;
/// assignments matching no rule keep weight 1.
/// </summary>
public sealed class KeywordWeightPolicy(IEnumerable<WeightRule> rules) : IGradingPolicy
{
    private readonly IReadOnlyList<WeightRule> _rules = rules.ToList();

    public double WeightFor(Assignment assignment) =>
        _rules.FirstOrDefault(r => TitleMatcher.Contains(assignment.Title, r.Keyword))?.Weight ?? 1.0;
}
