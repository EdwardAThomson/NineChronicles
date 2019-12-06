using Nekoyume.Helper;
using Nekoyume.TableData;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class VanillaItemView : MonoBehaviour
    {
        public Image gradeImage;
        public Image iconImage;
        
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }
        
        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void SetData(ItemSheet.Row itemRow)
        {
            if (itemRow is null)
            {
                Clear();
                return;
            }

            var gradeSprite = SpriteHelper.GetItemBackground(itemRow.Grade);
            gradeImage.overrideSprite = gradeSprite;

            var itemSprite = SpriteHelper.GetItemIcon(itemRow.Id);
            if (itemSprite is null)
                throw new FailedToLoadResourceException<Sprite>(itemRow.Id.ToString());

            iconImage.enabled = true;
            iconImage.overrideSprite = itemSprite;
            iconImage.SetNativeSize();
        }

        public virtual void Clear()
        {
            gradeImage.enabled = false;
            iconImage.enabled = false;
        }

        protected virtual void SetDim(bool isDim)
        {
            var alpha = isDim ? .3f : 1f;
            gradeImage.color = GetColor(gradeImage.color, alpha);
            iconImage.color = GetColor(iconImage.color, alpha);
        }
        
        protected Color GetColor(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}