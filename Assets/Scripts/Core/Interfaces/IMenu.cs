public interface IMenu
{
    string MenuId { get; }
    void Open();
    void Close();
    void RestoreFocus();
}