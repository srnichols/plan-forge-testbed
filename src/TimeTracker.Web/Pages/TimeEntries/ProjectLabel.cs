using TimeTracker.Web.Client.Models;

namespace TimeTracker.Web.Pages.TimeEntries;

/// <summary>
/// Builds the display label for a project option, prefixing the owning client's
/// name so users can tell which client a project belongs to.
/// </summary>
internal static class ProjectLabel
{
    private const string Separator = " – ";

    public static string Format(IReadOnlyDictionary<int, string> clientNames, ProjectDto project)
    {
        return clientNames.TryGetValue(project.ClientId, out string? clientName) && !string.IsNullOrWhiteSpace(clientName)
            ? $"{clientName}{Separator}{project.Name}"
            : project.Name;
    }
}
