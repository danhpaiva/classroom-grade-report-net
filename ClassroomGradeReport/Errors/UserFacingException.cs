namespace ClassroomGradeReport.Errors;

/// <summary>An error whose message is safe and meant to be shown to the user as-is (pt-BR).</summary>
public class UserFacingException(string message, Exception? inner = null) : Exception(message, inner);
