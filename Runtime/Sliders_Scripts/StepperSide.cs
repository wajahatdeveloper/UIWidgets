using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{

    [RequireComponent(typeof(Selectable))]
    public class StepperSide :
        UIBehaviour,
        IPointerClickHandler,
        ISubmitHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        Selectable _button;
        Stepper _stepper;

        Selectable button { get { return _button; } }

        Stepper stepper { get { return _stepper; } }

        bool leftmost { get { return button == stepper.sides[0]; } }

        internal Sprite cutSprite;

        protected StepperSide()
        { }

        protected override void Awake()
        {
            base.Awake();
            _button = GetComponent<Selectable>();
            _stepper = GetComponentInParent<Stepper>();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Press();
            AdjustSprite(false);
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();
            AdjustSprite(true);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            AdjustSprite(false);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            AdjustSprite(true);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            AdjustSprite(false);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            AdjustSprite(false);
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            AdjustSprite(false);
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            AdjustSprite(true);
        }

        private void Press()
        {
            if (!button.IsActive() || !button.IsInteractable())
                return;

            if (leftmost)
            {
                stepper.StepDown();
            }
            else
            {
                stepper.StepUp();
            }
        }

        private void AdjustSprite(bool restore)
        {
            var image = button.image;
            if (!image || image.overrideSprite == cutSprite)
                return;

            if (restore)
                image.overrideSprite = cutSprite;
            else
                image.overrideSprite = Stepper.CutSprite(image.overrideSprite, leftmost);
        }
    }
}