# 07 — Add-on & Platform Extension Development

**Add-on overview:** https://ninjatrader.com/support/helpguides/nt8/addon_development_overview.htm  
**Add-on reference:** https://developer.ninjatrader.com/docs/desktop/add_on  
**Chart style reference:** https://developer.ninjatrader.com/docs/desktop/chart_style  
**SuperDOM column reference:** https://developer.ninjatrader.com/docs/desktop/superdom_column  
**Distribution guide:** https://developer.ninjatrader.com/docs/desktop/distribution  
**User-based vendor licensing:** https://developer.ninjatrader.com/docs/desktop/user_based_licensing

---

## What an Add-on Is

An Add-on is a NinjaScript class that inherits from `AddOnBase` and gets access to platform-wide functionality that regular indicators and strategies don't have:

- Create **custom windows** and UI panels (using WPF/XAML)
- Modify existing NT8 windows (charts, DOM, etc.)
- Subscribe to **live market data** across any instrument
- Access **account and position** information globally
- Respond to **connection events** and **order events** platform-wide
- Add functionality to charts, SuperDOM, and Market Analyzer

---

## Add-on vs Indicator vs Strategy

| Capability | Indicator | Strategy | Add-on |
|---|---|---|---|
| Chart calculation | Yes | Yes | Limited |
| Place orders | No | Yes | Via Account API |
| Custom window | No | No | Yes |
| Modify NT8 UI | No | No | Yes |
| Global market data | No | No | Yes |
| All account access | No | No | Yes |
| Connection events | No | No | Yes |
| Compiles in NS Editor | Yes | Yes | Yes |
| Works without chart | No | No | Yes |

---

## Add-on Base Structure

```csharp
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.AddOns
{
    public class MyAddOn : AddOnBase
    {
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name        = "MyAddOn";
                Description = "What this add-on does";
            }
            else if (State == State.Configure)
            {
                // Subscribe to platform events
                Connection.ConnectionStatusUpdate += OnConnectionStatusUpdate;
                Account.AccountItemUpdate         += OnAccountItemUpdate;
            }
            else if (State == State.Terminated)
            {
                // MUST unsubscribe — ghost handlers cause serious issues
                Connection.ConnectionStatusUpdate -= OnConnectionStatusUpdate;
                Account.AccountItemUpdate         -= OnAccountItemUpdate;
            }
        }

        private void OnConnectionStatusUpdate(object sender, ConnectionStatusEventArgs e)
        {
            if (e.Status == ConnectionStatus.Connected)
                Print("Connected: " + e.Connection.Name);
        }

        private void OnAccountItemUpdate(object sender, AccountItemEventArgs e)
        {
            // e.Account, e.AccountItem (CashValue, OpenPositionPnL, etc.), e.Value
            Print(string.Format("Account update: {0} = {1}", e.AccountItem, e.Value));
        }
    }
}
```

---

## Creating a Custom Window

Add-ons can launch standalone windows using NT8's built-in WPF window helpers. The window will appear in NT8's workspace and can be saved/restored.

```csharp
// In your AddOn class — launch the window
private MyAddOnWindow _window;

// Call this from a menu item or connection event
private void ShowMyWindow()
{
    if (_window == null || _window.IsDisposed)
    {
        // NTWindow is NT8's base window class
        _window = new MyAddOnWindow();
        _window.Show();
    }
    else
    {
        _window.Activate();
    }
}
```

### Window Class (WPF + XAML)
```csharp
// MyAddOnWindow.xaml.cs
using NinjaTrader.Gui;

public class MyAddOnWindow : NTWindow
{
    public MyAddOnWindow()
    {
        Caption = "My Custom Window";
        Width   = 400;
        Height  = 300;
        
        // Set WPF content
        Content = new MyWindowContent();  // WPF UserControl
    }
}
```

```xml
<!-- MyWindowContent.xaml (WPF UserControl) -->
<UserControl x:Class="NinjaTrader.NinjaScript.AddOns.MyWindowContent"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Margin="10">
        <TextBlock Text="Hello from my Add-on!" FontSize="16" />
        <Button Content="Place Order" Click="OnPlaceOrderClick" Margin="0,10,0,0"/>
        <DataGrid x:Name="PositionsGrid" AutoGenerateColumns="True" />
    </StackPanel>
</UserControl>
```

---

## Accessing Market Data in an Add-on

```csharp
// Subscribe to real-time data for any instrument
private MarketSubscription _subscription;

protected override void OnStateChange()
{
    if (State == State.Configure)
    {
        // Subscribe to last price, bid, ask for ES front month
        Instrument instrument = Instrument.GetInstrument("ES 03-25");
        _subscription = new MarketSubscription(instrument, this);
        _subscription.MarketDataUpdate += OnMarketDataUpdate;
    }
    else if (State == State.Terminated)
    {
        if (_subscription != null)
        {
            _subscription.MarketDataUpdate -= OnMarketDataUpdate;
            _subscription.Dispose();
        }
    }
}

private void OnMarketDataUpdate(object sender, MarketDataEventArgs e)
{
    switch (e.MarketDataType)
    {
        case MarketDataType.Last:
            Print("Last: " + e.Price + " x " + e.Volume);
            break;
        case MarketDataType.Bid:
            Print("Bid: " + e.Price);
            break;
        case MarketDataType.Ask:
            Print("Ask: " + e.Price);
            break;
    }
}
```

---

## Accessing Account Data in an Add-on

```csharp
// Get all accounts
foreach (Account acct in Account.All)
{
    Print("Account: " + acct.Name + " Cash: " + acct.Get(AccountItem.CashValue, Currency.UsDollar));
}

// Get positions
foreach (Position pos in Account.All[0].Positions)
{
    Print("Position: " + pos.Instrument.FullName + " Qty: " + pos.Quantity +
          " AvgPrice: " + pos.AveragePrice);
}

// Place an order from an Add-on
Account.All[0].CreateOrder(
    Instrument.GetInstrument("MES 03-25"),
    OrderAction.Buy,
    OrderType.Market,
    null,   // ATM strategy name
    1,      // quantity
    0,      // limit price
    0,      // stop price
    TimeInForce.Day,
    "MyOrder",
    ""
);
```

---

## Adding a Menu Item to NT8

```csharp
// Override OnStateChange → State.Configure to add to NT8's Tools menu
protected override void OnStateChange()
{
    if (State == State.Configure)
    {
        // Create a menu item in NT8's Tools menu
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var menuItem = new System.Windows.Controls.MenuItem { Header = "My Add-on Window" };
            menuItem.Click += (s, e) => ShowMyWindow();
            
            // Add to NT8's main menu (Tools)
            var mainMenu = (System.Windows.Controls.Menu)
                System.Windows.Application.Current.MainWindow
                    .FindName("mainMenu");
            mainMenu?.Items.Add(menuItem);
        });
    }
}
```

---

## Custom Bars Type

Create a completely custom bar type (e.g. custom range bars, volume bars, proprietary aggregation):

Inherits from `BarsType`. Reference: https://developer.ninjatrader.com/docs/desktop/bars_type

---

## Custom Chart Style

Create a custom chart rendering style (e.g. Heiken Ashi variant, custom candle colouring, point-and-figure):

Inherits from `ChartStyle`. Uses **SharpDX** (DirectX .NET wrapper) for high-performance rendering.  
Reference: https://developer.ninjatrader.com/docs/desktop/chart_style  
SharpDX docs: https://developer.ninjatrader.com/docs/desktop/sharpdx

---

## Custom SuperDOM Column

Add a custom column to the SuperDOM order ladder:

Inherits from `SuperDOMColumn`. Reference: https://developer.ninjatrader.com/docs/desktop/superdom_column

---

## Custom Market Analyzer Column

Add a custom scanner column to the Market Analyzer:

Inherits from `MarketAnalyzerColumn`. Reference: https://developer.ninjatrader.com/docs/desktop/market_analyzer_column

---

## Distribution — Packaging for Others

### Import/Export (.zip)
NT8 scripts are distributed as `.zip` files. Export via:  
NinjaScript Editor → right-click script → Export NinjaScript

The `.zip` contains compiled `.dll` and source `.cs` (if you include source).

### Protecting Source Code
To distribute without source:
1. Compile in NT8 editor
2. Export — uncheck "Include source code"
3. Recipients get the `.dll` only

For commercial distribution with licensing:
- See user-based vendor licensing docs below
- The NT8 ecosystem vendor portal manages activation keys

### Download Add-on Framework Starter
NT8 provides a downloadable NinjaScript-compatible `.zip` with the basic add-on skeleton:  
Available at: https://ninjatrader.com/support/helpguides/nt8/addon_development_overview.htm

---

## Add-on Reference Links

| Resource | URL |
|---|---|
| Add-on overview & framework download | https://ninjatrader.com/support/helpguides/nt8/addon_development_overview.htm |
| Add-on API reference | https://developer.ninjatrader.com/docs/desktop/add_on |
| Chart style reference | https://developer.ninjatrader.com/docs/desktop/chart_style |
| SuperDOM column reference | https://developer.ninjatrader.com/docs/desktop/superdom_column |
| SharpDX rendering reference | https://developer.ninjatrader.com/docs/desktop/sharpdx |
| Distribution guide | https://developer.ninjatrader.com/docs/desktop/distribution |
| User-based vendor licensing | https://developer.ninjatrader.com/docs/desktop/user_based_licensing |
