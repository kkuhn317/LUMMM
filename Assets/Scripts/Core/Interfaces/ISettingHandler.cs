public interface ISettingHandler
{
    SettingType SettingType { get; }
    void ApplyFromSaved();
    void Apply(int index);
    void Apply(bool value); // used for toggles
    void Save();
    void RefreshUI();
}