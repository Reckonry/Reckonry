# Quadro RW analysis for crypto-assets

This analysis uses only the official PDF files stored locally in
`docs/sources/agenzia-entrate/`.

Page numbers below are PDF page numbers as opened from the local files. This
document is an engineering analysis for LedgerForge. It is not tax, legal,
accounting, or financial advice.

## Source files used

| Source file | Relevant pages | Purpose |
| --- | ---: | --- |
| `PF2_modello_2026_agg 13 05 2026.pdf` | 10 | Official Quadro RW form layout and column labels. |
| `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 48-54 | Official Quadro RW instructions for monitoring, IVIE, IVAFE, and crypto-assets tax. |
| `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 66 | Investment/activity code table; code 21 is crypto-assets. |
| `PF2_modello_2026_agg 13 05 2026.pdf` | 7-8 | Official Quadro RT crypto capital gains form layout. |
| `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 38-39 | Official Quadro RT crypto capital gains instructions. |
| `PF1_modello_2026_agg 13 05 2026.pdf` | 14 | RX summary rows for RT, RW IVIE/IVAFE, and RW crypto-assets tax. |
| `PF1_istruzioni_2026_agg 28 05 2026.pdf` | 162 | RX instructions mapping RW and RT results into RX. |

## Scope separation

### RW monitoring

RW monitoring concerns the disclosure of foreign investments, foreign financial
assets, and crypto-assets held through wallets, digital accounts, or other
storage systems. The PF2 instructions state that Quadro RW is used for fiscal
monitoring by Italian resident individuals who hold such assets. They also state
that RW can be required even if assets were fully divested during the tax
period.

For LedgerForge, RW monitoring is a reporting projection from the canonical
ledger. It should not mutate ledger events and should preserve traceability from
each report line back to ledger events and original source references.

### Crypto-asset tax / IC

The same RW section also contains the tax on crypto-assets, shown on the form as
`IC` and summarized in row RW8. The PF2 instructions state that this tax applies
to crypto-assets capable of producing income under article 67, paragraph 1,
letter c-sexies of TUIR, where stamp duty was not applied, and that the rate is
2 per mille / 0.20%.

For LedgerForge, IC support must be separate from monitoring support. The engine
may calculate explainable draft values when all required inputs are available,
but it must not invent missing market values, tax credits, ownership
percentages, or prior-year declaration values.

### RT capital gains

Quadro RT is separate from Quadro RW. PF2 model pages 7-8 and PF2 instructions
pages 38-39 describe RT section V-A and V-B for crypto capital gains and related
substitute tax. PF1 instructions page 162 maps RT90 and RT112 into RX21, while
RW8 maps into RX27.

RT requires proceeds, acquisition costs, normal values for exchanges/permutations,
loss carryforwards, prior declaration credits, and in some cases redetermined
values. This is capital-gains logic and is outside an RW monitoring snapshot.
LedgerForge must keep RT calculations separate from RW reporting.

## Official crypto-specific rules identified

- Crypto-assets use investment/activity code `21` in RW column 3. Source:
  `PF2_istruzioni_2026_agg 13 05 2026.pdf`, page 66.
- For virtual currencies/crypto, the foreign state code in RW column 4 is not
  mandatory. Source: `PF2_istruzioni_2026_agg 13 05 2026.pdf`, page 51.
- The IC base is the value of crypto-assets at the end of each calendar year as
  detected from the exchange platform where the asset was acquired. If that is
  not possible, the value may be taken from an analogous platform where the same
  asset is traded or from specialized market-value sites. If no such value is
  available, acquisition cost is used. If the asset is no longer held on 31
  December, the value at the end of the holding period is used. Source:
  `PF2_istruzioni_2026_agg 13 05 2026.pdf`, page 50.
- IC is due in proportion to holding days and ownership percentage. Source:
  `PF2_istruzioni_2026_agg 13 05 2026.pdf`, page 50.
- Column 33 calculates IC at 0.20% on column 8, ownership quota, and holding
  period. Column 34 is column 33 less the allowed foreign patrimonial tax credit
  in column 12. Source: `PF2_istruzioni_2026_agg 13 05 2026.pdf`, page 53.

## Ambiguities and engineering decisions needed

- The official instructions say the foreign state code is not mandatory for
  virtual currencies, but they do not define a canonical replacement value for
  exchange-held crypto, self-custody, or multi-jurisdiction custody. Official
  guidance is ambiguous.
- The official instructions define a hierarchy for crypto valuation sources, but
  they do not define how software should choose a source when the purchase
  exchange, a later custody venue, and market-data sites disagree. Official
  guidance is ambiguous.
- The official instructions permit aggregation for homogeneous financial products
  with the same investment code and foreign state. For crypto-assets, where state
  is not mandatory and individual assets can have different market behavior,
  whether aggregation should be per asset, per venue, or broader is not fully
  specified. Official guidance is ambiguous.
- The official instructions allow a credit for foreign patrimonial tax on the
  same crypto-assets, but determining the "state" for decentralized assets or
  self-custody can be unclear. Official guidance is ambiguous.
- LedgerForge must not fill ambiguous values by default. It should emit warnings
  and require user or professional input.

## RW monitoring fields: RW1-RW5 columns 1-21

| Field name | Column number | Legal meaning | Official source file | Page number | Required value | Calculation rule | LedgerForge source | Status | Implementation complexity | Missing information |
| --- | ---: | --- | --- | ---: | --- | --- | --- | --- | --- | --- |
| Codice titolo | 1 | Legal title under which the asset is held. Codes include ownership, usufruct, bare ownership, or other rights. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 51 | Usually `1` for direct ownership, unless taxpayer context differs. | User-selected code; not derivable from exchange transactions alone. | User profile / report configuration. | Not implemented. | Low | Taxpayer legal title. |
| Tipo possesso | 2 | Indicates delegated account movement authority or beneficial owner status. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 51 | Blank, `1`, or `2` depending on taxpayer relationship. | User-selected code; not derivable from ledger postings alone. | User profile / report configuration. | Not implemented. | Low | Whether taxpayer is delegate or beneficial owner. |
| Codice individuaz. bene | 3 | Identifies the foreign investment or financial activity type. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 51, 66 | `21` for crypto-assets. | Map crypto holdings to official code 21. | Canonical ledger asset classification. | Partially available; official RW mapping not implemented. | Low | Confirmation that all included ledger assets are crypto-assets. |
| Codice Stato estero | 4 | Foreign state code from the official country table. Not mandatory for virtual currencies. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 51 | Blank or country code depending on professional interpretation. | For crypto, do not infer by default. Require configuration if populated. | Exchange/custody metadata, if available. | Not implemented. | Medium | Country treatment for exchange custody, self-custody, and multi-venue assets. Official guidance is ambiguous. |
| Quota di possesso | 5 | Ownership percentage of the foreign investment/activity. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 51 | Ownership percentage. Common default may be 100%, but must be user-confirmed. | Apply user-provided ownership share to values and IC calculations. | User profile / report configuration. | Not implemented. | Low | Co-ownership and beneficial ownership details. |
| Criterio determin. valore | 6 | Valuation criterion code: market, nominal, redemption, acquisition cost, cadastral, succession/other acts. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 51-52 | For crypto, likely market value when available, acquisition cost when required by the official fallback. | Select from official hierarchy: market/exchange/platform value first; acquisition cost only if required fallback applies. | Pricing provider, exchange statement values, acquisition cost ledger. | Not implemented. | High | Reliable valuation source, fallback evidence, and user/professional approval. |
| Valore iniziale | 7 | Value at beginning of tax period or first day of holding. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | EUR value at 1 Jan or first holding day. | Quantity held at start/first holding day multiplied by accepted valuation source, or acquisition cost fallback where officially applicable. | RW snapshot opening quantity; future pricing/valuation evidence. | Partially available for quantity only. | High | Official valuation source, FX handling, acquisition cost fallback evidence. |
| Valore finale | 8 | Value at end of tax period or end of holding period. This is the base for IC in column 33. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 50, 52-53 | EUR value at 31 Dec, or end of holding period if no longer held. | Apply official crypto valuation hierarchy. Use value at end of holding period for assets no longer held at 31 Dec. | RW snapshot closing quantity; RW value inputs; future pricing/valuation evidence. | Partially available for quantity and some imported EUR values; official valuation not complete. | High | End-of-year or disposal-date valuation source for every asset. |
| Valore massimo c/c paesi non collaborativi | 9 | Maximum amount reached during the tax period for current accounts/savings books in non-cooperative countries. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Not applicable to crypto-assets unless a professional maps a product differently. | Do not populate for crypto code 21 by default. | None for crypto. | Out of scope for crypto. | Low | Non-crypto account classification if supported later. |
| Giorni IVAFE-IC | 10 | Holding days for assets subject to IVAFE or IC. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 50, 52 | Number of days held when IC is due. | Determine holding intervals from ledger balances. Count days with positive holdings, adjusted for ownership. | Canonical ledger postings and timestamps. | Not implemented as official RW field; snapshot reports exist. | Medium | Lot-independent day-count method for aggregate asset balances; handling intraday zero crossings. |
| Mesi IVIE | 11 | Months of possession for IVIE real-estate assets. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Not applicable to crypto-assets. | Do not populate for crypto code 21. | None. | Out of scope. | Low | None for crypto. |
| Credito d'imposta | 12 | Foreign patrimonial tax credit for the same property, financial product, or crypto-asset; capped by calculated tax. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 50, 52-53 | Amount of eligible foreign patrimonial tax credit, if any. | User-provided credit, capped at the relevant calculated tax in column 29, 31, or 33. | User profile / professional adjustment input; external tax documents. | Not implemented. | Medium | Foreign tax paid, finality, eligibility, applicable state. Official guidance is ambiguous for decentralized/self-custody cases. |
| Detrazioni - IVIE | 13 | IVIE principal-residence deduction. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Not applicable to crypto-assets. | Do not populate for crypto code 21. | None. | Out of scope. | Low | None for crypto. |
| Codice | 14 | Indicates related income schedules RL, RM, RT, combinations, future income, or unproductive asset status. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Likely `3` when related RT reporting exists, `5` when income will be future/unproductive, or another code if RL/RM applies. | Derive only when report modules can prove related schedules; otherwise require user/professional selection. | Ledger event classifications; future RT/RL/RM report modules. | Not implemented. | Medium | Whether generated or external income schedules are being filed. |
| Quota partecipazione | 15 | Participation percentage in company/entity when taxpayer is beneficial owner. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Percentage only for beneficial-owner structures. | User-provided; not calculated from exchange data. | User profile / report configuration. | Not implemented. | Low | Entity ownership details. |
| Solo monitoraggio | 16 | Checkbox for monitoring-only cases where IVIE, IVAFE, or IC is not due. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 51-52 | Checked only when report line is monitoring-only and no tax liquidation is due. | Set only from explicit rule/configuration; do not infer silently. | User/professional configuration; report mode. | Not implemented. | Medium | Reason IC is not due, if applicable. |
| Codice fiscale societa o altra entita giuridica | 17 | Identifies company/entity when taxpayer is beneficial owner. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Entity tax code or identifying code where applicable. | User-provided if column 2 is code 2 and column 15 applies. | User profile / report configuration. | Not implemented. | Low | Entity identity. |
| Codice fiscale altri cointestatari | 18 | Tax code of another co-owner required to file the same section. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Co-owner tax code if applicable. | User-provided. | User profile / report configuration. | Not implemented. | Low | Co-owner identity. |
| Codice fiscale altri cointestatari | 19 | Tax code of another co-owner required to file the same section. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Co-owner tax code if applicable. | User-provided. | User profile / report configuration. | Not implemented. | Low | Co-owner identity. |
| Presenza piu cointestatari | 20 | Checkbox when there are more than two co-owners. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Checked if more than two co-owners exist. | User-provided. | User profile / report configuration. | Not implemented. | Low | Co-owner count. |
| Regime fiscalita privilegiata | 21 | Checkbox for financial products held in privileged-tax states or territories. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52, 66 | Not clearly applicable to crypto code 21 by default. | Do not infer for crypto unless configured from official country/product treatment. | Custody/exchange metadata; user configuration. | Not implemented. | Medium | Whether and how privileged-tax-state treatment applies to crypto custody. Official guidance is ambiguous. |

## RW tax fields: RW1-RW5 columns 29-34

| Field name | Column number | Legal meaning | Official source file | Page number | Required value | Calculation rule | LedgerForge source | Status | Implementation complexity | Missing information |
| --- | ---: | --- | --- | ---: | --- | --- | --- | --- | --- | --- |
| IVAFE | 29 | Calculated tax on foreign financial products. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Not used for crypto code 21. | Do not calculate for crypto-assets; reserved for financial products. | None for crypto. | Out of scope for crypto. | Medium | Non-crypto financial-product support. |
| IVAFE dovuta | 30 | IVAFE due after allowed credit. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 52 | Not used for crypto code 21. | Column 29 minus column 12 for IVAFE assets. | None for crypto. | Out of scope for crypto. | Medium | Non-crypto financial-product support. |
| IVIE | 31 | Calculated tax on foreign real estate. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Not used for crypto code 21. | Apply IVIE rate to column 8, quota, and holding period for real estate. | None for crypto. | Out of scope. | Medium | Real-estate module. |
| IVIE dovuta | 32 | IVIE due after credit and deduction. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Not used for crypto code 21. | Column 31 minus column 12 and column 13. | None for crypto. | Out of scope. | Medium | Real-estate module. |
| IC | 33 | Calculated tax on crypto-assets. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Calculated IC amount for each crypto RW line. | Column 8 value x ownership quota x holding-period ratio x 0.20%. | Official RW value line, holding days, ownership share. | Not implemented as official field. | High | Complete valuation, holding days, quota, and decision on line aggregation. |
| IC dovuta | 34 | Crypto-assets tax due after allowed foreign patrimonial tax credit. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | IC due for each crypto RW line. | Column 33 minus column 12, floored according to form rules if applicable. | Column 33 calculation plus user-provided credit. | Not implemented. | High | Foreign tax credit eligibility and amount. |

## RW summary fields: RW6, RW7, RW8

RW6 summarizes IVAFE, RW7 summarizes IVIE, and RW8 summarizes crypto-assets tax.
For LedgerForge crypto work, RW8 is the relevant summary row. RW6 and RW7 should
remain separate and should not be populated from crypto-assets.

| Field name | Column number | Legal meaning | Official source file | Page number | Required value | Calculation rule | LedgerForge source | Status | Implementation complexity | Missing information |
| --- | ---: | --- | --- | ---: | --- | --- | --- | --- | --- | --- |
| RW6 - Totale imposta dovuta | 1 | Total IVAFE due from RW lines. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Sum of column 30. | Sum all RW1-RW5 IVAFE-due values. | Non-crypto financial-product module. | Out of scope for crypto. | Medium | IVAFE support. |
| RW6 - Eccedenza dichiarazione precedente | 2 | Prior-year IVAFE credit from previous return. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Prior declaration excess. | User-provided value from prior RX26. | User input. | Not implemented. | Low | Prior-year return data. |
| RW6 - Eccedenza compensata Mod. F24 | 3 | IVAFE excess already compensated through F24. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | F24-compensated amount. | User-provided. | User input. | Not implemented. | Low | F24 records. |
| RW6 - Acconti versati | 4 | IVAFE advances already paid. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Advances paid. | User-provided. | User input. | Not implemented. | Low | Payment records. |
| RW6 - Imposta a debito | 5 | IVAFE debit. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Positive balance. | Column 1 - column 2 + column 3 - column 4, if positive. | RW6 columns 1-4. | Out of scope for crypto. | Low | IVAFE support and user inputs. |
| RW6 - Imposta a credito | 6 | IVAFE credit. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Negative balance as credit. | Column 1 - column 2 + column 3 - column 4, if negative. | RW6 columns 1-4. | Out of scope for crypto. | Low | IVAFE support and user inputs. |
| RW7 - Totale imposta dovuta | 1 | Total IVIE due from RW lines. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Sum of column 32. | Sum all RW1-RW5 IVIE-due values. | Real-estate module. | Out of scope. | Medium | IVIE support. |
| RW7 - Eccedenza dichiarazione precedente | 2 | Prior-year IVIE credit from previous return. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Prior declaration excess. | User-provided value from prior RX25. | User input. | Not implemented. | Low | Prior-year return data. |
| RW7 - Eccedenza compensata Mod. F24 | 3 | IVIE excess already compensated through F24. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | F24-compensated amount. | User-provided. | User input. | Not implemented. | Low | F24 records. |
| RW7 - Acconti versati | 4 | IVIE advances already paid. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Advances paid. | User-provided. | User input. | Not implemented. | Low | Payment records. |
| RW7 - Imposta a debito | 5 | IVIE debit. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Positive balance. | Column 1 - column 2 + column 3 - column 4, if positive. | RW7 columns 1-4. | Out of scope. | Low | IVIE support and user inputs. |
| RW7 - Imposta a credito | 6 | IVIE credit. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Negative balance as credit. | Column 1 - column 2 + column 3 - column 4, if negative. | RW7 columns 1-4. | Out of scope. | Low | IVIE support and user inputs. |
| RW8 - Totale imposta dovuta | 1 | Total crypto-assets tax due from RW crypto lines. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Sum of column 34. | Sum all RW1-RW5 column 34 values across modules; only first module should carry RW8 totals. | Official RW crypto lines. | Not implemented. | Medium | Completed RW line calculations. |
| RW8 - Eccedenza dichiarazione precedente | 2 | Prior-year crypto-assets tax credit. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Prior declaration excess. | User-provided from prior-year declaration. | User input. | Not implemented. | Low | Prior-year return data. |
| RW8 - Eccedenza compensata Mod. F24 | 3 | Crypto-assets tax excess already compensated through F24. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | F24-compensated amount. | User-provided. | User input. | Not implemented. | Low | F24 records. |
| RW8 - Acconti versati | 4 | Crypto-assets tax advances already paid. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53 | Advances paid through relevant F24 codes. | User-provided payments for first and second advance. | User input. | Not implemented. | Low | F24 payment records. |
| RW8 - Imposta a debito | 5 | Crypto-assets tax debit. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53-54 | Positive balance; not paid if amount does not exceed 12 euro according to instructions. | Column 1 - column 2 + column 3 - column 4, if positive. | RW8 columns 1-4. | Not implemented. | Medium | Prior credits, compensated amounts, advances, rounding/payment threshold behavior. |
| RW8 - Imposta a credito | 6 | Crypto-assets tax credit. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 10; 53-54 | Negative balance as credit. | Column 1 - column 2 + column 3 - column 4, if negative. | RW8 columns 1-4. | Not implemented. | Medium | Prior credits, compensated amounts, and advances. |

## LedgerForge data mapping

| RW requirement | LedgerForge canonical source | Current gap |
| --- | --- | --- |
| Asset identity | `LedgerPosting.AssetSymbol` and future asset metadata. | Need official asset classification and stable asset identifiers. |
| Opening quantity | RW snapshot opening quantity from canonical postings before the year. | Quantity exists; official value calculation does not. |
| Closing quantity | RW snapshot closing quantity from canonical postings through year end. | Quantity exists; official value calculation does not. |
| Holding days | Balance intervals derived from posting timestamps. | Need deterministic day-count rule and tests for intraday movements. |
| Source traceability | `SourceReference.SourceSystem`, `SourceFile`, `SourceRowNumber`, and `RawData`. | Must carry trace links into RW report lines. |
| Unknown data | Unknown ledger events and audit warnings. | RW output must block or warn when unknowns can affect asset balances or values. |
| EUR values | Imported normalized EUR fields and future pricing providers. | Need official valuation hierarchy and evidence records. |
| Ownership percentage | User/report configuration. | Not derivable from exchange exports. |
| Tax credits and prior payments | User/report configuration. | Not derivable from exchange exports. |

## RT capital gains fields related to crypto

This section is intentionally separate from RW. It documents the boundary so
LedgerForge does not accidentally turn RW monitoring into capital-gains logic.

| RT field | Legal meaning | Official source file | Page number | LedgerForge status |
| --- | --- | --- | ---: | --- |
| RT41 | Total proceeds / normal value and related acquisition costs for crypto disposals, split between pre-2025 and 2025 columns. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 7; 38 | Out of scope for RW. Requires capital-gains engine. |
| RT42 | Disposals where redetermined crypto value is used. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 7; 38 | Out of scope for RW. Requires user election and valuation evidence. |
| RT43 | Prior-year losses carried forward. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 7; 39 | User/prior return input; not in ledger. |
| RT44 | Losses certified by intermediaries. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 7; 39 | External document input; not in ledger by default. |
| RT45 | Prior substitute-tax excess. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF2_istruzioni_2026_agg 13 05 2026.pdf` | 7; 39 | User/prior return input. |
| RT88-RT90 | Difference, substitute tax, and substitute tax due for crypto section V-B. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF1_istruzioni_2026_agg 28 05 2026.pdf` | 8; 162 | Out of scope for RW; maps to RX21, not RX27. |
| RT112 | Crypto substitute-tax credit section referenced by RX21. | `PF2_modello_2026_agg 13 05 2026.pdf`; `PF1_istruzioni_2026_agg 28 05 2026.pdf` | 8; 162 | Out of scope for RW. |

## Implementation checklist by priority

1. Preserve traceability from every RW output line back to ledger event IDs and
   original `SourceReference` values.
2. Add an explicit Italy RW report model that represents RW1-RW5 columns 1-21
   and 29-34, plus RW8 columns 1-6, without modifying the ledger.
3. Classify crypto-assets as RW column 3 code `21` only when asset metadata
   confirms they are crypto-assets.
4. Add configuration for taxpayer-specific fields: title code, possession type,
   ownership percentage, co-owners, beneficial-owner details, prior credits,
   F24 compensations, advances, and monitoring-only treatment.
5. Implement deterministic holding-day calculation from immutable ledger
   postings, with warnings for unknown events and timestamp anomalies.
6. Implement the official valuation evidence model for column 6, column 7, and
   column 8: exchange value, analogous platform value, market-data site value,
   and acquisition-cost fallback.
7. Add a "no invented values" validation gate: missing valuation, ownership, or
   ambiguous state/custody treatment must produce warnings or block final output.
8. Generate draft RW crypto lines with column 33 and column 34 only after column
   8, ownership quota, holding days, and tax credit inputs are complete.
9. Generate RW8 from completed RW crypto lines and user-provided prior/payment
   inputs; keep RW6 and RW7 separate.
10. Add report-level audit output explaining every generated number, source
    event coverage, valuation evidence, unknown-event impact, and assumptions.
11. Keep RT capital gains as a separate future module; do not reuse RW snapshots
    as a substitute for LIFO/FIFO/cost-basis logic.
12. Add professional-review export fields so users can hand the draft RW
    calculation, warnings, and source traceability to a qualified advisor.
