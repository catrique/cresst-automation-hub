namespace AutomationApp.Services.Soc.Organizing;

public interface IAsoOrganizerService
{
    Task OrganizeAsync(string folderPath);
}