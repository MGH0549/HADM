namespace Hadm.Application.Common;

public static class TextRules
{
    public static string? CleanNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string RequireUser(ICurrentUserService currentUser) =>
        currentUser.UserId ?? throw new UnauthorizedAppException("Missing user.");
}
