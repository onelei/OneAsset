using UnityEngine;
using UnityEngine.UI;

namespace OneAsset.Samples
{
    public class UISample : MonoBehaviour
    {
        public Image image;

        public void SetIcon(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}