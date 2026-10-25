using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Verso
{
    /// <summary>Feedback dos botões: cresce no hover/seleção, afunda no clique, toca o som de UI.</summary>
    public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float speed = 18f;

        private float target = 1f;
        private float baseScale = -1f;
        private bool hovered, selected, pressed;

        private void Awake() { baseScale = transform.localScale.x; }

        private void OnEnable()
        {
            if (baseScale < 0f) baseScale = transform.localScale.x;
            hovered = selected = pressed = false; target = 1f;
            transform.localScale = new Vector3(baseScale, baseScale, 1f);
        }

        private void Update()
        {
            float goal = target * baseScale;
            float s = Mathf.Lerp(transform.localScale.x, goal, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }

        private void Refresh()
        {
            target = pressed ? pressScale : (hovered || selected ? hoverScale : 1f);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            hovered = true; Refresh(); UIAudio.Hover();
            // o mouse manda no destaque: tira a seleção de teclado de outro botão
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null && es.currentSelectedGameObject != gameObject)
                es.SetSelectedGameObject(null);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hovered = false; pressed = false; Refresh();
            // sem isso o botão continua "selecionado" (grande e clarinho) depois que o mouse sai
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject == gameObject) es.SetSelectedGameObject(null);
        }
        public void OnPointerDown(PointerEventData e) { pressed = true; Refresh(); }
        public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }
        public void OnSelect(BaseEventData e)
        {
            selected = true; Refresh();
            // navegação por teclado/controle também faz o som (menos a seleção automática ao abrir a tela)
            if (Time.timeSinceLevelLoad > 0.3f && !(e is PointerEventData)) UIAudio.Hover();
        }
        public void OnDeselect(BaseEventData e) { selected = false; Refresh(); }
        public void OnSubmit(BaseEventData e) { UIAudio.Click(); }
        public void OnPointerClick(PointerEventData e) { UIAudio.Click(); }
    }
}
