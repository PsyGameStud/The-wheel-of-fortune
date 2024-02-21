using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;
using PsyGameStud.Gameplay;

namespace PsyGameStud.PickerWheelUI 
{
    public class PickerWheel : MonoBehaviour 
    {
        [Space]
        [SerializeField] private Transform _pickerWheelTransform;
        [SerializeField] private Transform _wheelCircle;
        [SerializeField] private GameObject _wheelPiecePrefab;
        [SerializeField] private Transform _wheelPiecesParent;

        [Space]
        [Header ("Sounds :")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _tickAudioClip;
        [SerializeField] [Range (0f, 1f)] private float _volume = .5f;
        [SerializeField] [Range (-3f, 3f)] private float _pitch = 1f;

        [Space]
        [Header ("Picker wheel settings :")]
        [Range (1, 20)] public int _spinDuration = 8;

        [Space]
        [Header ("Picker wheel pieces :")]
        public WheelPiece[] _wheelPieces;

        // Events
        private UnityAction _onSpinStartEvent;
        private UnityAction<WheelPiece> _onSpinEndEvent;

        private bool _isSpinning = false;
        public bool IsSpinning { get { return _isSpinning; } }

        private Vector2 _pieceMinSize = new Vector2 (81f, 146f);
        private Vector2 _pieceMaxSize = new Vector2 (144f, 213f);
        private int _piecesMin = 2;
        private int _piecesMax = 12;

        [SerializeField] private float _pieceAngle;
        [SerializeField] private float _halfPieceAngle;
        [SerializeField] private float _halfPieceAngleWithPaddings;

        private double _accumulatedWeight;
        private System.Random _rand = new System.Random();

        private List<int> _nonZeroChancesIndices = new List<int>();

        private void Start() 
        {
            _pieceAngle = 360 / _wheelPieces.Length;
            _halfPieceAngle = _pieceAngle / 2f;
            _halfPieceAngleWithPaddings = _halfPieceAngle - (_halfPieceAngle / 4f);

            Generate();
            CalculateWeightsAndIndices();

            //SetupAudio ();
        }

        private void SetupAudio() 
        {
            _audioSource.clip = _tickAudioClip;
            _audioSource.volume = _volume;
            _audioSource.pitch = _pitch;
        }

        private void Generate()
        {
            _wheelPiecePrefab = InstantiatePiece();

            RectTransform rt = _wheelPiecePrefab.transform.GetChild (0).GetComponent<RectTransform>();
            float pieceWidth = Mathf.Lerp(_pieceMinSize.x, _pieceMaxSize.x, 1f - Mathf.InverseLerp(_piecesMin, _piecesMax, _wheelPieces.Length));
            float pieceHeight = Mathf.Lerp(_pieceMinSize.y, _pieceMaxSize.y, 1f - Mathf.InverseLerp(_piecesMin, _piecesMax, _wheelPieces.Length));
            rt.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, pieceWidth);
            rt.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, pieceHeight);

            for (int i = 0; i < _wheelPieces.Length; i++)
            {
                DrawPiece (i);
            }

            Destroy(_wheelPiecePrefab);
        }

        private void DrawPiece (int index) 
        {
            WheelPiece piece = _wheelPieces [ index ];
            Transform pieceTrns = InstantiatePiece().transform.GetChild (0);

            //pieceTrns.GetChild (0).GetComponent<Image>().sprite = piece.Icon;
            pieceTrns.GetChild(0).GetComponent<TextMeshProUGUI>().text = FormatNumsHelper.FormatNum(piece.Amount);

            pieceTrns.RotateAround(_wheelPiecesParent.position, Vector3.back, _pieceAngle * index);
        }

        private GameObject InstantiatePiece() 
        {
            return Instantiate(_wheelPiecePrefab, _wheelPiecesParent.position, Quaternion.identity, _wheelPiecesParent);
        }

        public void Spin() 
        {
            if (!_isSpinning) 
            {
                _isSpinning = true;

                _onSpinStartEvent?.Invoke();

                int index = GetRandomPieceIndex();
                WheelPiece piece = _wheelPieces[index];

                Debug.Log($"Reward: {piece.Amount}");

                if (piece.Chance == 0 && _nonZeroChancesIndices.Count != 0) 
                {
                    index = _nonZeroChancesIndices[Random.Range(0, _nonZeroChancesIndices.Count)];
                    piece = _wheelPieces[index];
                }

                float angle = -(_pieceAngle * index);

                float rightOffset = (angle - _halfPieceAngleWithPaddings) % 360;
                float leftOffset = (angle + _halfPieceAngleWithPaddings) % 360;

                float randomAngle = Random.Range(leftOffset, rightOffset);

                Vector3 targetRotation = Vector3.back * (randomAngle + 2 * 360 * _spinDuration);

                float prevAngle, currentAngle;
                prevAngle = currentAngle = _wheelCircle.eulerAngles.z;

                bool isIndicatorOnTheLine = false;

                Debug.LogError(targetRotation);

                _wheelCircle
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
                })
                .OnComplete(() => 
                {
                    _isSpinning = false;

                    _onSpinEndEvent?.Invoke(piece);
                    _onSpinStartEvent = null; 
                    _onSpinEndEvent = null;
                }) ;
            }
        }

        public void OnSpinStart(UnityAction action) 
        {
            _onSpinStartEvent = action;
        }

        public void OnSpinEnd(UnityAction<WheelPiece> action) 
        {
            _onSpinEndEvent = action;
        }

        private int GetRandomPieceIndex() 
        {
            double r = _rand.NextDouble() * _accumulatedWeight;

            for (int i = 0; i < _wheelPieces.Length; i++)
                if (_wheelPieces [ i ]._weight >= r)
                    return i;

            return 0;
        }

        private void CalculateWeightsAndIndices() 
        {
            for (int i = 0; i < _wheelPieces.Length; i++) 
            {
                WheelPiece piece = _wheelPieces[i];

                //add weights:
                _accumulatedWeight += piece.Chance;
                piece._weight = _accumulatedWeight;

                //add index :
                piece.Index = i;

                //save non zero chance indices:
                if (piece.Chance > 0)
                    _nonZeroChancesIndices.Add(i);
            }
        }
    }
}