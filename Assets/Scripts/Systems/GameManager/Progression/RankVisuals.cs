using UnityEngine;

[CreateAssetMenu(menuName = "UI/Rank Visuals")]
public class RankVisuals : ScriptableObject
{
    [SerializeField] private Sprite questionSprite;
    [SerializeField] private Sprite[] rankSprites; // D, C, B, A, S

    public Sprite GetSprite(PlayerRank rank)
    {
        if (rank == PlayerRank.Default) return questionSprite;

        int i = (int)rank - 1;
        if (i >= 0 && i < rankSprites.Length) return rankSprites[i];

        return questionSprite;
    }
}