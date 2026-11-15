using Microsoft.AspNetCore.Mvc;

namespace LibraryApi;

public static class RequestRules
{
    public static string? Validate(params (string Label, string? Value, int MaxLength)[] fields)
    {
        foreach (var (label, value, maxLength) in fields)
        {
            if (string.IsNullOrWhiteSpace(value))
                return $"{label} is required.";

            if (value.Trim().Length > maxLength)
                return $"{label} is too long.";
        }

        return null;
    }
}

public static class ApiProblems
{
    public static ObjectResult Make(ControllerBase controller, int statusCode, string detail) =>
        controller.Problem(title: detail, detail: detail, statusCode: statusCode);
}
