using DerivTycoon.Buildings;
using DerivTycoon.Core;
using DerivTycoon.Trading;
using UnityEngine;
using UnityEngine.UI;

namespace DerivTycoon.UI
{
    public class BuildingInfoUI : MonoBehaviour
    {
        public static BuildingInfoUI Instance { get; private set; }

        [Header("Panel")]
        public GameObject PanelRoot;

        [Header("Display")]
        public Text BuildingNameText;
        public Text SymbolText;
        public Text EntryPriceText;
        public Text CurrentPriceText;
        public Text PnLText;
        public Text StakeText;

        [Header("Actions")]
        public Button SellButton;
        public Button CloseButton;

        private BuildingController _currentBuilding;
        private Trade _currentTrade;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SellButton?.onClick.AddListener(OnSell);
            CloseButton?.onClick.AddListener(Hide);
            Hide();
        }

        private void OnEnable()  => EventBus.OnTickReceived += OnTickReceived;
        private void OnDisable() => EventBus.OnTickReceived -= OnTickReceived;

        private void OnTickReceived(API.Models.TickData tick)
        {
            if (_currentTrade == null || tick.symbol != _currentTrade.Symbol) return;
            UpdatePriceDisplay();
        }

        public void Show(BuildingController building)
        {
            _currentBuilding = building;
            _currentTrade = building.Trade;

            if (BuildingNameText != null)
                BuildingNameText.text = building.CommodityName;

            if (SymbolText != null)
                SymbolText.text = _currentTrade?.Symbol ?? "";

            if (StakeText != null && _currentTrade != null)
                StakeText.text = $"Stake: ${_currentTrade.Stake:F2}  ×{_currentTrade.Multiplier}";

            UpdatePriceDisplay();
            PanelRoot?.SetActive(true);
        }

        private void UpdatePriceDisplay()
        {
            if (_currentTrade == null) return;

            if (EntryPriceText != null)
                EntryPriceText.text = $"Entry:   ${_currentTrade.EntryPrice:F5}";

            if (CurrentPriceText != null)
                CurrentPriceText.text = $"Current: ${_currentTrade.CurrentPrice:F5}";

            if (PnLText != null)
            {
                float pnl = _currentTrade.PnL;
                float pct = _currentTrade.PnLPercent;
                string sign = pnl >= 0 ? "+" : "";
                PnLText.text = $"P&L: {sign}{pnl:F2} ({sign}{pct:F3}%)";
                PnLText.color = pnl >= 0 ? new Color(0.25f, 0.95f, 0.35f) : new Color(1f, 0.3f, 0.2f);
            }

            if (SellButton != null)
                SellButton.gameObject.SetActive(_currentTrade.IsActive);
        }

        private void OnSell()
        {
            if (_currentTrade == null) return;
            TradeManager.Instance?.CloseTrade(_currentTrade.Id);
            Hide();
        }

        public void Hide()
        {
            _currentBuilding = null;
            _currentTrade = null;
            PanelRoot?.SetActive(false);
        }
    }
}
