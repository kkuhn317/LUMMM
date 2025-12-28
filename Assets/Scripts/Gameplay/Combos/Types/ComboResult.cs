public struct ComboResult
{
    public readonly RewardType rewardType; // Score, OneUp, Coin, Powerup, Custom
    public readonly PopupID popupID; // Which popup sprite to show
    public readonly int amount; // Score points, coin count, etc.

    public ComboResult(RewardType rewardType, PopupID popupID, int amount = 0)
    {
        this.rewardType = rewardType;
        this.popupID = popupID;
        this.amount = amount;
    }
}