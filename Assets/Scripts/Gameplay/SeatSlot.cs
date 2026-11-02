using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WheresMySeat
{
    public class SeatSlot : MonoBehaviour, ISlot, ISelectableSlot
    {
        public string id;
        public int indexOfSeat;
        public Transform anchor;
        public List<SimpleTraitType> traits = new List<SimpleTraitType>();
        public SeatPosture posture = SeatPosture.Sitting;
        public bool isItemOnly;
        public bool isBlockedByDance;

        [HideInInspector] public Person occupant;
        [HideInInspector] public GameObject itemOccupant;

        private SpriteRenderer _baseRenderer;
        private SpriteRenderer _borderRenderer;
        private SpriteRenderer _dirtyOverlayRenderer;
        private SpriteRenderer _hintRingRenderer;
        private TextMeshPro _seatLabelTmp;
        private TextMeshPro _traitBadgeTmp;
        private Sprite _normalSeatSprite;
        private Sprite _validSeatSprite;
        private Sprite _invalidSeatSprite;
        private bool _isSelecting;
        private bool _isHinted;

        public string Id => id;
        public int IndexOfSeat => indexOfSeat;
        public Transform Anchor => anchor != null ? anchor : transform;
        public bool IsOccupied => occupant != null || isBlockedByDance;
        public bool IsSeatSlot => true;
        public Person Occupant => occupant;
        public GameObject ItemOccupant => itemOccupant;
        public List<SimpleTraitType> Traits => traits;
        public SeatPosture Posture => posture;

        public void BuildVisuals(string seatId, int idx, List<SimpleTraitType> slotTraits, Color themeColor)
        {
            id = seatId;
            indexOfSeat = idx;
            traits = slotTraits != null ? new List<SimpleTraitType>(slotTraits) : new List<SimpleTraitType>();

            if (anchor == null)
            {
                var anchorGo = new GameObject("Anchor");
                anchorGo.transform.SetParent(transform, false);
                anchorGo.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                anchor = anchorGo.transform;
            }

            bool isOpposite = traits.Contains(SimpleTraitType.Opposite_Direction);
            bool isVip = traits.Contains(SimpleTraitType.King) || traits.Contains(SimpleTraitType.Private);

            if (isVip)
            {
                _normalSeatSprite = ProceduralSprites.LoadAPKSprite("seat_king_0", 95f);
            }
            else if (isOpposite)
            {
                _normalSeatSprite = ProceduralSprites.LoadAPKSprite("seat_bus_nguoc_full", 56f);
            }
            else
            {
                _normalSeatSprite = ProceduralSprites.LoadAPKSprite("seat_bus", 56f);
            }

            _validSeatSprite = ProceduralSprites.LoadAPKSprite("seat_bus_valid", 56f) ?? _normalSeatSprite;
            _invalidSeatSprite = ProceduralSprites.LoadAPKSprite("seat_bus_invalid", 56f) ?? _normalSeatSprite;

            if (_normalSeatSprite == null)
                _normalSeatSprite = ProceduralSprites.GetRoundedRect(128, 128, 24f, 5f);

            // Selection / Status Halo beneath seat
            var borderGo = new GameObject("SeatHalo");
            borderGo.transform.SetParent(transform, false);
            borderGo.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            borderGo.transform.localScale = new Vector3(1.22f, 1.38f, 1f);
            _borderRenderer = borderGo.AddComponent<SpriteRenderer>();
            _borderRenderer.sprite = ProceduralSprites.LoadAPKSprite("seat_border", 42f) ?? ProceduralSprites.GetRoundedRect(128, 128, 28f);
            _borderRenderer.color = new Color(1f, 1f, 1f, 0f);
            _borderRenderer.sortingOrder = 1;

            // Main APK Seat Sprite
            var baseGo = new GameObject("SeatBody");
            baseGo.transform.SetParent(transform, false);
            baseGo.transform.localScale = isVip ? new Vector3(1.05f, 0.92f, 1f) : new Vector3(1.12f, 1.08f, 1f);
            _baseRenderer = baseGo.AddComponent<SpriteRenderer>();
            _baseRenderer.sprite = _normalSeatSprite;
            _baseRenderer.color = Color.white;
            _baseRenderer.sortingOrder = 2;

            // Dirty stain overlay (from APKUI/dirty.png)
            var dirtyGo = new GameObject("DirtyOverlay");
            dirtyGo.transform.SetParent(transform, false);
            dirtyGo.transform.localPosition = new Vector3(0.06f, 0.14f, 0f);
            dirtyGo.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            _dirtyOverlayRenderer = dirtyGo.AddComponent<SpriteRenderer>();
            _dirtyOverlayRenderer.sprite = ProceduralSprites.LoadAPKSprite("dirty", 48f);
            _dirtyOverlayRenderer.sortingOrder = 3;
            _dirtyOverlayRenderer.enabled = traits.Contains(SimpleTraitType.Dirty);

            // Hint Ring (pulsing golden aura)
            var hintGo = new GameObject("HintRing");
            hintGo.transform.SetParent(transform, false);
            hintGo.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            _hintRingRenderer = hintGo.AddComponent<SpriteRenderer>();
            _hintRingRenderer.sprite = ProceduralSprites.LoadAPKSprite("game_sfx-aura-yellow", 140f) ?? ProceduralSprites.GetCircle(128, 10f);
            _hintRingRenderer.color = new Color(1f, 0.90f, 0.25f, 0.90f);
            _hintRingRenderer.sortingOrder = 1;
            _hintRingRenderer.enabled = false;

            // Seat number badge (#1, #2, ...) on bottom wood trim of seat
            var numGo = new GameObject("SeatNumber");
            numGo.transform.SetParent(transform, false);
            numGo.transform.localPosition = new Vector3(-0.32f, -0.44f, 0f);
            _seatLabelTmp = numGo.AddComponent<TextMeshPro>();
            ProceduralSprites.ApplyGameFont(_seatLabelTmp);
            _seatLabelTmp.text = $"#{idx + 1}";
            _seatLabelTmp.fontSize = 1.95f;
            _seatLabelTmp.fontStyle = FontStyles.Bold;
            _seatLabelTmp.alignment = TextAlignmentOptions.Left;
            _seatLabelTmp.color = new Color(1f, 0.96f, 0.85f, 0.92f);
            _seatLabelTmp.sortingOrder = 4;
            var numRect = numGo.GetComponent<RectTransform>();
            numRect.sizeDelta = new Vector2(0.9f, 0.36f);

            // Trait Badges (WIN, AISLE, FRONT, DIRTY, VIP, etc.)
            var badgeGo = new GameObject("TraitBadges");
            badgeGo.transform.SetParent(transform, false);
            badgeGo.transform.localPosition = new Vector3(0.12f, -0.44f, 0f);
            _traitBadgeTmp = badgeGo.AddComponent<TextMeshPro>();
            ProceduralSprites.ApplyGameFont(_traitBadgeTmp);
            _traitBadgeTmp.text = BuildTraitShortLabel();
            _traitBadgeTmp.fontSize = 1.55f;
            _traitBadgeTmp.fontStyle = FontStyles.Bold;
            _traitBadgeTmp.alignment = TextAlignmentOptions.Right;
            _traitBadgeTmp.color = new Color(1f, 0.92f, 0.65f, 0.95f);
            _traitBadgeTmp.sortingOrder = 4;
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.sizeDelta = new Vector2(1.05f, 0.36f);

            var col = gameObject.GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.32f, 1.38f);

            RefreshVisualState();
        }

        private string BuildTraitShortLabel()
        {
            if (isBlockedByDance) return "✖ BLOCK";
            var parts = new List<string>();
            if (traits.Contains(SimpleTraitType.Window)) parts.Add("WIN");
            if (traits.Contains(SimpleTraitType.Aisle)) parts.Add("AISLE");
            if (traits.Contains(SimpleTraitType.Front)) parts.Add("FRONT");
            if (traits.Contains(SimpleTraitType.Back)) parts.Add("BACK");
            if (traits.Contains(SimpleTraitType.Driver)) parts.Add("DRV");
            if (traits.Contains(SimpleTraitType.Dirty)) parts.Add("DIRTY");
            if (traits.Contains(SimpleTraitType.Table)) parts.Add("TBL");
            if (traits.Contains(SimpleTraitType.View_River)) parts.Add("RIVER");
            if (traits.Contains(SimpleTraitType.Cold)) parts.Add("AC");
            if (traits.Contains(SimpleTraitType.Hot)) parts.Add("HOT");
            if (traits.Contains(SimpleTraitType.Private) || traits.Contains(SimpleTraitType.King)) parts.Add("VIP");
            if (traits.Contains(SimpleTraitType.Opposite_Direction)) parts.Add("REV");
            return string.Join(" ", parts);
        }

        public void SetOccupant(Person person)
        {
            occupant = person;
            RefreshVisualState();
        }

        public void SetOccupantItem(GameObject item)
        {
            itemOccupant = item;
            RefreshVisualState();
        }

        public void ActiveSelecting(bool active)
        {
            _isSelecting = active;
            RefreshVisualState();
        }

        public void ShowHintHighlight(bool show)
        {
            _isHinted = show;
            if (_hintRingRenderer != null)
                _hintRingRenderer.enabled = show;
            RefreshVisualState();
        }

        public void AddTrait(SimpleTraitType t)
        {
            if (!traits.Contains(t)) traits.Add(t);
            if (_dirtyOverlayRenderer != null)
                _dirtyOverlayRenderer.enabled = traits.Contains(SimpleTraitType.Dirty);
            if (_traitBadgeTmp != null) _traitBadgeTmp.text = BuildTraitShortLabel();
        }

        public void RemoveTrait(SimpleTraitType t)
        {
            traits.Remove(t);
            if (_dirtyOverlayRenderer != null)
                _dirtyOverlayRenderer.enabled = traits.Contains(SimpleTraitType.Dirty);
            if (_traitBadgeTmp != null) _traitBadgeTmp.text = BuildTraitShortLabel();
        }

        public void RefreshVisualState()
        {
            if (_baseRenderer == null || _borderRenderer == null) return;

            if (_dirtyOverlayRenderer != null)
                _dirtyOverlayRenderer.enabled = traits.Contains(SimpleTraitType.Dirty);

            if (isBlockedByDance)
            {
                _baseRenderer.sprite = _invalidSeatSprite;
                _baseRenderer.color = new Color(0.85f, 0.35f, 0.40f, 0.95f);
                _borderRenderer.color = new Color(1f, 0.25f, 0.35f, 0.95f);
                return;
            }

            if (_isSelecting)
            {
                _baseRenderer.sprite = _normalSeatSprite;
                _baseRenderer.color = new Color(1f, 0.98f, 0.82f, 1f);
                _borderRenderer.color = new Color(0.25f, 0.92f, 0.98f, 0.95f);
            }
            else if (occupant != null)
            {
                if (occupant.IsPlacementValid)
                {
                    _baseRenderer.sprite = _validSeatSprite;
                    _baseRenderer.color = Color.white;
                    _borderRenderer.color = new Color(0.20f, 0.92f, 0.52f, 0.85f);
                }
                else
                {
                    _baseRenderer.sprite = _invalidSeatSprite;
                    _baseRenderer.color = Color.white;
                    _borderRenderer.color = new Color(1f, 0.28f, 0.36f, 0.92f);
                }
            }
            else if (_isHinted)
            {
                _baseRenderer.sprite = _normalSeatSprite;
                _baseRenderer.color = new Color(1f, 0.96f, 0.76f, 1f);
                _borderRenderer.color = new Color(1f, 0.85f, 0.18f, 0.95f);
            }
            else
            {
                _baseRenderer.sprite = _normalSeatSprite;
                _baseRenderer.color = Color.white;
                _borderRenderer.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        private void Update()
        {
            if (_isHinted && _hintRingRenderer != null)
            {
                float pulse = 0.72f + Mathf.Sin(Time.time * 6f) * 0.08f;
                _hintRingRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
    }

    public class StartSlot : MonoBehaviour, ISlot, ISelectableSlot
    {
        public string id;
        public int indexOfSlot;
        public Transform anchor;
        public List<SimpleTraitType> traits = new List<SimpleTraitType>();
        [HideInInspector] public Person occupant;
        [HideInInspector] public GameObject itemOccupant;

        private SpriteRenderer _padRenderer;
        private Sprite _activePadSprite;
        private Sprite _greyPadSprite;

        public string Id => id;
        public int IndexOfSeat => -1 - indexOfSlot;
        public Transform Anchor => anchor != null ? anchor : transform;
        public bool IsOccupied => occupant != null;
        public bool IsSeatSlot => false;
        public Person Occupant => occupant;
        public GameObject ItemOccupant => itemOccupant;
        public List<SimpleTraitType> Traits => traits;
        public SeatPosture Posture => SeatPosture.Standing;

        public void BuildVisuals(string slotId, int idx)
        {
            id = slotId;
            indexOfSlot = idx;
            anchor = transform;

            _activePadSprite = ProceduralSprites.LoadAPKSprite("startPos", 56f) ?? ProceduralSprites.GetRoundedRect(128, 64, 24f);
            _greyPadSprite = ProceduralSprites.LoadAPKSprite("startPos_grey", 56f) ?? _activePadSprite;

            var padGo = new GameObject("QueuePad");
            padGo.transform.SetParent(transform, false);
            padGo.transform.localPosition = new Vector3(0f, -0.22f, 0f);
            padGo.transform.localScale = new Vector3(1.12f, 1.12f, 1f);
            _padRenderer = padGo.AddComponent<SpriteRenderer>();
            _padRenderer.sprite = _greyPadSprite;
            _padRenderer.color = Color.white;
            _padRenderer.sortingOrder = 1;

            var col = gameObject.GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.18f, 1.18f);
        }

        public void SetOccupant(Person person)
        {
            occupant = person;
            if (_padRenderer != null)
            {
                _padRenderer.sprite = occupant != null ? _activePadSprite : _greyPadSprite;
                _padRenderer.color = occupant != null ? Color.white : new Color(1f, 1f, 1f, 0.78f);
            }
        }

        public void SetOccupantItem(GameObject item)
        {
            itemOccupant = item;
        }

        public void ActiveSelecting(bool active)
        {
            if (_padRenderer != null)
            {
                _padRenderer.sprite = active ? _activePadSprite : (occupant != null ? _activePadSprite : _greyPadSprite);
                _padRenderer.color = active ? new Color(0.65f, 1f, 0.95f, 1f) : Color.white;
            }
        }
    }

    public class OnOffItem : MonoBehaviour, IPointerClickHandler
    {
        public int indexOfSeat;
        public bool isOn = true;
        private SpriteRenderer _bulbRenderer;
        private SpriteRenderer _glowRenderer;
        private TextMeshPro _labelTmp;

        public bool IsOn => isOn;
        public int IndexOfSeat => indexOfSeat;

        public void BuildVisuals(int seatIdx, bool initialOn)
        {
            indexOfSeat = seatIdx;
            isOn = initialOn;

            var glowGo = new GameObject("LampGlow");
            glowGo.transform.SetParent(transform, false);
            glowGo.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            _glowRenderer = glowGo.AddComponent<SpriteRenderer>();
            _glowRenderer.sprite = ProceduralSprites.LoadAPKSprite("game_sfx-aura-yellow", 180f) ?? ProceduralSprites.GetCircle(64);
            _glowRenderer.sortingOrder = 8;

            var bulbGo = new GameObject("LampBulb");
            bulbGo.transform.SetParent(transform, false);
            bulbGo.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            _bulbRenderer = bulbGo.AddComponent<SpriteRenderer>();
            _bulbRenderer.sprite = ProceduralSprites.LoadAPKSprite("game_booster_pop", 120f) ?? ProceduralSprites.GetCircle(64);
            _bulbRenderer.sortingOrder = 9;

            var txtGo = new GameObject("LampLabel");
            txtGo.transform.SetParent(transform, false);
            txtGo.transform.localPosition = new Vector3(0f, 0f, 0f);
            _labelTmp = txtGo.AddComponent<TextMeshPro>();
            ProceduralSprites.ApplyGameFont(_labelTmp);
            _labelTmp.fontSize = 1.65f;
            _labelTmp.fontStyle = FontStyles.Bold;
            _labelTmp.alignment = TextAlignmentOptions.Center;
            _labelTmp.sortingOrder = 10;
            txtGo.GetComponent<RectTransform>().sizeDelta = new Vector2(1.2f, 0.45f);

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.36f;

            RefreshLampVisual();
        }

        public void SwitchOn()
        {
            isOn = true;
            RefreshLampVisual();
            if (LevelCompletionManager.Instance != null)
                LevelCompletionManager.Instance.ForceValidateAllPerson();
        }

        public void SwitchOff()
        {
            isOn = false;
            RefreshLampVisual();
            if (LevelCompletionManager.Instance != null)
                LevelCompletionManager.Instance.ForceValidateAllPerson();
        }

        public void Toggle()
        {
            if (isOn) SwitchOff();
            else SwitchOn();
        }

        private void OnMouseDown()
        {
            Toggle();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
        }

        private void RefreshLampVisual()
        {
            if (_bulbRenderer != null)
                _bulbRenderer.color = isOn ? new Color(1f, 0.94f, 0.32f, 1f) : new Color(0.55f, 0.58f, 0.65f, 1f);
            if (_glowRenderer != null)
                _glowRenderer.color = isOn ? new Color(1f, 0.88f, 0.20f, 0.55f) : new Color(0f, 0f, 0f, 0f);
            if (_labelTmp != null)
            {
                _labelTmp.text = isOn ? "💡ON" : "💡OFF";
                _labelTmp.color = isOn ? ProceduralSprites.DarkOutlineColor : new Color(0.92f, 0.94f, 0.98f);
            }
        }
    }
}
