# LedgerForge Benchmark Report

Generated at UTC: `2026-07-02T14:18:28.0748440+00:00`

Synthetic benchmark data only. No real financial data is used.

## Scope

- Parsing: Binance CSV importer over generated fake CSV rows.
- Ledger generation: canonical `ledger.json` write and validation.
- RW generation: yearly RW snapshot report generation.
- Audit: in-memory ledger integrity check.
- Memory: managed heap observed after each operation and process-wide allocated bytes during each operation.

## Results

| Transactions | Events | Parsing ms | Ledger ms | RW ms | Audit ms | Max observed heap MB | Allocated MB | CSV MB | Ledger JSON MB | Integrity | Confidence |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 100 | 100 | 24.199 | 117.379 | 17.52 | 24.12 | 1.56 | 1.68 | 0.007 | 0.061 | 100 | 100 |
| 1,000 | 1,000 | 10.643 | 30.16 | 1.107 | 4.172 | 10.518 | 12.113 | 0.069 | 0.609 | 100 | 100 |
| 10,000 | 10,000 | 132.254 | 255.951 | 23.247 | 52.924 | 74.475 | 112.046 | 0.689 | 6.099 | 100 | 100 |
| 100,000 | 100,000 | 780.914 | 1235.133 | 50.31 | 218.436 | 674.417 | 1091.199 | 6.886 | 61.082 | 100 | 100 |
| 1,000,000 | 1,000,000 | 2846.003 | 29335.078 | 491.093 | 1385.577 | 5839.9 | 12178.548 | 68.855 | 611.772 | 100 | 100 |

## Detailed Operations

### 100 Transactions

| Operation | Elapsed ms | Observed heap MB | Allocated MB |
| --- | ---: | ---: | ---: |
| Input generation | 12.823 | 0.205 | 0.057 |
| Parsing | 24.199 | 0.602 | 0.371 |
| Ledger generation | 117.379 | 1.311 | 0.977 |
| RW generation | 17.52 | 1.56 | 0.127 |
| Audit | 24.12 | 1.311 | 0.148 |

### 1.000 Transactions

| Operation | Elapsed ms | Observed heap MB | Allocated MB |
| --- | ---: | ---: | ---: |
| Input generation | 9.11 | 1.649 | 0.284 |
| Parsing | 10.643 | 4.489 | 3.375 |
| Ledger generation | 30.16 | 10.518 | 6.961 |
| RW generation | 1.107 | 8.561 | 0.071 |
| Audit | 4.172 | 6.245 | 1.421 |

### 10.000 Transactions

| Operation | Elapsed ms | Observed heap MB | Allocated MB |
| --- | ---: | ---: | ---: |
| Input generation | 18.394 | 7.58 | 2.73 |
| Parsing | 132.254 | 18.122 | 33.754 |
| Ledger generation | 255.951 | 73.873 | 60.875 |
| RW generation | 23.247 | 74.475 | 0.584 |
| Audit | 52.924 | 35.84 | 14.103 |

### 100.000 Transactions

| Operation | Elapsed ms | Observed heap MB | Allocated MB |
| --- | ---: | ---: | ---: |
| Input generation | 150.808 | 34.814 | 27.185 |
| Parsing | 780.914 | 142.493 | 336.949 |
| Ledger generation | 1235.133 | 668.656 | 581.207 |
| RW generation | 50.31 | 674.417 | 5.738 |
| Audit | 218.436 | 299.15 | 140.119 |

### 1.000.000 Transactions

| Operation | Elapsed ms | Observed heap MB | Allocated MB |
| --- | ---: | ---: | ---: |
| Input generation | 768.152 | 278.678 | 271.748 |
| Parsing | 2846.003 | 815.282 | 3365.322 |
| Ledger generation | 29335.078 | 5839.9 | 7092.198 |
| RW generation | 491.093 | 5793.524 | 57.252 |
| Audit | 1385.577 | 3447.175 | 1392.029 |

