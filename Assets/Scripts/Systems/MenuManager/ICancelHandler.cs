public interface ICancelHandler
{
    int CancelPriority { get; }
    bool OnCancel();
}