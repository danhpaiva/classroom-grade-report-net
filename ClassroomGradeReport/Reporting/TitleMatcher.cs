using System.Globalization;

namespace ClassroomGradeReport.Reporting;

/// <summary>Case- and accent-insensitive "contains" used to classify assignments by their title.</summary>
public static class TitleMatcher
{
    private static readonly CompareInfo Compare = new CultureInfo("pt-BR").CompareInfo;

    /// <summary>True when <paramref name="title"/> contains <paramref name="keyword"/>; an empty keyword never matches.</summary>
    public static bool Contains(string title, string keyword) =>
        keyword.Length > 0 &&
        Compare.IndexOf(title, keyword, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
}
