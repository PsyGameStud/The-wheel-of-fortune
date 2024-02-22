using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PsyGameStud
{
    public class FlyingElement : MonoBehaviour
    {
        [SerializeField] private Image _image;

        private RectTransform _myTransform;

        public void Setup(Sprite view)
        {
            _image.sprite = view;

            _myTransform = transform as RectTransform;

            _myTransform.anchoredPosition = new Vector3(Random.Range(-200f, 200f), Random.Range(200f, 400f));
            _myTransform.localScale = Vector3.zero;

            _myTransform.DOScale(Random.Range(0.5f, 1f), 1f);
        }

        public async UniTask FlyProcess()
        {
            var sequence = DOTween.Sequence();

            await sequence.Append(_myTransform.DOScale(0f, 0.5f))
                .Join(_myTransform.DOAnchorPos(Vector2.zero, 0.5f));
        }
    }
}
