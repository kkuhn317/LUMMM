public interface ISettingsProvider
{
    string[] GetOptions(SettingType type);
    int GetSavedIndex(SettingType type);
    void ApplySetting(SettingType type, int index);
    void SaveSetting(SettingType type, int index);
}