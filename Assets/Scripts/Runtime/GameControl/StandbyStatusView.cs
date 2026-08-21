using System.Collections;
using TMPro;
using UnityEngine;

namespace NCKH.VR.GameControl
{
    [DisallowMultipleComponent]
    public sealed class StandbyStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;

        private GameController boundController;
        private Coroutine bindCoroutine;

        private void OnEnable()
        {
            SetText("Đang chờ bộ điều khiển trò chơi...");
            if (!TryBindController())
                bindCoroutine = StartCoroutine(BindWhenAvailable());
        }

        private void OnDisable()
        {
            if (bindCoroutine != null)
            {
                StopCoroutine(bindCoroutine);
                bindCoroutine = null;
            }

            UnbindController();
        }

        private IEnumerator BindWhenAvailable()
        {
            while (isActiveAndEnabled && !TryBindController())
                yield return null;

            bindCoroutine = null;
        }

        private bool TryBindController()
        {
            GameController controller = GameController.Instance;
            if (controller == null)
                return false;

            if (boundController != controller)
            {
                UnbindController();
                boundController = controller;
                boundController.StatusChanged += SetText;
            }

            SetText(controller.IsConfigured
                ? "Đang kết nối đến máy chủ..."
                : "Chưa cấu hình máy chủ");
            return true;
        }

        private void UnbindController()
        {
            if (boundController != null)
                boundController.StatusChanged -= SetText;
            boundController = null;
        }

        private void SetText(string value)
        {
            if (statusText != null)
                statusText.text = value ?? string.Empty;
        }
    }
}
