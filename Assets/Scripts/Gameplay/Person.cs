using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WheresMySeat
{
    [Serializable]
    public struct PlacementUndo
    {
        public Person person;
        public ISlot from;
        public ISlot to;
        public Person swapped;
        public ISlot swappedFrom;
        public bool valid;
    }

    public class EyeFollowOnDrag2D : MonoBehaviour
    {
        public Transform leftPupil;
        public Transform rightPupil;
        public float maxOffset = 0.045f;
        public float moveSpeed = 14f;
        public bool isDragging;

        private Vector3 _leftOrigin;
        private Vector3 _rightOrigin;
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
            if (leftPupil != null) _leftOrigin = leftPupil.localPosition;
            if (rightPupil != null) _rightOrigin = rightPupil.localPosition;
        }

        private void Update()
        {
            if (leftPupil == null) return;
            if (_cam == null) _cam = Camera.main;

            if (isDragging && _cam != null)
            {
                Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 dir = (Vector2)(mouseWorld - transform.position);
                Vector2 offset = Vector2.ClampMagnitude(dir * 0.08f, maxOffset);
                leftPupil.localPosition = Vector3.Lerp(leftPupil.localPosition, _leftOrigin + (Vector3)offset, Time.deltaTime * moveSpeed);
                if (rightPupil != null && rightPupil != leftPupil)
                    rightPupil.localPosition = Vector3.Lerp(rightPupil.localPosition, _rightOrigin + (Vector3)offset, Time.deltaTime * moveSpeed);
            }
            else
            {
                float idleX = Mathf.Sin(Time.time * 1.8f + transform.GetHashCode() * 0.01f) * 0.018f;
                Vector3 idleOffset = new Vector3(idleX, 0f, 0f);
                leftPupil.localPosition = Vector3.Lerp(leftPupil.localPosition, _leftOrigin + idleOffset, Time.deltaTime * moveSpeed);
                if (rightPupil != null && rightPupil != leftPupil)
                    rightPupil.localPosition = Vector3.Lerp(rightPupil.localPosition, _rightOrigin + idleOffset, Time.deltaTime * moveSpeed);
            }
        }
    }

    public class Person : MonoBehaviour, IPerson
    {
        public string displayName;
        public string tagId;
        public RoleType roleType = RoleType.Adult;
        public bool isUseSpecialBoster;
        public List<PersonRequirement> requirements = new List<PersonRequirement>();
        public List<string> personTraits = new List<string>();

        public ISlot CurrentSlot;
        public ISlot InitialSlot;
        public SeatSlot hintSlot;

        public bool IsPlacementValid { get; set; }
        public string LastInvalidReason { get; set; }
        public event Action<Person> PlacementChanged;

        public int remainingTurns;
        public bool IsBlocked => remainingTurns > 0;

        public PersonFaceAnimation currentFace = PersonFaceAnimation.Face_normal;
        public PersonDragDrop personDragDrop;
        public EyeFollowOnDrag2D eyeFollow;

        private Transform _visualRoot;
        private SpriteRenderer _shadowRenderer;
        private SpriteRenderer _bodyRenderer;
        private SpriteRenderer _blushRenderer;
        private SpriteRenderer _eyesRenderer;
        private SpriteRenderer _pupilsRenderer;
        private SpriteRenderer _eyebrowsRenderer;
        private SpriteRenderer _mouthRenderer;
        private SpriteRenderer _statusBadgeRenderer;
        private TextMeshPro _nameLabelTmp;
        private TextMeshPro _blockCounterTmp;
        private GameObject _magicCrownObj;

        private Sprite _normalBodySprite;
        private Sprite _wrongBodySprite;
        private float _bobSeed;
        private static int _spawnCounter;

        public string DisplayName => displayName;
        public RoleType RoleType => roleType;
        public List<PersonRequirement> Requirements => requirements;

        public void BuildCharacterVisuals(string charName, Color color, RoleType role, List<PersonRequirement> reqs, List<string> traits = null, int blockedTurns = 0)
        {
            displayName = charName;
            tagId = charName;
            roleType = role;
            requirements = reqs ?? new List<PersonRequirement>();
            personTraits = traits ?? new List<string>();
            remainingTurns = blockedTurns;
            _bobSeed = UnityEngine.Random.Range(0f, 10f);

            int charHash = Mathf.Abs((charName ?? "Blob").GetHashCode() + (_spawnCounter++));
            int bodyIdx = (charHash % 4) + 1; // 1..4
            int paletteIdx = charHash % 8;     // 0..7
            string bodyBaseName = $"Blob/Blob_{bodyIdx}_body{bodyIdx}";

            _normalBodySprite = ProceduralSprites.LoadAPKSprite($"{bodyBaseName}_p{paletteIdx}", 175f)
                             ?? ProceduralSprites.LoadAPKSprite(bodyBaseName, 175f)
                             ?? ProceduralSprites.GetBlobBody(128);
            _wrongBodySprite = ProceduralSprites.LoadAPKSprite($"{bodyBaseName}_wrong", 175f)
                            ?? _normalBodySprite;

            // Shadow (APKUI/Blob/shadow.png)
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(transform, false);
            shadowGo.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            shadowGo.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
            _shadowRenderer = shadowGo.AddComponent<SpriteRenderer>();
            _shadowRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/shadow", 175f) ?? ProceduralSprites.GetCircle(64);
            _shadowRenderer.color = new Color(1f, 1f, 1f, 0.78f);
            _shadowRenderer.sortingOrder = 10;

            // Visual Root (for squash & stretch + idle breathing)
            var rootGo = new GameObject("VisualRoot");
            rootGo.transform.SetParent(transform, false);
            _visualRoot = rootGo.transform;
            float roleScale = role == RoleType.Kid ? 0.80f : 0.94f;
            _visualRoot.localScale = new Vector3(roleScale, roleScale, 1f);

            // Blob Body
            var bodyGo = new GameObject("BlobBody");
            bodyGo.transform.SetParent(_visualRoot, false);
            _bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
            _bodyRenderer.sprite = _normalBodySprite;
            _bodyRenderer.color = Color.white;
            _bodyRenderer.sortingOrder = 12;

            // Rosy Blush Cheeks (APKUI/Blob/blush.png)
            var blushGo = new GameObject("Blush");
            blushGo.transform.SetParent(_visualRoot, false);
            blushGo.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            _blushRenderer = blushGo.AddComponent<SpriteRenderer>();
            _blushRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/blush", 175f);
            _blushRenderer.color = new Color(1f, 1f, 1f, 0.88f);
            _blushRenderer.sortingOrder = 13;

            // Paired Eyes (APKUI/Blob/Normal_eye.png, Happy_eye.png, Angry_eye.png, Wow_eye.png)
            var eyesGo = new GameObject("Eyes");
            eyesGo.transform.SetParent(_visualRoot, false);
            eyesGo.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            _eyesRenderer = eyesGo.AddComponent<SpriteRenderer>();
            _eyesRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Normal_eye", 175f);
            _eyesRenderer.sortingOrder = 14;

            // Paired Pupils / Lens (APKUI/Blob/Normal_len.png)
            var pupilsGo = new GameObject("Pupils");
            pupilsGo.transform.SetParent(eyesGo.transform, false);
            pupilsGo.transform.localPosition = Vector3.zero;
            _pupilsRenderer = pupilsGo.AddComponent<SpriteRenderer>();
            _pupilsRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Normal_len", 175f);
            _pupilsRenderer.sortingOrder = 15;

            eyeFollow = gameObject.AddComponent<EyeFollowOnDrag2D>();
            eyeFollow.leftPupil = pupilsGo.transform;
            eyeFollow.rightPupil = pupilsGo.transform;

            // Eyebrows (APKUI/Blob/Normal_eyebrows.png)
            var browsGo = new GameObject("Eyebrows");
            browsGo.transform.SetParent(_visualRoot, false);
            browsGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            _eyebrowsRenderer = browsGo.AddComponent<SpriteRenderer>();
            _eyebrowsRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Normal_eyebrows", 175f);
            _eyebrowsRenderer.sortingOrder = 16;

            // Mouth / Lip (APKUI/Blob/Normal_lip.png, Happy_lip.png, Angry_lip.png, Wow_lip.png)
            var mouthGo = new GameObject("Mouth");
            mouthGo.transform.SetParent(_visualRoot, false);
            mouthGo.transform.localPosition = new Vector3(0f, -0.09f, 0f);
            _mouthRenderer = mouthGo.AddComponent<SpriteRenderer>();
            _mouthRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Normal_lip", 175f);
            _mouthRenderer.sortingOrder = 16;

            // Magic Booster Icon (APKUI/ic-special.png)
            _magicCrownObj = new GameObject("MagicCrown");
            _magicCrownObj.transform.SetParent(_visualRoot, false);
            _magicCrownObj.transform.localPosition = new Vector3(0f, 0.54f, 0f);
            _magicCrownObj.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            var crownSr = _magicCrownObj.AddComponent<SpriteRenderer>();
            crownSr.sprite = ProceduralSprites.LoadAPKSprite("ic-special", 110f);
            crownSr.sortingOrder = 20;
            _magicCrownObj.SetActive(false);

            // Warm Cream Name Pill beneath blob (APKUI/game_level_bar.png)
            var nameBgGo = new GameObject("NamePill");
            nameBgGo.transform.SetParent(transform, false);
            nameBgGo.transform.localPosition = new Vector3(0f, -0.52f, 0f);
            nameBgGo.transform.localScale = new Vector3(0.58f, 0.48f, 1f);
            var nameBgSr = nameBgGo.AddComponent<SpriteRenderer>();
            nameBgSr.sprite = ProceduralSprites.LoadAPKSprite("game_level_bar", 100f) ?? ProceduralSprites.GetRoundedRect(128, 48, 20f);
            nameBgSr.color = Color.white;
            nameBgSr.sortingOrder = 17;

            var nameTxtGo = new GameObject("NameText");
            nameTxtGo.transform.SetParent(transform, false);
            nameTxtGo.transform.localPosition = new Vector3(0f, -0.51f, 0f);
            _nameLabelTmp = nameTxtGo.AddComponent<TextMeshPro>();
            ProceduralSprites.ApplyGameFont(_nameLabelTmp);
            _nameLabelTmp.text = displayName;
            _nameLabelTmp.fontSize = 1.95f;
            _nameLabelTmp.fontStyle = FontStyles.Bold;
            _nameLabelTmp.alignment = TextAlignmentOptions.Center;
            _nameLabelTmp.color = ProceduralSprites.DarkOutlineColor;
            _nameLabelTmp.sortingOrder = 18;
            nameTxtGo.GetComponent<RectTransform>().sizeDelta = new Vector2(1.4f, 0.4f);

            // Status Badge (top-right: APKUI/v.png checkmark or APKUI/shared_red_noti_cham_than_0.png exclamation)
            var statusGo = new GameObject("StatusBadge");
            statusGo.transform.SetParent(transform, false);
            statusGo.transform.localPosition = new Vector3(0.38f, 0.38f, 0f);
            statusGo.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            _statusBadgeRenderer = statusGo.AddComponent<SpriteRenderer>();
            _statusBadgeRenderer.sortingOrder = 19;

            // Turn-based blocking lock indicator
            var blockGo = new GameObject("BlockTurnCounter");
            blockGo.transform.SetParent(transform, false);
            blockGo.transform.localPosition = new Vector3(-0.36f, 0.38f, 0f);
            _blockCounterTmp = blockGo.AddComponent<TextMeshPro>();
            ProceduralSprites.ApplyGameFont(_blockCounterTmp);
            _blockCounterTmp.fontSize = 2.0f;
            _blockCounterTmp.fontStyle = FontStyles.Bold;
            _blockCounterTmp.alignment = TextAlignmentOptions.Center;
            _blockCounterTmp.color = new Color(1f, 0.82f, 0.22f);
            _blockCounterTmp.sortingOrder = 19;
            blockGo.GetComponent<RectTransform>().sizeDelta = new Vector2(1.0f, 0.5f);
            UpdateBlockingUI();

            var col = gameObject.GetComponent<CircleCollider2D>();
            if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.54f;

            personDragDrop = gameObject.GetComponent<PersonDragDrop>();
            if (personDragDrop == null) personDragDrop = gameObject.AddComponent<PersonDragDrop>();
            personDragDrop.person = this;

            SetFaceAnimation(PersonFaceAnimation.Face_normal);
            UpdateStatusBadge();
        }

        public bool HasTag(string t)
        {
            if (string.IsNullOrEmpty(t)) return false;
            return string.Equals(tagId, t, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(displayName, t, StringComparison.OrdinalIgnoreCase) ||
                   personTraits.Contains(t);
        }

        public bool HasTrait(string t)
        {
            return personTraits != null && personTraits.Contains(t);
        }

        public void SetInitialSlot(ISlot slot)
        {
            InitialSlot = slot;
            SnapDirect(slot);
        }

        public void SnapDirect(ISlot targetSlot)
        {
            if (CurrentSlot != null && CurrentSlot.Occupant == this)
            {
                CurrentSlot.SetOccupant(null);
            }

            CurrentSlot = targetSlot;
            if (targetSlot != null)
            {
                targetSlot.SetOccupant(this);
                transform.position = targetSlot.Anchor.position;
            }
        }

        public void SnapTo(ISlot targetSlot, bool notifyPlacement = true)
        {
            SnapDirect(targetSlot);
            if (notifyPlacement)
            {
                PlacementChanged?.Invoke(this);
            }
        }

        public void SetPlacementValidity(bool valid, string reason = "")
        {
            IsPlacementValid = valid;
            LastInvalidReason = reason;

            if (CurrentSlot == null || !CurrentSlot.IsSeatSlot)
            {
                SetFaceAnimation(PersonFaceAnimation.Face_normal);
            }
            else if (valid)
            {
                SetFaceAnimation(PersonFaceAnimation.Face_happy);
            }
            else
            {
                SetFaceAnimation(PersonFaceAnimation.Face_angry);
            }

            if (CurrentSlot is SeatSlot ss)
            {
                ss.RefreshVisualState();
            }

            UpdateStatusBadge();
        }

        public void SetFaceAnimation(PersonFaceAnimation face)
        {
            currentFace = face;
            if (_bodyRenderer != null)
            {
                _bodyRenderer.sprite = (face == PersonFaceAnimation.Face_angry) ? _wrongBodySprite : _normalBodySprite;
            }

            if (face == PersonFaceAnimation.Face_happy || face == PersonFaceAnimation.Face_Sing_happy)
            {
                if (_eyesRenderer != null) _eyesRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Happy_eye", 175f);
                if (_pupilsRenderer != null) _pupilsRenderer.enabled = false;
                if (_eyebrowsRenderer != null) _eyebrowsRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Happy_eyebrows", 175f);
                if (_mouthRenderer != null) _mouthRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Happy_lip", 175f);
                if (_blushRenderer != null) _blushRenderer.enabled = true;
            }
            else if (face == PersonFaceAnimation.Face_angry || face == PersonFaceAnimation.Face_sad)
            {
                if (_eyesRenderer != null) _eyesRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Angry_eye", 175f);
                if (_pupilsRenderer != null)
                {
                    _pupilsRenderer.enabled = true;
                    _pupilsRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Angry_len", 175f);
                }
                if (_eyebrowsRenderer != null) _eyebrowsRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Angry_eyebrows", 175f);
                if (_mouthRenderer != null) _mouthRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Angry_lip", 175f);
                if (_blushRenderer != null) _blushRenderer.enabled = false;
            }
            else if (face == PersonFaceAnimation.Face_drag || face == PersonFaceAnimation.Face_wow)
            {
                if (_eyesRenderer != null) _eyesRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Wow_eye", 175f);
                if (_pupilsRenderer != null)
                {
                    _pupilsRenderer.enabled = true;
                    _pupilsRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Wow_len", 175f);
                }
                if (_eyebrowsRenderer != null) _eyebrowsRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Wow_eyebrows", 175f);
                if (_mouthRenderer != null) _mouthRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Wow_lip", 175f);
                if (_blushRenderer != null) _blushRenderer.enabled = true;
            }
            else
            {
                if (_eyesRenderer != null) _eyesRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Normal_eye", 175f);
                if (_pupilsRenderer != null)
                {
                    _pupilsRenderer.enabled = true;
                    _pupilsRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Normal_len", 175f);
                }
                if (_eyebrowsRenderer != null) _eyebrowsRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Normal_eyebrows", 175f);
                if (_mouthRenderer != null) _mouthRenderer.sprite = ProceduralSprites.LoadAPKSprite("Blob/Normal_lip", 175f);
                if (_blushRenderer != null) _blushRenderer.enabled = true;
            }
        }

        public void SetUseSpecialBoster()
        {
            isUseSpecialBoster = true;
            if (_magicCrownObj != null)
                _magicCrownObj.SetActive(true);
            if (LevelCompletionManager.Instance != null)
                LevelCompletionManager.Instance.ForceValidateAllPerson();
        }

        public void DecrementBlockCounter()
        {
            if (remainingTurns > 0)
            {
                remainingTurns--;
                UpdateBlockingUI();
            }
        }

        public void UpdateBlockingUI()
        {
            if (_blockCounterTmp == null) return;
            if (remainingTurns > 0)
            {
                _blockCounterTmp.gameObject.SetActive(true);
                _blockCounterTmp.text = $"📞{remainingTurns}";
            }
            else
            {
                _blockCounterTmp.gameObject.SetActive(false);
            }
        }

        public void SetSortingElevation(bool elevated)
        {
            int boost = elevated ? 40 : 0;
            if (_bodyRenderer != null) _bodyRenderer.sortingOrder = 12 + boost;
            if (_blushRenderer != null) _blushRenderer.sortingOrder = 13 + boost;
            if (_eyesRenderer != null) _eyesRenderer.sortingOrder = 14 + boost;
            if (_pupilsRenderer != null) _pupilsRenderer.sortingOrder = 15 + boost;
            if (_eyebrowsRenderer != null) _eyebrowsRenderer.sortingOrder = 16 + boost;
            if (_mouthRenderer != null) _mouthRenderer.sortingOrder = 16 + boost;
            if (_shadowRenderer != null) _shadowRenderer.enabled = !elevated;
            if (eyeFollow != null) eyeFollow.isDragging = elevated;
        }

        private void UpdateStatusBadge()
        {
            if (_statusBadgeRenderer == null) return;

            if (CurrentSlot == null || !CurrentSlot.IsSeatSlot)
            {
                _statusBadgeRenderer.enabled = false;
            }
            else if (IsPlacementValid)
            {
                _statusBadgeRenderer.enabled = true;
                _statusBadgeRenderer.sprite = ProceduralSprites.LoadAPKSprite("v", 75f);
                _statusBadgeRenderer.color = Color.white;
            }
            else
            {
                _statusBadgeRenderer.enabled = true;
                _statusBadgeRenderer.sprite = ProceduralSprites.LoadAPKSprite("shared_red_noti_cham_than_0", 90f)
                                           ?? ProceduralSprites.LoadAPKSprite("x", 75f);
                _statusBadgeRenderer.color = Color.white;
            }
        }

        private void Update()
        {
            if (_visualRoot == null) return;
            float baseScale = roleType == RoleType.Kid ? 0.80f : 0.94f;
            if (personDragDrop != null && personDragDrop.isDragging)
            {
                _visualRoot.localScale = Vector3.Lerp(_visualRoot.localScale, new Vector3(baseScale * 1.14f, baseScale * 1.14f, 1f), Time.deltaTime * 14f);
            }
            else
            {
                float breath = Mathf.Sin(Time.time * 3.2f + _bobSeed) * 0.032f;
                _visualRoot.localScale = new Vector3(baseScale * (1f - breath * 0.5f), baseScale * (1f + breath), 1f);
            }
        }
    }

    public class PersonDragDrop : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Person person;
        public float raycastRadius = 0.85f;
        public bool isDragging;

        private Camera _cam;
        private ISlot _highlightedSlot;
        private Vector3 _dragStartWorldPos;
        private static PersonDragDrop _activeDragger;
        private static readonly Stack<PlacementUndo> UndoStack = new Stack<PlacementUndo>();

        public static bool CanUndo => UndoStack.Count > 0;
        public static void ClearUndoHistory() => UndoStack.Clear();

        private void Awake()
        {
            _cam = Camera.main;
            if (person == null) person = GetComponent<Person>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            BeginDragAtScreenPos(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ContinueDragAtScreenPos(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            EndDrag();
        }

        private void OnMouseDown()
        {
            if (isDragging) return;
            BeginDragAtScreenPos(Input.mousePosition);
        }

        private void OnMouseDrag()
        {
            ContinueDragAtScreenPos(Input.mousePosition);
        }

        private void OnMouseUp()
        {
            EndDrag();
        }

        private void BeginDragAtScreenPos(Vector2 screenPos)
        {
            if (isDragging) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsGameState(GameState.Playing))
                return;

            // Always select this person in the floating speech bubble when tapped/dragged!
            if (WheresMySeatGameController.Instance != null)
            {
                WheresMySeatGameController.Instance.InspectPerson(person);
            }

            // Check if a targeted booster (Hint or Magic Seat) is waiting for a character click
            if (BoostersController.Instance != null && BoostersController.Instance.TryConsumeTargetedBoosterOnPerson(person))
            {
                return;
            }

            if (person.IsBlocked)
            {
                if (WheresMySeatGameController.Instance != null)
                    WheresMySeatGameController.Instance.ShowToast($"{person.DisplayName} is on a phone call for {person.remainingTurns} more move(s)!");
                return;
            }

            if (_cam == null) _cam = Camera.main;
            _activeDragger = this;
            isDragging = true;
            _dragStartWorldPos = transform.position;
            person.SetSortingElevation(true);
            person.SetFaceAnimation(PersonFaceAnimation.Face_drag);
        }

        private void ContinueDragAtScreenPos(Vector2 screenPos)
        {
            if (!isDragging || _activeDragger != this) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 mouseWorld = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            mouseWorld.z = 0f;
            transform.position = Vector3.Lerp(transform.position, mouseWorld + new Vector3(0f, 0.18f, 0f), Time.deltaTime * 28f);

            ISlot best = FindBestSlot(mouseWorld);
            if (best != _highlightedSlot)
            {
                if (_highlightedSlot is ISelectableSlot prevSel)
                    prevSel.ActiveSelecting(false);
                _highlightedSlot = best;
                if (_highlightedSlot is ISelectableSlot newSel)
                    newSel.ActiveSelecting(true);
            }
        }

        private void EndDrag()
        {
            if (!isDragging || _activeDragger != this) return;
            isDragging = false;
            _activeDragger = null;
            person.SetSortingElevation(false);

            if (_highlightedSlot is ISelectableSlot sel)
                sel.ActiveSelecting(false);

            ISlot target = _highlightedSlot;
            _highlightedSlot = null;

            float dragDist = Vector2.Distance(_dragStartWorldPos, transform.position);
            if (target != null && target != person.CurrentSlot && dragDist > 0.18f)
            {
                TryPlaceOrSwap(target);
            }
            else
            {
                person.SnapTo(person.CurrentSlot, false);
                if (LevelCompletionManager.Instance != null)
                    LevelCompletionManager.Instance.ForceValidateAllPerson();
            }
        }

        private ISlot FindBestSlot(Vector2 worldPos)
        {
            if (PlacementManager.Instance == null) return null;
            ISlot best = null;
            float bestDist = raycastRadius;

            var seatSlots = PlacementManager.Instance.GetAllSeatSlots();
            for (int i = 0; i < seatSlots.Count; i++)
            {
                SeatSlot s = seatSlots[i];
                if (s == null || s.isBlockedByDance) continue;
                float d = Vector2.Distance(worldPos, s.Anchor.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }

            var startSlots = PlacementManager.Instance.GetAllStartSlots();
            for (int i = 0; i < startSlots.Count; i++)
            {
                StartSlot s = startSlots[i];
                if (s == null) continue;
                float d = Vector2.Distance(worldPos, s.Anchor.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }

            return best;
        }

        private void TryPlaceOrSwap(ISlot targetSlot)
        {
            ISlot prevSlot = person.CurrentSlot;
            Person existingOccupant = targetSlot.Occupant;

            if (existingOccupant != null && existingOccupant.IsBlocked)
            {
                person.SnapTo(prevSlot, false);
                if (WheresMySeatGameController.Instance != null)
                    WheresMySeatGameController.Instance.ShowToast($"{existingOccupant.DisplayName} cannot be moved right now!");
                return;
            }

            var undoRecord = new PlacementUndo
            {
                person = person,
                from = prevSlot,
                to = targetSlot,
                swapped = existingOccupant,
                swappedFrom = targetSlot,
                valid = true
            };
            UndoStack.Push(undoRecord);

            if (existingOccupant != null && existingOccupant != person)
            {
                existingOccupant.SnapDirect(prevSlot);
                person.SnapDirect(targetSlot);
                existingOccupant.SnapTo(prevSlot, true);
                person.SnapTo(targetSlot, true);
            }
            else
            {
                person.SnapTo(targetSlot, true);
            }

            if (PlacementManager.Instance != null)
            {
                foreach (var p in PlacementManager.Instance.Persons)
                {
                    if (p != null && p != person)
                        p.DecrementBlockCounter();
                }
            }

            if (targetSlot.IsSeatSlot && TurnManager.Instance != null)
            {
                TurnManager.Instance.UseTurn();
            }

            if (WheresMySeatGameController.Instance != null)
            {
                WheresMySeatGameController.Instance.InspectPerson(person);
            }
        }

        public static bool UndoLastPlacement()
        {
            if (UndoStack.Count == 0) return false;
            PlacementUndo u = UndoStack.Pop();
            if (!u.valid || u.person == null || u.from == null) return false;

            u.person.SnapDirect(u.from);
            if (u.swapped != null && u.swappedFrom != null)
            {
                u.swapped.SnapDirect(u.swappedFrom);
            }

            if (LevelCompletionManager.Instance != null)
            {
                LevelCompletionManager.Instance.ForceValidateAllPerson();
            }
            return true;
        }
    }
}
