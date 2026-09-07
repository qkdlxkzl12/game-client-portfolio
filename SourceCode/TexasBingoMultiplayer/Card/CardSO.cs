using UnityEngine;

public abstract class CardSO : ScriptableObject
{
    public virtual Sprite GetBackSprite(CardSkinSO cardSkin)
    {
        return cardSkin.GetBackSprite();
    }

    public abstract Sprite GetFrontSprite(CardSkinSO cardSkin);
}
