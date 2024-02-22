using TMPro;
using UnityEngine;

namespace PsyGameStud
{
    public class Slot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rewrdText;

        public void Setup(int amount)
        {
            _rewrdText.text = $"{amount}";
        }
    }
}
