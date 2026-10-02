using System;
using Skins;
using TMPro;
using UI;
using UI.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Roulette
{
    public sealed class RouletteWinPopup : MonoBehaviour, IShowable
    {
        private const float FadeAlpha = 0.85f;
        private const int TextureSize = 512;
        private const float CameraHeight = 500f;
        private const float FitPadding = 0.85f;
        private const string ModelLayerName = "SkinsRender";

        private static readonly Vector3 StagePosition = new Vector3(0f, CameraHeight, 0f);

        [SerializeField] private RectTransform _window;
        [SerializeField, Tooltip("Фон окна. Перекрашивается в цвет редкости скина, при монетах — в Coins Background Color.")]
        private Image _background;
        [SerializeField, Tooltip("Цвет фона окна под выпавшие монеты (и до первого показа).")]
        private Color _coinsBackgroundColor = new Color(0.13f, 0.13f, 0.17f, 1f);
        [SerializeField, Range(0f, 1f), Tooltip("Насколько затемнять цвет редкости для фона. 0 — чистый цвет редкости.")]
        private float _backgroundDarkening = 0.35f;
        [SerializeField] private Image _fade;
        [SerializeField] private RawImage _prizeArea;
        [SerializeField] private TMP_Text _coinsLabel;
        [SerializeField, Tooltip("Старая плашка редкости: больше не рисуется, поле — якорь позиции rank-лейбла.")]
        private GameObject _rarityPlate;
        [SerializeField, Tooltip("Rank-лейблы GUI Kit с запечённым текстом: вариант под редкость скина.")]
        private GameObject _rankLabelNormal;
        [SerializeField] private GameObject _rankLabelRare;
        [SerializeField] private GameObject _rankLabelEpic;
        [SerializeField] private GameObject _rankLabelLegendary;
        [SerializeField, Tooltip("UI-партикл (Fx_Rotate из GUI Kit) под призом: показывается и со скином, и с монетами.")]
        private ParticleSystem _rotateFx;
        [SerializeField, Min(0f), Tooltip("Множитель масштаба Fx_Rotate: ассет авторован под канвас GUI Kit, множителем подгоняется под окно.")]
        private float _rotateFxScale = 1f;
        [SerializeField] private Button _takeButton;
        [SerializeField] private Camera _stageCamera;
        [SerializeField] private Transform _modelSlot;
        [SerializeField, Min(1f)] private float _rotationSpeed = 45f;

        private RenderTexture _texture;
        private GameObject _currentModel;
        private Animator _currentAnimator;
        private Vector3 _rotationAnchor;

        public event Action Closed;

        private void Awake()
        {
            if (_window == null || _background == null || _fade == null || _prizeArea == null || _coinsLabel == null
                || _rarityPlate == null || _takeButton == null || _stageCamera == null || _modelSlot == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a win popup part is not assigned. Drag the Window, Background, Fade, PrizeArea, CoinsLabel, " +
                    "RarityPlate, TakeButton, StageCamera and ModelSlot into the fields.");
            }

            if (_rankLabelNormal == null || _rankLabelRare == null || _rankLabelEpic == null || _rankLabelLegendary == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a rank label is not assigned. Drag the Rank_Label prefabs (GUI Kit: Normal, Rare, Epic, Legendary) into the fields.");
            }

            if (_rotateFx == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RotateFx is not assigned. Drag the Fx_Rotate prefab (GUI Kit) into the _rotateFx field.");
            }

            if (_rotateFx.transform is RectTransform == false)
            {
                throw new InvalidOperationException(
                    $"{name}: RotateFx must be a UI prefab with a RectTransform root. Check that the Fx_Rotate prefab is dragged in.");
            }

            _rarityPlate.SetActive(false);

            int modelLayer = LayerMask.NameToLayer(ModelLayerName);

            if (modelLayer < 0)
            {
                throw new InvalidOperationException(
                    $"{name}: the '{ModelLayerName}' layer does not exist. Add it in Project Settings → Tags and Layers.");
            }

            SetLayerRecursive(_modelSlot.gameObject, modelLayer);

            _texture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);
            _stageCamera.targetTexture = _texture;
            _stageCamera.cullingMask = 1 << modelLayer;
            _stageCamera.orthographic = true;
            _stageCamera.clearFlags = CameraClearFlags.SolidColor;
            _stageCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _prizeArea.texture = _texture;
        }

        private void OnEnable()
        {
            _takeButton.onClick.AddListener(OnTakeClicked);
        }

        private void OnDisable()
        {
            _takeButton.onClick.RemoveListener(OnTakeClicked);
        }

        private void Update()
        {
            if (_currentModel == null)
            {
                return;
            }

            _currentModel.transform.RotateAround(
                _rotationAnchor,
                Vector3.up,
                _rotationSpeed * Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (_texture == null)
            {
                return;
            }

            _texture.Release();
            Destroy(_texture);
            _texture = null;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            _takeButton.interactable = true;
            PositionStage();

            _background.color = _coinsBackgroundColor;
            _fade.color = new Color(0f, 0f, 0f, FadeAlpha);

            UiAnimations.ScaleIn(_window, UiAnimations.WindowScaleInDuration);
        }

        public void Hide()
        {
            _takeButton.interactable = false;

            UiAnimations.ScaleOut(_window, UiAnimations.WindowScaleOutDuration, HideAndDestroy);
        }

        public void ShowSkin(SkinItem skin, SkinRarity rarity, Color rarityColor)
        {
            if (gameObject.activeSelf == false)
            {
                throw new InvalidOperationException(
                    $"{name}: ShowSkin requires a shown popup. Spawn the popup through UiSpawner first.");
            }

            if (skin == null)
            {
                throw new ArgumentNullException(nameof(skin),
                    $"{name}: ShowSkin requires a skin.");
            }

            if (skin.Model == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SkinItem '{skin.name}' has no model prefab. Assign the Model in the skin asset.");
            }

            _prizeArea.gameObject.SetActive(true);
            _coinsLabel.gameObject.SetActive(false);

            _background.color = Darken(rarityColor);

            ShowRankLabel(rarity);
            ShowRotateFx();

            SetModel(skin);
        }

        public void ShowCoins(int amount)
        {
            if (gameObject.activeSelf == false)
            {
                throw new InvalidOperationException(
                    $"{name}: ShowCoins requires a shown popup. Spawn the popup through UiSpawner first.");
            }

            _prizeArea.gameObject.SetActive(false);
            ClearModel();

            _coinsLabel.gameObject.SetActive(true);
            _coinsLabel.text = $"×{amount}";

            ShowRotateFx();
        }

        private void ShowRankLabel(SkinRarity rarity)
        {
            GameObject prefab = GetRankLabelPrefab(rarity);

            InstantiateAt((RectTransform)_rarityPlate.transform, prefab, 1f);
        }

        private void ShowRotateFx()
        {
            GameObject instance = InstantiateAt(
                (RectTransform)_prizeArea.transform,
                _rotateFx.gameObject,
                _rotateFxScale);
            ParticleSystem fx = instance.GetComponent<ParticleSystem>();

            if (fx.isPlaying == false)
            {
                fx.Play();
            }
        }

        private GameObject InstantiateAt(RectTransform anchor, GameObject prefab, float scaleMultiplier)
        {
            GameObject instance = Instantiate(prefab, anchor.parent);

            if (instance.transform is RectTransform rect == false)
            {
                throw new InvalidOperationException(
                    $"{name}: prefab '{prefab.name}' has no RectTransform root. Rank labels and rotate fx must be UI prefabs.");
            }

            rect.anchorMin = anchor.anchorMin;
            rect.anchorMax = anchor.anchorMax;
            rect.anchoredPosition = anchor.anchoredPosition;
            rect.pivot = anchor.pivot;
            rect.sizeDelta = anchor.sizeDelta;
            rect.localRotation = anchor.localRotation;
            rect.localScale = Vector3.Scale(
                prefab.transform.localScale,
                new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier));
            rect.SetSiblingIndex(anchor.GetSiblingIndex());

            return instance;
        }

        private GameObject GetRankLabelPrefab(SkinRarity rarity)
        {
            switch (rarity)
            {
                case SkinRarity.Rare:
                    return _rankLabelRare;

                case SkinRarity.Epic:
                    return _rankLabelEpic;

                case SkinRarity.Legendary:
                    return _rankLabelLegendary;

                default:
                    return _rankLabelNormal;
            }
        }

        private Color Darken(Color color)
        {
            // Альфу не трогаем: Lerp к чёрному иначе приглушает окно до
            // полупрозрачности (чёрный несёт a=0).
            Color darkened = Color.Lerp(color, Color.black, _backgroundDarkening);
            darkened.a = color.a;

            return darkened;
        }

        private void PositionStage()
        {
            _modelSlot.position = StagePosition;
        }

        private void SetModel(SkinItem skin)
        {
            ClearModel();

            _currentModel = Instantiate(skin.Model, _modelSlot);
            SetLayerRecursive(_currentModel, _modelSlot.gameObject.layer);

            if (_currentModel.TryGetComponent(out SkinModel skinModel) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: model '{skin.Model.name}' has no SkinModel component on its prefab root.");
            }

            if (_currentModel.TryGetComponent(out _currentAnimator) == true)
            {
                _currentAnimator.SetTrigger("Walk");
            }

            FitCamera(skinModel);
        }

        private void FitCamera(SkinModel skinModel)
        {
            MeshFilter meshFilter = skinModel.MeshFilter;

            if (meshFilter.sharedMesh == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SkinModel '{skinModel.name}' has no mesh in its MeshFilter.");
            }

            _currentModel.transform.position = _modelSlot.position;

            Bounds worldBounds = ComputeWorldBounds(meshFilter.transform, meshFilter.sharedMesh.bounds);
            _currentModel.transform.position += _modelSlot.position - worldBounds.center;

            _rotationAnchor = _modelSlot.position;

            worldBounds = ComputeWorldBounds(meshFilter.transform, meshFilter.sharedMesh.bounds);
            float maxExtent = Mathf.Max(worldBounds.extents.y, worldBounds.extents.x);

            if (maxExtent <= 0f)
            {
                return;
            }

            _stageCamera.orthographicSize = Mathf.Max(0.1f, maxExtent / FitPadding);
        }

        private void OnTakeClicked()
        {
            Hide();
        }

        private void HideAndDestroy()
        {
            ClearModel();

            Action closed = Closed;
            closed?.Invoke();

            Destroy(gameObject);
        }

        private void ClearModel()
        {
            _currentAnimator = null;

            if (_currentModel == null)
            {
                return;
            }

            Destroy(_currentModel);
            _currentModel = null;
        }

        private static void SetLayerRecursive(GameObject root, int layer)
        {
            root.layer = layer;

            for (int i = 0; i < root.transform.childCount; i++)
            {
                SetLayerRecursive(root.transform.GetChild(i).gameObject, layer);
            }
        }

        private static Bounds ComputeWorldBounds(Transform sourceTransform, Bounds localBounds)
        {
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents;

            Bounds worldBounds = new Bounds(
                sourceTransform.TransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z)),
                Vector3.zero);

            worldBounds.Encapsulate(sourceTransform.TransformPoint(center + new Vector3(extents.x, -extents.y, -extents.z)));
            worldBounds.Encapsulate(sourceTransform.TransformPoint(center + new Vector3(-extents.x, extents.y, -extents.z)));
            worldBounds.Encapsulate(sourceTransform.TransformPoint(center + new Vector3(extents.x, extents.y, -extents.z)));
            worldBounds.Encapsulate(sourceTransform.TransformPoint(center + new Vector3(-extents.x, -extents.y, extents.z)));
            worldBounds.Encapsulate(sourceTransform.TransformPoint(center + new Vector3(extents.x, -extents.y, extents.z)));
            worldBounds.Encapsulate(sourceTransform.TransformPoint(center + new Vector3(-extents.x, extents.y, extents.z)));
            worldBounds.Encapsulate(sourceTransform.TransformPoint(center + new Vector3(extents.x, extents.y, extents.z)));

            return worldBounds;
        }
    }
}
