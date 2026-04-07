using DerivTycoon.City;
using DerivTycoon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerivTycoon.UI
{
    public class TradePanelUI : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject PanelRoot;

        [Header("Commodity Buttons")]
        public Button GoldButton;
        public Button SilverButton;
        public Button VolatilityButton;

        [Header("Info")]
        public Text SelectedCommodityText;
        public Text LivePriceText;

        [Header("Actions")]
        public Button ConfirmButton;
        public Button CancelButton;

        private string _selectedSymbol;
        private Button _activeButton;

        private static readonly Color SelectedColor = new Color(0.2f, 0.7f, 0.3f);
        private static readonly Color DefaultColor = new Color(0.15f, 0.15f, 0.2f);

        private void Start()
        {
            GoldButton?.onClick.AddListener(() => SelectCommodity("frxXAUUSD", GoldButton));
            SilverButton?.onClick.AddListener(() => SelectCommodity("frxXAGUSD", SilverButton));
            VolatilityButton?.onClick.AddListener(() => SelectCommodity("1HZ100V", VolatilityButton));

            ConfirmButton?.onClick.AddListener(OnConfirm);
            CancelButton?.onClick.AddListener(Hide);

            SelectCommodity("1HZ100V", VolatilityButton);
            Hide();
        }

        private void OnEnable()
        {
            EventBus.OnTickReceived += OnTickReceived;
        }

        private void OnDisable()
        {
            EventBus.OnTickReceived -= OnTickReceived;
        }

        private void OnTickReceived(API.Models.TickData tick)
        {
            if (tick.symbol != _selectedSymbol) return;
            if (LivePriceText != null)
                LivePriceText.text = $"${tick.quote:F5}";
        }

        private void SelectCommodity(string symbol, Button btn)
        {
            _selectedSymbol = symbol;

            if (_activeButton != null)
                SetButtonColor(_activeButton, DefaultColor);

            _activeButton = btn;
            if (btn != null) SetButtonColor(btn, SelectedColor);

            var name = GameManager.Instance?.GetCommodityName(symbol) ?? symbol;
            if (SelectedCommodityText != null)
                SelectedCommodityText.text = name;

            var price = API.MarketDataStore.Instance?.GetLatestPrice(symbol) ?? 0f;
            if (LivePriceText != null)
                LivePriceText.text = price > 0 ? $"${price:F5}" : "Market closed";
        }

        private void SetButtonColor(Button btn, Color color)
        {
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        private void OnConfirm()
        {
            if (string.IsNullOrEmpty(_selectedSymbol)) return;
            Hide();
            BuildingPlacer.Instance?.BeginPlacement(_selectedSymbol);
        }

        public void Show()
        {
            PanelRoot?.SetActive(true);
        }

        public void Hide()
        {
            PanelRoot?.SetActive(false);
        }
    }
}
