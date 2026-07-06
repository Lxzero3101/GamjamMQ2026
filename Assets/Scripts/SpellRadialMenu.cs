using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpellRadialMenu : MonoBehaviour
{
    [System.Serializable]
    private class SpellOption
    {
        public string _spellName = "New Spell";
        public Sprite _icon;
        public GameObject _prefab;
    }

    [Header("Spells (adjustable list)")]
    [SerializeField] private SpellOption[] _spellOptions;

    [Header("Activation")]
    [SerializeField] private Key _activationKey = Key.F;
    [SerializeField] private Key _cancelEquippedKey = Key.Escape;

    [Header("Radial Layout")]
    [SerializeField] private float _radius = 150f;
    [SerializeField] private float _iconSize = 72f;
    [SerializeField] private float _deadzoneRadius = 35f;
    [SerializeField] private float _startAngleOffsetDegrees = 90f; // 90 = first icon points up

    [Header("Visuals")]
    [SerializeField] private Sprite _backgroundCircleSprite;
    [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color _iconNormalColor = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private Color _iconHighlightColor = Color.white;
    [SerializeField] private float _iconHighlightScale = 1.25f;
    [SerializeField] private bool _showNameLabel = true;

    [Header("Placement")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private bool _consumeOnPlace = true;
    [SerializeField] private Color _ghostColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Canvas")]
    [SerializeField] private Canvas _existingCanvas; // optional, auto-created if left empty

    // Runtime state
    private Canvas _canvas;
    private RectTransform _menuRoot;
    private RectTransform[] _iconRects;
    private Image[] _iconImages;
    private Text[] _iconLabels;
    private Vector2 _menuScreenCenter;
    private int _selectedIndex = -1;
    private bool _menuOpen;

    private GameObject _equippedGhost;
    private SpellOption _equippedOption;

    private void Awake()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        EnsureCanvas();
    }

    private void EnsureCanvas()
    {
        if (_existingCanvas != null)
        {
            _canvas = _existingCanvas;
            return;
        }

        GameObject canvasGO = new GameObject("SpellRadialMenu_Canvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        var activation = keyboard[_activationKey];

        if (activation.wasPressedThisFrame && !_menuOpen)
        {
            OpenMenu(mouse.position.ReadValue());
        }
        else if (activation.isPressed && _menuOpen)
        {
            UpdateMenuSelection(mouse.position.ReadValue());
        }
        else if (activation.wasReleasedThisFrame && _menuOpen)
        {
            CloseMenu(confirmSelection: true);
        }

        // Cancel currently equipped item
        if (_equippedOption != null && keyboard[_cancelEquippedKey].wasPressedThisFrame)
        {
            ClearEquipped();
        }

        // Follow mouse with equipped ghost + place on click
        if (_equippedGhost != null && !_menuOpen)
        {
            Vector3 worldPos = ScreenToWorld(mouse.position.ReadValue());
            _equippedGhost.transform.position = worldPos;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                PlaceEquippedItem(worldPos);
            }
        }
    }

    // ---------- Menu open / update / close ----------

    private void OpenMenu(Vector2 screenPos)
    {
        if (_spellOptions == null || _spellOptions.Length == 0) return;

        _menuOpen = true;
        _menuScreenCenter = screenPos;
        _selectedIndex = -1;

        _menuRoot = new GameObject("RadialMenuRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        _menuRoot.SetParent(_canvas.transform, false);
        _menuRoot.anchoredPosition = ScreenToCanvasPoint(screenPos);

        // Background
        GameObject bgGO = new GameObject("Background", typeof(Image));
        bgGO.transform.SetParent(_menuRoot, false);
        Image bgImage = bgGO.GetComponent<Image>();
        bgImage.sprite = _backgroundCircleSprite;
        bgImage.color = _backgroundColor;
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = Vector2.one * (_radius * 2f + _iconSize);
        bgRect.anchoredPosition = Vector2.zero;

        int count = _spellOptions.Length;
        _iconRects = new RectTransform[count];
        _iconImages = new Image[count];
        _iconLabels = new Text[count];

        float segmentAngle = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = _startAngleOffsetDegrees - i * segmentAngle;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;

            GameObject iconGO = new GameObject("Icon_" + _spellOptions[i]._spellName, typeof(Image));
            iconGO.transform.SetParent(_menuRoot, false);
            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.sizeDelta = Vector2.one * _iconSize;
            iconRect.anchoredPosition = pos;

            Image iconImage = iconGO.GetComponent<Image>();
            iconImage.sprite = _spellOptions[i]._icon;
            iconImage.color = _iconNormalColor;
            iconImage.preserveAspect = true;

            _iconRects[i] = iconRect;
            _iconImages[i] = iconImage;

            if (_showNameLabel)
            {
                GameObject labelGO = new GameObject("Label", typeof(Text));
                labelGO.transform.SetParent(iconGO.transform, false);
                Text label = labelGO.GetComponent<Text>();
                label.text = _spellOptions[i]._spellName;
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 14;
                label.color = Color.white;
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                RectTransform labelRect = labelGO.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, -0.6f);
                labelRect.anchorMax = new Vector2(1f, 0f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                _iconLabels[i] = label;
            }
        }

        UpdateMenuSelection(screenPos);
    }

    private void UpdateMenuSelection(Vector2 mouseScreenPos)
    {
        if (!_menuOpen || _iconRects == null) return;

        Vector2 dir = mouseScreenPos - _menuScreenCenter;
        int newSelected = -1;

        if (dir.magnitude >= _deadzoneRadius)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle = NormalizeAngle(angle);

            int count = _spellOptions.Length;
            float segmentAngle = 360f / count;

            // Convert to the same reference frame used when placing icons
            float relativeAngle = NormalizeAngle(_startAngleOffsetDegrees - angle);
            newSelected = Mathf.FloorToInt((relativeAngle + segmentAngle / 2f) / segmentAngle) % count;
        }

        if (newSelected != _selectedIndex)
        {
            _selectedIndex = newSelected;
            for (int i = 0; i < _iconImages.Length; i++)
            {
                bool isSelected = (i == _selectedIndex);
                _iconImages[i].color = isSelected ? _iconHighlightColor : _iconNormalColor;
                _iconRects[i].localScale = Vector3.one * (isSelected ? _iconHighlightScale : 1f);
            }
        }
    }

    private void CloseMenu(bool confirmSelection)
    {
        _menuOpen = false;

        if (confirmSelection && _selectedIndex >= 0 && _selectedIndex < _spellOptions.Length)
        {
            Equip(_spellOptions[_selectedIndex]);
        }

        if (_menuRoot != null)
        {
            Destroy(_menuRoot.gameObject);
        }
        _menuRoot = null;
        _iconRects = null;
        _iconImages = null;
        _iconLabels = null;
        _selectedIndex = -1;
    }

    // ---------- Equip / place ----------

    private void Equip(SpellOption option)
    {
        if (option._prefab == null) return;

        ClearEquipped();

        _equippedOption = option;
        _equippedGhost = Instantiate(option._prefab);
        _equippedGhost.name = option._spellName + "_Ghost";

        // Disable colliders on ghost so it doesn't interact with the world yet
        foreach (var col in _equippedGhost.GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        // Tint sprite renderers translucent to show it's a preview
        foreach (var sr in _equippedGhost.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.color = _ghostColor;
        }
    }

    private void PlaceEquippedItem(Vector3 worldPos)
    {
        if (_equippedOption == null) return;

        Instantiate(_equippedOption._prefab, worldPos, Quaternion.identity);

        if (_consumeOnPlace)
        {
            ClearEquipped();
        }
    }

    private void ClearEquipped()
    {
        if (_equippedGhost != null) Destroy(_equippedGhost);
        _equippedGhost = null;
        _equippedOption = null;
    }

    // ---------- Helpers ----------

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 screenPoint = screenPos;
        screenPoint.z = Mathf.Abs(_mainCamera.transform.position.z);
        Vector3 world = _mainCamera.ScreenToWorldPoint(screenPoint);
        world.z = 0f;
        return world;
    }

    private Vector2 ScreenToCanvasPoint(Vector2 screenPos)
    {
        RectTransform canvasRect = _canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out Vector2 localPoint);
        return localPoint;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }
}