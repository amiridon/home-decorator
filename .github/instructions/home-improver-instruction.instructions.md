
# SPEC‑4: AI‑Driven Home‑Design Visualization SaaS  
*Consolidated specification – merges SPEC‑1, SPEC‑2 & SPEC‑3 (May 18 2025)*

> **What’s new in this consolidation**  
> • Carries forward all expansions from SPEC‑2 (Stripe billing, product recommendations).  
> • Integrates the **Mobile MVP Test‑Harness** and **Secret‑Management** controls introduced in SPEC‑3.  
> • Unifies architecture, data‑model, API surface and timelines into a single canonical document.

---

## 1 · Background

Homeowners, interior designers and real‑estate agents often need rapid, cost‑effective design visualisations.  
This SaaS lets users snap a photo of an interior or exterior space and receive an AI‑enhanced image that shows an upgraded design. A mobile‑first experience—powered by a .NET backend, SQLite and the DALL·E API—drives faster decision‑making and creative exploration.

---

## 2 · Requirements

### 2.1 Must Have

| ID | Requirement |
|----|-------------|
| **M0** | **Mobile MVP Test‑Harness** built with .NET MAUI (iOS + Android). Includes `IsFakeMode` flag to bypass auth and hit stub data while backend APIs come online. |
| **M1** | Core generation workflow from SPEC‑1 (auth, image upload, prompt entry, history, admin abuse monitoring). |
| **M2** | **Stripe billing** for free‑tier credits, one‑time packs and subscriptions. Blocks generation when out of credits. |
| **M3** | **AI‑matched product recommendations** with price, vendor and affiliate URL for each generated image. |
| **M4** | **Admin reconciliation tools** for Stripe events and internal credit ledger. |
| **M5** | **Secret management**—no secrets in Git; GitHub Push Protection, runtime secrets in Azure Key Vault, `dotnet user‑secrets` in dev. |

### 2.2 Should Have

| ID | Requirement |
|----|-------------|
| S1 | Affiliate‑link click tracking & analytics dashboard. |
| S2 | “Buy this look” cart that deep‑links to retailer checkout. |
| S3 | Opt‑in shoppable email (generated image + products). |

### 2.3 Could Have

| ID | Requirement |
|----|-------------|
| C1 | Side‑by‑side original vs generated comparison slider. |
| C2 | Project folders grouping multiple rooms per property. |
| C3 | Push notifications when generation completes. |

### 2.4 Won’t Have (in MVP)

* AR or real‑time 3‑D visualisation.  
* Full remodelling budget calculators.

---

## 3 · High‑Level Architecture

```plantuml
@startuml
actor User
actor Stripe
actor RetailAPI as "Retail / Affiliate API"
User  --> MobileApp              : Snap photo / prompt
User  --> WebSPA                 : Browser flow
MobileApp --> WebAPI             : POST /image-request
WebSPA    --> WebAPI             : same
note right of MobileApp : .NET MAUI
IsFakeMode flag
WebAPI --> BillingSvc            : Check credits
BillingSvc --> SQLite_DB         : Ledger
WebAPI --> DallE_API             : generate()
WebAPI --> ProductMatcherSvc     : object‑detect & map products
ProductMatcherSvc --> RetailAPI  : query catalog
ProductMatcherSvc --> WebAPI
WebAPI --> S3_Storage            : store images
WebAPI --> SQLite_DB             : persist request + products
WebAPI --> MobileApp : img URL + recs
== Billing ==
MobileApp --> WebAPI : GET /billing/checkout
WebAPI --> Stripe
Stripe --> User
Stripe --> WebAPI : webhooks
WebAPI --> SQLite_DB : update credits
@enduml
```

---

## 4 · Data Model (SQLite 3)

### 4.1 User & Billing

```sql
CREATE TABLE Users (
    Id TEXT PRIMARY KEY,
    Email TEXT NOT NULL UNIQUE,
    DisplayName TEXT,
    StripeCustomerId TEXT,
    Credits INTEGER DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE PaymentTransactions (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    StripePaymentIntentId TEXT,
    AmountCents INTEGER,
    Currency TEXT,
    CreditsPurchased INTEGER,
    Type TEXT CHECK(Type IN ('OneTime','Subscription','Refund')),
    Status TEXT CHECK(Status IN ('Pending','Succeeded','Failed','Refunded')),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(UserId) REFERENCES Users(Id)
);

CREATE TABLE Subscriptions (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    StripeSubscriptionId TEXT,
    PlanName TEXT,
    Status TEXT CHECK(Status IN ('Active','PastDue','Canceled','Trialing')),
    CurrentPeriodEnd DATETIME,
    FOREIGN KEY(UserId) REFERENCES Users(Id)
);
```

### 4.2 Images & Products

```sql
CREATE TABLE ImageRequests (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    OriginalImageUrl TEXT NOT NULL,
    Prompt TEXT NOT NULL,
    Status TEXT CHECK(Status IN ('Pending','Processing','Completed','Failed')),
    GeneratedImageUrl TEXT,
    CreditsCharged INTEGER DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(UserId) REFERENCES Users(Id)
);

CREATE TABLE Products (
    Id TEXT PRIMARY KEY,
    ExternalId TEXT NOT NULL,
    Name TEXT,
    ThumbnailUrl TEXT,
    DetailUrl TEXT,
    Price DECIMAL(10,2),
    Currency TEXT,
    Vendor TEXT
);

CREATE TABLE ImageRequestProducts (
    ImageRequestId TEXT,
    ProductId TEXT,
    Score REAL,
    PRIMARY KEY (ImageRequestId, ProductId),
    FOREIGN KEY(ImageRequestId) REFERENCES ImageRequests(Id),
    FOREIGN KEY(ProductId) REFERENCES Products(Id)
);
```

> **Suggested Indexes**  
> `CREATE INDEX idx_user_credits ON Users(Id,Credits);`  
> `CREATE INDEX idx_img_status  ON ImageRequests(Status);`

---

## 5 · Key Application Services

| Service | Responsibility |
|---------|----------------|
| **BillingSvc** | Wrap Stripe SDK; manage ledger & webhooks. |
| **GenerationSvc** | Call DALL·E, store images. |
| **ProductMatcherSvc** | Detect objects & fetch matching SKUs. |
| **RecommendationSvc** | Rank & filter product list. |
| **FeatureFlagSvc** | Injects fake‑data toggle and other flags into MAUI & SPA. |

---

## 6 · API Surface (Minimal .NET)

| Verb | Endpoint | Auth | Purpose |
|------|----------|------|---------|
| POST | `/auth/login` | ❌ | Email/password or social login. |
| GET  | `/billing/checkout/{packId}` | ✅ | Stripe Checkout link. |
| GET  | `/billing/portal` | ✅ | Stripe Billing Portal link. |
| POST | `/stripe/webhook` | ❌ | Receive Stripe events. |
| POST | `/image-request` | ✅ | New generation request (debited). |
| GET  | `/image-request/{id}` | ✅ | Poll status & results. |
| GET  | `/history` | ✅ | Past requests + thumbnails. |
| GET  | `/products/{id}` | ✅ | Product detail & buy link. |

---

## 7 · Secret‑Management Requirements

| Layer | Practice | Rationale |
|-------|----------|-----------|
| **GitHub** | Push Protection & secret‑scanning on every branch. | Prevent leaked credentials. |
| **Local Dev** | `dotnet user-secrets` store. | Keeps tokens off disk. |
| **CI/CD** | GitHub Secrets referenced in workflows; never echoed. | Least‑privilege builds. |
| **Cloud** | Azure Key Vault via `IConfiguration`; RBAC scoped. | Centralised encryption & rotation. |
| **Database** | Only dynamic per‑user tokens, column‑level encryption. | Limits blast‑radius. |

**Prohibited:** secrets in `appsettings.Production.json`, scripts, Dockerfiles, or source control.

Rotation every 90 days minimum; Key Vault & GitHub audit logs reviewed weekly.

---

## 8 · Implementation Timeline

| Week | Track | Major Deliverables |
|------|-------|--------------------|
| **0** | 🏗 Foundation | Repo + CI/CD + GitHub secret policies. |
| **1** | 📱 Mobile Test‑Harness | .NET MAUI shell, fake‑data toggle, DI plumbing. |
| **2** | 💳 Billing Core | Stripe sandbox flow, credit ledger, webhooks. |
| **3** | 🖼️ Image Generation | DALL·E integration; thumbnail feed. |
| **4** | 🛍️ Product Matching | Object detection & retail API, “Shop this look”. |
| **5** | 🧪 Hardening & QA | Regression tests, telemetry, secret‑rotation playbook. |

Continuous testing: every story must demo in the MAUI harness.

---

## 9 · Compliance Checklist (Definition of Done)

- [ ] GitHub Push Protection blocks non‑approved bypasses.  
- [ ] Key Vault RBAC: only WebAPI & jobs have “Get Secret”.  
- [ ] Secrets surface via `IConfiguration` only.  
- [ ] MAUI fake‑data flag documented in README.  
- [ ] Playwright / XCUITest smoke tests run in CI using injected secrets.  

---

## 10 · Success Metrics

| KPI | Target |
|-----|--------|
| Stripe payment success | ≥ 98 % |
| p95 time‑to‑image | ≤ 30 s |
| First‑month successful generations | ≥ 100 |
| MRR by month 3 | ≥ $1 000 |
| Affiliate clicks per generation | > 1.5 |
| User satisfaction | ≥ 4 / 5 |

---

## 11 · Future Iterations

1. Migrate database to PostgreSQL for scale.  
2. Add biometric login once backend auth finalised.  
3. Multi‑vendor product aggregation (Home Depot, IKEA, etc.).  
4. AI style‑coach that iterates prompts & product mixes.  
5. Geo‑replicated backup vault for disaster recovery.

---
