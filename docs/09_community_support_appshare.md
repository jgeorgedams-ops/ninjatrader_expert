# 09 — Community, Support & App Share

**Developer forum:** https://discourse.ninjatrader.com/  
**NinjaScript help centre:** https://support.ninjatrader.com/s/topic/0TOHs000000SESYOA4/ninjascript  
**NT8 Ecosystem (third-party apps):** https://ninjatraderecosystem.com/  
**Getting started (support KB):** https://support.ninjatrader.com/s/article/Developer-Guide-Getting-Started-with-NinjaScript

---

## Official Support Channels

| Channel | URL | Best For |
|---|---|---|
| Developer Forum (Discourse) | https://discourse.ninjatrader.com/ | New, active Q&A and discussions |
| NinjaScript Help Centre | https://support.ninjatrader.com/s/topic/0TOHs000000SESYOA4/ninjascript | Searchable KB articles |
| Legacy Support Forum | https://forum.ninjatrader.com/ | Deep archive of historical Q&A (2008–2024) |
| Contact Support | https://developer.ninjatrader.com/contact-us/ | Direct support ticket |

---

## NT8 Ecosystem — Third-Party Apps & Indicators

**URL:** https://ninjatraderecosystem.com/

The NT8 Ecosystem is the official marketplace for third-party indicators, strategies, add-ons, and tools. Many are free; some are commercial.

### User App Share (Free)
The User App Share contains hundreds of **free** community-contributed indicators and strategies. Search by category.

Direct URL: https://ninjatraderecosystem.com/user-app-share-2/

Notable free resources:
- Volume profile indicators
- Order flow tools
- Custom bar types
- Risk management utilities
- Strategy templates

---

## GitHub — Best Community Repositories

NinjaTrader has **no official public GitHub org**. Built-in samples ship inside NT8 at:
```
Documents\NinjaTrader 8\bin\Custom\Samples\
```
Key built-in samples to study: `SampleMACrossover`, `SampleSuperTrend`, `SampleAddOn`.

### GitHub Topic Pages (Browse All)
| Topic | URL | Repos |
|---|---|---|
| `ninjatrader` | https://github.com/topics/ninjatrader | 43 repos |
| `ninjatrader-8` | https://github.com/topics/ninjatrader-8 | 35 repos |
| `ninjascript` | https://github.com/topics/ninjascript | Various |
| `ninjatrader-strategies` | https://github.com/topics/ninjatrader-strategies | Various |

### Top Repos by Category

#### Order Flow & Indicators
| Repo | Stars | URL | What to Learn |
|---|---|---|---|
| `WaleeTheRobot/order-flow-bot` | ★200 | https://github.com/WaleeTheRobot/order-flow-bot | ATM integration, volumetric data, semi-auto patterns |
| `trading-code/ninjatrader-freeorderflow` | ★127 | https://github.com/trading-code/ninjatrader-freeorderflow | Free order flow indicators, production-quality code |
| `WaleeTheRobot/open-auto-atr` | ★24 | https://github.com/WaleeTheRobot/open-auto-atr | ATR-based range indicators |

#### Strategy Frameworks
| Repo | Stars | URL | What to Learn |
|---|---|---|---|
| `MicroTrendsLtd/NinjaTrader8` | ★100 | https://github.com/MicroTrendsLtd/NinjaTrader8 | Unmanaged mode engine, hybrid semi-auto, advanced C# |
| `gjh33/SmartStrategies` | ★27 | https://github.com/gjh33/SmartStrategies | Clean managed-mode framework, tick replay handling |
| `r-yabyab/Custom-NinjaScript-Files` | — | https://github.com/r-yabyab/Custom-NinjaScript-Files | Backtest/live script pairs, bar replay vs tick replay |
| `ayb/ninjatrader-automated-trading-strategy` | — | https://github.com/ayb/ninjatrader-automated-trading-strategy | Complete inside bar strategy, ATR stops, dual-instrument |

#### Python / External Bridges
| Repo | Stars | URL | What to Learn |
|---|---|---|---|
| `TheSnowGuru/CSharpNinja-Python-NT8` | — | https://github.com/TheSnowGuru/CSharpNinja-Python-NinjaTrader8-trading-api-connector-drag-n-drop | Python↔NT8 DLL bridge |
| `moguli/zorro-ninjatrader` | — | https://github.com/moguli/zorro-ninjatrader | ATI DLL integration from external language |

#### Utilities & Add-ons
| Repo | Stars | URL | What to Learn |
|---|---|---|---|
| `eugeneilyin/nrdtocsv` | ★45 | https://github.com/eugeneilyin/nrdtocsv | Add-on development, NRD→CSV export for data analysis |
| `WaleeTheRobot/ninja-trader-discord-messenger` | ★25 | https://github.com/WaleeTheRobot/ninja-trader-discord-messenger | Add-on with external HTTP webhook calls |
| `sibvic/nt8-templates` | — | https://github.com/sibvic/nt8-templates | Boilerplate templates and snippets |
| `jDoom/NinjaScripts` | ★29 | https://github.com/jDoom/NinjaScripts | NT7→NT8 migration examples |

#### Testing Frameworks
| Repo | Stars | URL | What to Learn |
|---|---|---|---|
| `samuelcaldas/NinjaTrader.UnitTest` | — | https://github.com/samuelcaldas/NinjaTrader.UnitTest | Unit testing NinjaScript — Assert API, TestCase pattern |
| `Abattia/NinjaTrader8_UnitTests` | — | https://github.com/Abattia/NinjaTrader8_UnitTests | Lightweight DIY test harness using OnBarUpdate |

---

## How to Install a GitHub Script

1. Clone or download the repository `.zip`
2. For single `.cs` files:
   - Copy `.cs` file to `Documents\NinjaTrader 8\bin\Custom\Indicators\` (or `Strategies\`)
   - Open NinjaScript Editor → F5 to compile
3. For `.zip` release packages:
   - NT8 → Control Center → Tools → Import → NinjaScript...
   - Select the `.zip` file
4. Check Control Center → Log tab for any import errors

---

## Finding Answers — Where to Search

### For Coding Questions
1. **NT8 Developer forum** (discourse.ninjatrader.com) — most current, active
2. **Legacy forum** (forum.ninjatrader.com) — massive archive, search deeply
3. **NT8 official docs** (developer.ninjatrader.com/docs/desktop) — authoritative reference
4. **Built-in source code** — read the indicator/strategy source in NinjaScript Editor

### For Strategy Ideas
1. NT8 User App Share (ninjatraderecosystem.com)
2. GitHub topic pages (see above)
3. NT8 forum file sharing section

### Search Tips
- Include "NT8" or "NinjaTrader 8" in web searches — NT7 code won't compile directly
- Many Stack Overflow answers are for NT7 — verify the version
- The legacy forum has thread-specific permalinks — bookmark useful threads
- Forum username "NinjaTrader_Ray" and "NinjaTrader_ChelseaB" are official NT8 support staff

---

## Community Reference Links

| Resource | URL |
|---|---|
| Developer Forum (Discourse) | https://discourse.ninjatrader.com/ |
| NinjaScript help centre | https://support.ninjatrader.com/s/topic/0TOHs000000SESYOA4/ninjascript |
| NT8 Ecosystem / User App Share | https://ninjatraderecosystem.com/ |
| Getting started KB article | https://support.ninjatrader.com/s/article/Developer-Guide-Getting-Started-with-NinjaScript |
| GitHub: ninjatrader topic | https://github.com/topics/ninjatrader |
| GitHub: ninjatrader-8 topic | https://github.com/topics/ninjatrader-8 |
