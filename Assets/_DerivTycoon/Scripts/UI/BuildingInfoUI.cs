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

        [Header("Production")]
        public Text VaultText;
        public Text CountdownText;
        public Text WinStreakText;
        public Button ToggleProductionButton;
        public Text ToggleProductionButtonText;

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
            ToggleProductionButton?.onClick.AddListener(OnToggleProduction);
            Hide();
        }

        private void OnEnable()  => EventBus.OnTickReceived += OnTickReceived;
        private void OnDisable() => EventBus.OnTickReceived -= OnTickReceived;

        private void OnTickReceived(API.Models.TickData tick)
        {
            if (_currentTrade == null || tick.symbol != _currentTrade.Symbol) return;
            UpdateDisplay();
        }

        // Update countdown every frame while panel is open
        private void Update()
        {
            if (_currentBuilding == null || _currentTrade == null) return;
            if (_currentTrade.ProductionEnabled)
                UpdateCountdown();
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

            UpdateDisplay();
            PanelRoot?.SetActive(true);
        }

        private void UpdateDisplay()
        {
            UpdatePriceDisplay();
            UpdateProductionDisplay();
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
            {
                SellButton.gameObject.SetActive(_currentTrade.IsActive);
                SellButton.interactable = !_currentTrade.ProductionEnabled;
                var sellImg = SellButton.GetComponent<UnityEngine.UI.Image>();
                if (sellImg != null)
                    sellImg.color = _currentTrade.ProductionEnabled
                        ? new Color(0.4f, 0.07f, 0.05f)   // dimmed while producing
                        : new Color(0.7f, 0.12f, 0.08f);  // full red when available
            }
        }

        private void UpdateProductionDisplay()
        {
            if (_currentTrade == null) return;

            if (VaultText != null)
                VaultText.text = $"Vault: ${_currentTrade.VaultBalance:F2}";

            if (WinStreakText != null)
                WinStreakText.text = _currentTrade.WinStreak > 0
                    ? $"Streak: {_currentTrade.WinStreak}"
                    : $"Cycles: {_currentTrade.TotalCyclesRun}";

            if (ToggleProductionButtonText != null)
                ToggleProductionButtonText.text = _currentTrade.ProductionEnabled
                    ? "Stop Production"
                    : "Start Production";

            if (ToggleProductionButton != null)
                ToggleProductionButton.gameObject.SetActive(_currentTrade.IsActive);

            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (CountdownText == null || _currentBuilding == null) return;

            if (_currentTrade != null && _currentTrade.ProductionEnabled)
            {
                float secs = _currentBuilding.CycleCountdownSeconds;
                int mins = Mathf.FloorToInt(secs / 60f);
                int s    = Mathf.FloorToInt(secs % 60f);
                CountdownText.text = mins > 0 ? $"Next: {mins}m {s:D2}s" : $"Next: {s}s";
            }
            else
            {
                CountdownText.text = "Production: OFF";
            }
        }

        private void OnToggleProduction()
        {
            _currentBuilding?.ToggleProduction();
            UpdateProductionDisplay();
        }

        private void OnSell()
        {
            if (_currentTrade == null || _currentBuilding == null) return;
            // Let BuildingController handle cycle-first logic
            _currentBuilding.TrySellMine();
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
