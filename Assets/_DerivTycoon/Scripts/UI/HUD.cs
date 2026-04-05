using DerivTycoon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerivTycoon.UI
{
    public class HUD : MonoBehaviour
    {
        [Header("References")]
        public Text BalanceText;
        public Button NewTradeButton;
        public TradePanelUI TradePanel;

        private void OnEnable()
        {
            EventBus.OnBalanceChanged += UpdateBalance;
            EventBus.OnGameStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnBalanceChanged -= UpdateBalance;
            EventBus.OnGameStateChanged -= OnStateChanged;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                UpdateBalance(GameManager.Instance.Balance);

            NewTradeButton?.onClick.AddListener(OnNewTradeClicked);
        }

        private void UpdateBalance(float balance)
        {
            if (BalanceText != null)
                BalanceText.text = $"${balance:N2}";
        }

        private void OnNewTradeClicked()
        {
            TradePanel?.Show();
        }

        private void OnStateChanged(GameState state)
        {
            if (NewTradeButton != null)
                NewTradeButton.gameObject.SetActive(state != GameState.Placement);
        }
    }
}
