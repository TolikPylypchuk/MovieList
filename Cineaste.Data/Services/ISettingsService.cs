namespace Cineaste.Data.Services
{
    public interface ISettingsService
    {
        Settings GetSettings();
        void UpdateSettings(Settings settings);
    }
}
