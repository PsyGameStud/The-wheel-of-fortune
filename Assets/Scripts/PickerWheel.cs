using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine.UI;

namespace PsyGameStud
{
    public class PickerWheel : MonoBehaviour 
    {
        [Space]
        [SerializeField] private Button _spintButton;
        [SerializeField] private Image _imageButton;
        [SerializeField] private TextMeshProUGUI _spintText;
        [SerializeField] private Sprite _enableSprite;
        [SerializeField] private Sprite _disableSprite;

        [Space]
        [SerializeField] private Transform _pickerWheelTransform;
        [SerializeField] private Transform _wheelCircle;
        [SerializeField] private Slot _slotPrefab;
        [SerializeField] private Transform _wheelPiecesParent;

        [Space]
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private Image _rewardImage;
        [SerializeField] private List<Sprite> _rewards;
        private Sprite _lastReward;

        [Space] 
        [SerializeField] private FlyingElement _flyingPrefab;
        [SerializeField] private Transform _parent;
        private List<FlyingElement> _flyingElements = new List<FlyingElement>();

        [Space]
        [Header ("Sounds :")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _tickAudioClip;
        [SerializeField] [Range (0f, 1f)] private float _volume = .5f;
        [SerializeField] [Range (-3f, 3f)] private float _pitch = 1f;

        [Space]
        [Header ("Picker wheel settings :")]
        [Range (1, 20)] public int _spinDuration = 5;

        private List<WheelPiece> _wheelPieces = new List<WheelPiece>();
        private List<Slot> _slots = new List<Slot>();

        private bool _isSpinning = false;

        private Vector2 _pieceMinSize = new Vector2 (81f, 146f);
        private Vector2 _pieceMaxSize = new Vector2 (144f, 213f);
        private int _piecesMin = 2;
        private int _piecesMax = 12;
        private int _countCicles = 10;

        private float _pieceAngle;
        private float _halfPieceAngle;
        private float _halfPieceAngleWithPaddings;

        private double _accumulatedWeight;
        private System.Random _rand = new System.Random();

        private List<int> _nonZeroChancesIndices = new List<int>();

        private void Start() 
        {
            for (int i = 0; i < 12; i++)
            {
                var piece = new WheelPiece();
                _wheelPieces.Add(piece);
            }

            for (int i = 0; i < 10; i++)
            {
                _flyingElements.Add(CreateElement());
            }

            _pieceAngle = 360 / _wheelPieces.Count;
            _halfPieceAngle = _pieceAngle / 2f;
            _halfPieceAngleWithPaddings = _halfPieceAngle - (_halfPieceAngle / 4f);

            _rewardImage.sprite = _rewards[UnityEngine.Random.Range(0, _rewards.Count)];
            _lastReward = _rewardImage.sprite;

            _spintButton.onClick.AddListener(Spin);

            Generate();
            CalculateWeightsAndIndices();
            ChangeRewards().Forget();

            //SetupAudio ();
        }

        private FlyingElement CreateElement()
        {
            return Instantiate(_flyingPrefab, _parent);
        }

        private void SetupAudio() 
        {
            _audioSource.clip = _tickAudioClip;
            _audioSource.volume = _volume;
            _audioSource.pitch = _pitch;
        }

        private void Generate()
        {
            RectTransform rt = _slotPrefab.transform.GetChild (0).GetComponent<RectTransform>();
            float pieceWidth = Mathf.Lerp(_pieceMinSize.x, _pieceMaxSize.x, 1f - Mathf.InverseLerp(_piecesMin, _piecesMax, _wheelPieces.Count));
            float pieceHeight = Mathf.Lerp(_pieceMinSize.y, _pieceMaxSize.y, 1f - Mathf.InverseLerp(_piecesMin, _piecesMax, _wheelPieces.Count));
            rt.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, pieceWidth);
            rt.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, pieceHeight);

            for (int i = 0; i < _wheelPieces.Count; i++)
            {
                DrawPiece (i);
            }
        }

        private void DrawPiece(int index) 
        {
            WheelPiece piece = _wheelPieces[index];
            var newSLot = InstantiatePiece();
            newSLot.Setup(piece.Amount);

            _slots.Add(newSLot);

            newSLot.transform.RotateAround(_wheelPiecesParent.position, Vector3.back, _pieceAngle * index);
        }

        private void GenerateRandomValues()
        {
            HashSet<int> usedNumbers = new HashSet<int>();

            for (int i = 0; i < _wheelPieces.Count; i++)
            {
                int randomNumber = GetUniqueRandomNumber(usedNumbers);
                _wheelPieces[i].Amount  = randomNumber;
                _wheelPieces[i].Chance = UnityEngine.Random.Range(1, 101);
                _slots[i].Setup(randomNumber);
            }
        }

        private int GetUniqueRandomNumber(HashSet<int> usedNumbers)
        {
            int randomNumber;

            do
            {
                randomNumber = UnityEngine.Random.Range(1, 21) * 5;
            } while (usedNumbers.Contains(randomNumber));

            usedNumbers.Add(randomNumber);
            return randomNumber;
        }

        private Slot InstantiatePiece() 
        {
            return Instantiate(_slotPrefab, _wheelPiecesParent.position, Quaternion.identity, _wheelPiecesParent);
        }

        private async UniTask SpinProcess()
        {
            _spintButton.enabled = false;
            _imageButton.sprite = _disableSprite;

            int index = GetRandomPieceIndex();
            WheelPiece piece = _wheelPieces[index];

            Debug.Log($"Reward: {piece.Amount}");

            float angle = -(_pieceAngle * index);

            float rightOffset = (angle - _halfPieceAngleWithPaddings) % 360;
            float leftOffset = (angle + _halfPieceAngleWithPaddings) % 360;

            float randomAngle = UnityEngine.Random.Range(leftOffset, rightOffset);

            Vector3 targetRotation = Vector3.back * (randomAngle + 2 * 360 * _spinDuration);

            float prevAngle, currentAngle;
            prevAngle = currentAngle = _wheelCircle.eulerAngles.z;

            bool isIndicatorOnTheLine = false;

            await _wheelCircle
            .DORotate(targetRotation, _spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad)
            .OnUpdate(() =>
            {
                float diff = Mathf.Abs(prevAngle - currentAngle);
                if (diff >= _halfPieceAngle)
                {
                    if (isIndicatorOnTheLine)
                    {
                        _audioSource.PlayOneShot(_audioSource.clip);
                    }

                    prevAngle = currentAngle;
                    isIndicatorOnTheLine = !isIndicatorOnTheLine;
                }
                currentAngle = _wheelCircle.eulerAngles.z;
            });

            _isSpinning = false;

            await GetReward(piece.Amount);

            await ChangeRewards();
        }

        private async UniTask GetReward(int reward)
        {
            _rewardText.gameObject.SetActive(true);
            _rewardText.text = $"{0}";
            _rewardImage.gameObject.SetActive(false);

            foreach (var item in _flyingElements)
            {
                item.Setup(_rewardImage.sprite);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(1f));

            foreach (var item in _flyingElements)
            {
                item.FlyProcess(UnityEngine.Random.Range(0.5f, 2f)).Forget();
            }

            await DOVirtual.Int(0, reward, 2f, progress =>
            {
                _rewardText.text = $"{progress}";
            }).SetAutoKill();

            await UniTask.Delay(TimeSpan.FromSeconds(1f));

            _rewardImage.gameObject.SetActive(true);
            _rewardText.gameObject.SetActive(false);
        }

        private async UniTask ChangeRewards()
        {
            var time = 10;
            _imageButton.sprite = _disableSprite;
            _spintButton.enabled = false;

            for (int i = 0; i < _countCicles; i++)
            {
                time -= 1;
                _rewardImage.sprite = _rewards[UnityEngine.Random.Range(0, _rewards.Count)];
                _spintText.text = $"{time}";
                GenerateRandomValues();
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
            }

            if (_rewardImage.sprite == _lastReward)
            {
                var idLastReward = _rewards.IndexOf(_lastReward);
                idLastReward += 1;

                if (idLastReward >= _rewards.Count)
                {
                    idLastReward = 0;
                }

                _rewardImage.sprite = _rewards[idLastReward];
                _lastReward = _rewardImage.sprite;
            }

            _imageButton.sprite = _enableSprite;
            _spintButton.enabled = true;
            _spintText.text = "ИСПЫТАТЬ УДАЧУ";
        }

        private async void Spin() 
        {
            if (!_isSpinning) 
            {
                _isSpinning = true;

                await SpinProcess();
            }
        }

        private int GetRandomPieceIndex() 
        {
            double r = _rand.NextDouble() * _accumulatedWeight;

            for (int i = 0; i < _wheelPieces.Count; i++)
                if (_wheelPieces [i].Weight >= r)
                    return i;

            return 0;
        }

        private void CalculateWeightsAndIndices() 
        {
            for (int i = 0; i < _wheelPieces.Count; i++) 
            {
                WheelPiece piece = _wheelPieces[i];

                //add weights:
                _accumulatedWeight += piece.Chance;
                piece.Weight = _accumulatedWeight;

                //add index :
                piece.Index = i;

                //save non zero chance indices:
                if (piece.Chance > 0)
                    _nonZeroChancesIndices.Add(i);
            }
        }
    }
}