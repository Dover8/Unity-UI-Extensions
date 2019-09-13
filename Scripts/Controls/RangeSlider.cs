// /*-------------------------------------------
// ---------------------------------------------
// Creation Date: 12/09/19
// Author: bmackinnon
// Description: Iron Works
// Extension of the Unity.UI Slider that has two handles and a Min and Max value
// Soluis Technolgies ltd.
// ---------------------------------------------
// -------------------------------------------*/

using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Range Slider", 34)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class RangeSlider : Selectable, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {

        [Serializable]
        public class RangeSliderEvent : UnityEvent<float, float> { }

        [SerializeField]
        private RectTransform m_FillRect;

        public RectTransform FillRect { get { return m_FillRect; } set { if (SetClass(ref m_FillRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField]
        private RectTransform m_LowHandleRect;

        public RectTransform LowHandleRect { get { return m_LowHandleRect; } set { if (SetClass(ref m_LowHandleRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField]
        private RectTransform m_HighHandleRect;

        public RectTransform HighHandleRect { get { return m_HighHandleRect; } set { if (SetClass(ref m_HighHandleRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [Space]

        [SerializeField]
        private float m_MinValue = 0;

        public float MinValue { get { return m_MinValue; } set { if (SetStruct(ref m_MinValue, value)) { SetLow(m_LowValue); SetHigh(m_HighValue); UpdateVisuals(); } } }


        [SerializeField]
        private float m_MaxValue = 1;

        public float MaxValue { get { return m_MaxValue; } set { if (SetStruct(ref m_MaxValue, value)) { SetLow(m_LowValue); SetHigh(m_HighValue); UpdateVisuals(); } } }

        [SerializeField]
        private bool m_WholeNumbers = false;

        public bool WholeNumbers { get { return m_WholeNumbers; } set { if (SetStruct(ref m_WholeNumbers, value)) { SetLow(m_LowValue); SetHigh(m_HighValue); UpdateVisuals(); } } }

        [SerializeField]
        private float m_LowValue;
        public virtual float LowValue
        {
            get
            {
                if (WholeNumbers)
                {
                    return Mathf.Round(m_LowValue);
                }

                return m_LowValue;
            }
            set
            {
                SetLow(value);
            }
        }

        public float NormalizedLowValue
        {
            get
            {
                if (Mathf.Approximately(MinValue, MaxValue))
                {
                    return 0;
                }
                return Mathf.InverseLerp(MinValue, MaxValue, LowValue); //max value may need to be high value here...
            }
            set
            {
                this.LowValue = Mathf.Lerp(MinValue, MaxValue, value);
            }
        }


        [SerializeField]
        private float m_HighValue;
        public virtual float HighValue
        {
            get
            {
                if (WholeNumbers)
                {
                    return Mathf.Round(m_HighValue);
                }

                return m_HighValue;
            }
            set
            {
                SetHigh(value);
            }
        }

        public float NormalizedHighValue
        {
            get
            {
                if (Mathf.Approximately(MinValue, MaxValue))
                {
                    return 0;
                }
                return Mathf.InverseLerp(MinValue, MaxValue, HighValue); //min value may need to be low value here...
            }
            set
            {
                this.HighValue = Mathf.Lerp(MinValue, MaxValue, value);
            }
        }

        /// <summary>
        /// Set the value of the slider without invoking onValueChanged callback.
        /// </summary>
        /// <param name="input">The new value for the slider.</param>
        public virtual void SetValueWithoutNotify(float low, float high)
        {
            SetLow(low, false);
            SetHigh(high, false);
        }

        [Space]

        [SerializeField]
        private RangeSliderEvent m_OnValueChanged = new RangeSliderEvent();

        public RangeSliderEvent OnValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }


        // Private fields

        private Image m_FillImage;
        private Transform m_FillTransform;
        private RectTransform m_FillContainerRect;
        private Transform m_HighHandleTransform;
        private RectTransform m_HighHandleContainerRect;
        private Transform m_LowHandleTransform;
        private RectTransform m_LowHandleContainerRect;

        // The offset from handle position to mouse down position
        private Vector2 m_LowOffset = Vector2.zero;
        // The offset from handle position to mouse down position
        private Vector2 m_HighOffset = Vector2.zero;

        private DrivenRectTransformTracker m_Tracker;

        // This "delayed" mechanism is required for case 1037681.
        private bool m_DelayedUpdateVisuals = false;

        // Size of each step.
        float StepSize { get { return WholeNumbers ? 1 : (MaxValue - MinValue) * 0.1f; } }

        protected RangeSlider()
        { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (WholeNumbers)
            {
                m_MinValue = Mathf.Round(m_MinValue);
                m_MaxValue = Mathf.Round(m_MaxValue);
            }

            if (IsActive())
            {
                UpdateCachedReferences();
                SetLow(m_LowValue, false);
                SetHigh(m_HighValue, false);
                //Update rects since other things might affect them even if value didn't change
                m_DelayedUpdateVisuals = true;
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
            {
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            }
        }
#endif

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
            {
                OnValueChanged.Invoke(LowValue, HighValue);
            }
#endif
        }

        /// <summary>
        /// See ICanvasElement.LayoutComplete
        /// </summary>
        public virtual void LayoutComplete()
        { }

        /// <summary>
        /// See ICanvasElement.GraphicUpdateComplete
        /// </summary>
        public virtual void GraphicUpdateComplete()
        { }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            SetLow(LowValue, false);
            SetHigh(HighValue, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }

        /// <summary>
        /// Update the rect based on the delayed update visuals.
        /// Got around issue of calling sendMessage from onValidate.
        /// </summary>
        protected virtual void Update()
        {
            if (m_DelayedUpdateVisuals)
            {
                m_DelayedUpdateVisuals = false;
                UpdateVisuals();
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
        }

        void UpdateCachedReferences()
        {
            if (m_FillRect && m_FillRect != (RectTransform)transform)
            {
                m_FillTransform = m_FillRect.transform;
                m_FillImage = m_FillRect.GetComponent<Image>();
                if (m_FillTransform.parent != null)
                    m_FillContainerRect = m_FillTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_FillRect = null;
                m_FillContainerRect = null;
                m_FillImage = null;
            }

            if (m_HighHandleRect && m_HighHandleRect != (RectTransform)transform)
            {
                m_HighHandleTransform = m_HighHandleRect.transform;
                if (m_HighHandleTransform.parent != null)
                    m_HighHandleContainerRect = m_HighHandleTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_HighHandleRect = null;
                m_HighHandleContainerRect = null;
            }

            if (m_LowHandleRect && m_LowHandleRect != (RectTransform)transform)
            {
                m_LowHandleTransform = m_LowHandleRect.transform;
                if (m_LowHandleTransform.parent != null)
                {
                    m_LowHandleContainerRect = m_LowHandleTransform.parent.GetComponent<RectTransform>();
                }
            }
            else
            {
                m_LowHandleRect = null;
                m_LowHandleContainerRect = null;
            }
        }
        
        void SetLow(float input)
        {
            SetLow(input, true);
        }

        protected virtual void SetLow(float input, bool sendCallback)
        {
            // Clamp the input
            float newValue = Mathf.Clamp(input, MinValue, HighValue); //clamp between min and High
            if (WholeNumbers)
            {
                newValue = Mathf.Round(newValue);
            }

            // If the stepped value doesn't match the last one, it's time to update
            if (m_LowValue == newValue)
                return;

            m_LowValue = newValue;
            UpdateVisuals();
            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("RangeSlider.lowValue", this);
                m_OnValueChanged.Invoke(newValue, HighValue);
            }
        }

        void SetHigh(float input)
        {
            SetHigh(input, true);
        }

        protected virtual void SetHigh(float input, bool sendCallback)
        {
            // Clamp the input
            float newValue = Mathf.Clamp(input, LowValue, MaxValue); //clamp between min and High
            if (WholeNumbers)
            {
                newValue = Mathf.Round(newValue);
            }

            // If the stepped value doesn't match the last one, it's time to update
            if (m_HighValue == newValue)
                return;

            m_HighValue = newValue;
            UpdateVisuals();
            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("RangeSlider.highValue", this);
                m_OnValueChanged.Invoke(LowValue, newValue);
            }
        }


        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }


        // Force-update the slider. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            m_Tracker.Clear();

            if (m_FillContainerRect != null)
            {
                m_Tracker.Add(this, m_FillRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                //this is where some new magic must happen. Slider just uses a filled image
                //and changes the % of fill. We must move the image anchors to be between the two handles.
                anchorMin[0] = NormalizedLowValue;
                anchorMax[0] = NormalizedHighValue;

                m_FillRect.anchorMin = anchorMin;
                m_FillRect.anchorMax = anchorMax;
            }

            if (m_LowHandleContainerRect != null)
            {
                m_Tracker.Add(this, m_LowHandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin[0] = anchorMax[0] = NormalizedLowValue;
                m_LowHandleRect.anchorMin = anchorMin;
                m_LowHandleRect.anchorMax = anchorMax;
            }

            if (m_HighHandleContainerRect != null)
            {
                m_Tracker.Add(this, m_HighHandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin[0] = anchorMax[0] = NormalizedHighValue;
                m_HighHandleRect.anchorMin = anchorMin;
                m_HighHandleRect.anchorMax = anchorMax;
            }
        }

        // Update the slider's position based on the mouse.
        void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            //this needs to differ from slider in that we have two handles, and need to move the right one. 
            //and if it was neither handle, we will have a seperate case where both handles move uniformly 
            //moving the entire range

            RectTransform clickRect = m_HighHandleRect ?? m_LowHandleRect ?? m_FillContainerRect;
            if (clickRect != null && clickRect.rect.size[0] > 0)
            {
                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, eventData.position, cam, out localCursor))
                {
                    return;
                }
                localCursor -= clickRect.rect.position;

                float lowVal = Mathf.Clamp01((localCursor - m_LowOffset)[0] / clickRect.rect.size[0]);

                NormalizedLowValue = lowVal;
            }
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);

            //HANDLE DRAG EVENTS
            m_LowOffset = m_HighOffset = Vector2.zero;
            Vector2 localMousePos;
            if (m_HighHandleRect != null && RectTransformUtility.RectangleContainsScreenPoint(m_HighHandleRect, eventData.position, eventData.enterEventCamera))
            {
                //dragging the high value handle
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HighHandleRect, eventData.position, eventData.pressEventCamera, out localMousePos))
                {
                    m_HighOffset = localMousePos;
                }
            }
            else if (m_LowHandleRect != null && RectTransformUtility.RectangleContainsScreenPoint(m_LowHandleRect, eventData.position, eventData.enterEventCamera))
            {
                //dragging the low value handle
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_LowHandleRect, eventData.position, eventData.pressEventCamera, out localMousePos))
                {
                    m_LowOffset = localMousePos;
                }
            }
            else
            {
                //outside the handles, move the entire slider along
                UpdateDrag(eventData, eventData.pressEventCamera);
            }
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
            {
                return;
            }
            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        public override void OnMove(AxisEventData eventData)
        {
            //this requires further investigation
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }
    }
}
