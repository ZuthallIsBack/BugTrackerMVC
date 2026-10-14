namespace BugTrackerMVC.Models;

public static class TicketValidationRules
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 120;
    public const int DescriptionMinLength = 20;
    public const int DescriptionMaxLength = 4000;
}
