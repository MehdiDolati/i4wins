# IoT Twin Backend Service

سرویس بک‌اند برای دریافت، پاکسازی، ذخیره‌سازی و گزارش‌گیری از داده‌های سنسورهای IoT تجهیزات صنعتی.

---

## فهرست مطالب

1. [معرفی پروژه](#معرفی-پروژه)
2. [راه‌اندازی](#راه‌اندازی)
3. [راهنمای استفاده از API](#راهنمای-استفاده-از-api)
4. [معماری](#معماری)
5. [پاکسازی داده](#پاکسازی-داده)
6. [تست‌ها](#تست‌ها)
7. [دغدغه‌ها و سوالات طراحی](#دغدغه‌ها-و-سوالات-طراحی)

---

## معرفی پروژه

این سرویس داده‌های خام سنسورهای IoT (دما، لرزش، فشار) را از فایل JSONL خوانده، پاکسازی و حذف تکرار انجام داده، و در SQLite ذخیره می‌کند. سپس APIهایی برای گزارش‌گیری ارائه می‌دهد.

### دستگاه‌های موجود

| Device ID | Type | Metrics |
|-----------|------|---------|
| PUMP-01 | پمپ | temperature, vibration, pressure |
| PUMP-02 | پمپ | temperature, vibration, pressure |
| FAN-03 | فن | temperature, vibration |
| COMP-01 | کمپرسور | temperature, vibration, pressure |

---

## راه‌اندازی

### پیش‌نیازها

- .NET 10 SDK
- SQLite (به صورت خودکار نصب می‌شود)

### اجرا

```bash
# بازیابی و بیلد
dotnet restore
dotnet build

# اجرای سرویس
dotnet run --project src/IoT.Twin.Api
```

سرویس در آدرس `http://localhost:5259` (یا پورت نمایش داده شده در کنسول) اجرا می‌شود.

### مستندات API (Scalar)

پس از اجرا، مستندات تعاملی API در آدرس زیر در دسترس است:

```
http://localhost:5259/scalar/v1
```

### بارگذاری خودکار داده

در اولین اجرا، فایل `readings.jsonl` به صورت خودکار خوانده، پاکسازی و در دیتابیس ذخیره می‌شود.

### اجرای تست‌ها

```bash
dotnet test
```

---

## راهنمای استفاده از API

### دریافت لیست دستگاه‌ها

```http
GET /api/devices
```

**پاسخ نمونه:**
```json
["COMP-01", "FAN-03", "PUMP-01", "PUMP-02"]
```

### دریافت آخرین reading یک دستگاه

```http
GET /api/devices/{deviceId}/latest?metric={metric}
```

**نمونه:**
```http
GET /api/devices/PUMP-01/latest?metric=temperature
```

**پاسخ نمونه:**
```json
{
  "deviceId": "PUMP-01",
  "metric": "temperature",
  "timestamp": "2025-06-01T08:34:30Z",
  "value": 67.535,
  "sequenceNumber": 1208,
  "isAnomaly": false
}
```

### دریافت آمار تجمیعی (Aggregation)

```http
GET /api/readings/aggregation?deviceId={deviceId}&metric={metric}&from={from}&to={to}
```

**پارامترها:**
- `deviceId` (اختیاری): فیلتر بر اساس دستگاه
- `metric` (اختیاری): فیلتر بر اساس نوع مقدار
- `from` (اختیاری): زمان شروع
- `to` (اختیاری): زمان پایان

**پاسخ نمونه:**
```json
[
  {
    "deviceId": "PUMP-01",
    "metric": "temperature",
    "count": 50,
    "min": 66.548,
    "max": 70.112,
    "average": 68.234
  }
]
```

### دریافت داده سری زمانی (Time Series)

```http
GET /api/readings/timeseries?deviceId={deviceId}&metric={metric}
```

**پاسخ نمونه:**
```json
[
  {
    "deviceId": "PUMP-01",
    "metric": "temperature",
    "points": [
      { "timestamp": "2025-06-01T08:00:00Z", "value": 70.112 },
      { "timestamp": "2025-06-01T08:00:20Z", "value": 69.331 }
    ]
  }
]
```

### دریافت anomalies

```http
GET /api/readings/anomalies?deviceId={deviceId}
```

**پاسخ نمونه:**
```json
[
  {
    "deviceId": "FAN-03",
    "metric": "vibration",
    "timestamp": "2025-06-01T08:05:00Z",
    "value": -9999.0,
    "sequenceNumber": 2732,
    "reason": "Value -9999 exceeds threshold for vibration"
  }
]
```

---

## معماری

### Clean Architecture

```
IoT.Twin.slnx
├── src/
│   ├── IoT.Twin.Api/              # لایه ارائه ( Controllers, Program.cs )
│   ├── IoT.Twin.Application/      # منطق کسب‌وکار ( Services, DTOs )
│   ├── IoT.Twin.Domain/           # موجودیت‌ها و رابط‌ها ( بدون وابستگی )
│   └── IoT.Twin.Infrastructure/   # پیاده‌سازی ( EF Core, SQLite, فایل خوان )
└── tests/
    ├── IoT.Twin.Application.Tests/
    └── IoT.Twin.Infrastructure.Tests/
```

### چرا Clean Architecture؟

| مزیت | توضیح |
|------|-------|
| **جداسازی مسئولیت‌ها** | هر لایه یک وظیفه مشخص دارد |
| **تست‌پذیری** | منطق کسب‌وکار بدون وابستگی به دیتابیس قابل تست است |
| **قابلیت توسعه** | می‌توان SQLite را با هر دیتابیس دیگری جایگزین کرد |
| **خوانایی** | جریان وابستگی‌ها واضح است: Api → Application → Domain ← Infrastructure |

### جریان داده

```
readings.jsonl
     │
     ▼
┌─────────────┐
│ JsonlReader  │  خواندن فایل JSONL
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ DataCleaner  │  پاکسازی + حذف تکرار + علامت‌گذاری anomaly
└──────┬──────┘
       │
       ▼
┌─────────────────┐
│ ReadingRepository│  ذخیره در SQLite
└──────┬──────────┘
       │
       ▼
┌─────────────┐
│   SQLite     │
└──────┬──────┘
       │
       ▼
┌─────────────────┐
│ ReportService   │  محاسبه آمار، سری زمانی، anomalies
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│   Controllers   │  ارائه API
└─────────────────┘
```

### چرا SQLite؟

| ویژگی | دلیل |
|-------|------|
| **بدون نیاز به سرور** | فقط یک فایل دیتابیس |
| **حجم داده مناسب** | ~2000 رکورد، SQLite کافی است |
| **پشتیبان‌گیری آسان** | فقط کپی فایل `.db` |
| **مهاجرت آسان** | در آینده می‌توان به PostgreSQL مهاجرت کرد |

### چرا Scalar به جای Swagger؟

| ویژگی | Scalar | Swagger |
|-------|--------|---------|
| **رابط کاربری** | مدرن و تمیز | قدیمی‌تر |
| **عملکرد** | سریع‌تر | کندتر |
| **امکانات** | تست API داخلی | فقط مستندات |
| **ظاهر** | قابل سفارشی‌سازی | محدود |

---

## پاکسازی داده

### خط لوله پاکسازی

```
داده خام → اعتبارسنجی → حذف تکرار → علامت‌گذاری anomaly → ذخیره
```

### قوانین پاکسازی

| مرحله | قانون | مثال |
|-------|-------|------|
| **اعتبارسنجی** | حذف رکوردهای null/خالی | `deviceId: null` → حذف |
| **حذف تکرار** | نگه‌داشتن اولین رکورد با `(deviceId, metric, seq)` | دو رکورد با seq=100 → یکی |
| **علامت‌گذاری anomaly** | `|value| > 1000` | `-9999` → anomaly |
| **نگه‌داشتن outlier** | anomaly حذف نمی‌شود، فقط علامت‌گذاری | `1000000` → ذخیره با flag |

### مشکلات داده یافت شده

| مشکل | مثال | تعداد |
|------|------|-------|
| مقدار null | `value: null` | ۱ |
| metric null | `metric: null` | ۱ |
| deviceId خالی | `deviceId: ""` | ۱ |
| deviceId مفقود | فیلد وجود ندارد | ۱ |
| مقدار outlier | `-9999`, `1000000` | ۲ |
| رکورد تکراری | seq یکسان | چند مورد |

---

## تست‌ها

### اجرای تست‌ها

```bash
dotnet test
```

### پوشش تست‌ها

| پروژه تست | تعداد | تمرکز |
|-----------|-------|-------|
| Application.Tests | 15 | پاکسازی، aggregation، time series |
| Infrastructure.Tests | 6 | repository، queries، filtering |
| **مجموع** | **21** | |

### تست‌های کلیدی

#### پاکسازی و حذف تکرار (Deduplication)

| تست | بررسی می‌کند |
|-----|-------------|
| `Clean_DuplicateSeq_KeepsFirstOnly` | حذف تکرار بر اساس seq |
| `Clean_DifferentDevices_KeepsAll` | دستگاه‌های مختلف نگه داشته می‌شوند |
| `Clean_MultipleRecords_CleansCorrectly` | ترکیب حذف تکرار + حذف null |

#### تشخیص Anomaly

| تست | بررسی می‌کند |
|-----|-------------|
| `Clean_OutlierValue_IsFlaggedAsAnomaly` | مقدار `-9999` به عنوان anomaly |
| `Clean_NormalValue_IsNotAnomaly` | مقدار عادی anomaly نیست |

#### Aggregation

| تست | بررسی می‌کند |
|-----|-------------|
| `GetAggregation_ReturnsCorrectStats` | min/max/avg/count صحیح |
| `GetAggregation_GroupsByDeviceAndMetric` | گروه‌بندی صحیح |

---

## دغدغه‌ها و سوالات طراحی

### ۱. عملکرد و مقیاس‌پذیری

> **سوال:** با افزایش حجم داده از ۲۰۰۰ به ۲ میلیون رکورد، چه تغییراتی نیاز است؟

**پاسخ فعلی:**
- SQLite برای این حجم مناسب است
- ایندکس روی `(DeviceId, Metric, SequenceNumber)` اضافه شده

**پیشنهادات بهبود:**
- اضافه کردن ایندکس روی `Timestamp` برای فیلترهای زمانی
- مهاجرت به PostgreSQL برای حجم بالا
- استفاده از caching برای درخواست‌های تکراری

### ۲. یکپارچگی داده

> **سوال:** چگونه از یکپارچگی داده در هنگام خطا اطمینان حاصل می‌کنیم؟

**پاسخ فعلی:**
- Unique constraint روی `(DeviceId, Metric, SequenceNumber)`
- بررسی تکرار قبل از درج

**پیشنهادات بهبود:**
- استفاده از Transaction برای درج گروهی
- اضافه کردن Idempotency key
- Logging برای رکوردهای رد شده

### ۳. امنیت

> **سوال:** چه ملاحظات امنیتی رعایت شده؟

**پاسخ فعلی:**
- API فقط خواندنی (GET) است
- SQLite فایلی است و دسترسی آن توسط OS کنترل می‌شود

**پیشنهادات بهبود:**
- اضافه کردن Authentication/Authorization
- Rate limiting برای جلوگیری از سوءاستفاده
- HTTPS اجباری در Production

### ۴. انعطاف‌پذیری تمیزسازی

> **سوال:** آیا قوانین پاکسازی قابل تغییر هستند؟

**پاسخ فعلی:**
- آستانه anomaly ثابت (1000) است
- قوانین در `DataCleaner` سخت‌کد شده‌اند

**پیشنهادات بهبود:**
- استفاده از Configuration برای آستانه‌ها
- پشتیبانی از قوانین سفارشی per-metric
- Plugin architecture برای پاکسازی

### ۵. مانیتورینگ و لاگینگ

> **سوال:** چگونه عملکرد سرویس را رصد می‌کنیم؟

**پاسخ فعلی:**
- لاگ‌های پیش‌فرض ASP.NET Core
- لاگ‌های EF Core برای کوئری‌ها

**پیشنهادات بهبود:**
- Structured logging (Serilog)
- Metrics (Prometheus)
- Health checks

### ۶. تست‌پذیری

> **سوال:** چگونه از صحت پاکسازی و aggregation اطمینان حاصل می‌کنیم؟

**پاسخ فعلی:**
- 21 تست unit با پوشش خوب
- استفاده از InMemory database برای تست‌ها
- Mock برای testing منطق کسب‌وکار

**پیشنهادات بهبود:**
- Integration tests با SQLite واقعی
- Performance tests برای حجم بالا
- Contract tests برای API

### ۷. قابلیت نگهداری

> **سوال:** چگونه کد را تمیز و خوانا نگه می‌داریم؟

**پاسخ فعلی:**
- Clean Architecture با جداسازی واضح
- نام‌گذاری واضح متغیرها و کلاس‌ها
- تست‌ها به عنوان مستند زنده

**پیشنهادات بهبود:**
- Code review process
- Static analysis (SonarQube)
- Documentation as code

### ۸. استقرار

> **سوال:** چگونه سرویس را deploy می‌کنیم؟

**پاسخ فعلی:**
- `dotnet run` برای توسعه
- SQLite file-based برای سادگی

**پیشنهادات بهبود:**
- Docker container
- CI/CD pipeline
- Environment-specific configuration

---

## ساختار پروژه

```
IoT.Twin.slnx
├── src/
│   ├── IoT.Twin.Api/
│   │   ├── Program.cs                    # تنظیمات و DI
│   │   ├── Controllers/
│   │   │   ├── DevicesController.cs      # مدیریت دستگاه‌ها
│   │   │   └── ReadingsController.cs     # گزارش‌گیری
│   │   └── IoT.Twin.Api.csproj
│   ├── IoT.Twin.Application/
│   │   ├── Services/
│   │   │   ├── ReadingService.cs         # سرویس reading
│   │   │   └── ReportService.cs          # سرویس گزارش
│   │   ├── DTOs/
│   │   │   ├── ReadingDto.cs
│   │   │   ├── AggregationDto.cs
│   │   │   ├── TimeSeriesDto.cs
│   │   │   └── AnomalyDto.cs
│   │   └── IoT.Twin.Application.csproj
│   ├── IoT.Twin.Domain/
│   │   ├── Entities/
│   │   │   └── Reading.cs                # موجودیت اصلی
│   │   ├── Interfaces/
│   │   │   └── IReadingRepository.cs     # رابط دیتابیس
│   │   └── IoT.Twin.Domain.csproj
│   └── IoT.Twin.Infrastructure/
│       ├── Data/
│       │   ├── AppDbContext.cs            # Context دیتابیس
│       │   └── ReadingRepository.cs       # پیاده‌سازی repository
│       ├── Services/
│       │   ├── JsonlReader.cs             # خواننده فایل JSONL
│       │   └── DataCleaner.cs             # پاکسازی داده
│       └── IoT.Twin.Infrastructure.csproj
├── tests/
│   ├── IoT.Twin.Application.Tests/
│   │   └── Services/
│   │       ├── DataCleanerTests.cs       # تست پاکسازی
│   │       └── ReportServiceTests.cs     # تست گزارش
│   └── IoT.Twin.Infrastructure.Tests/
│       └── RepositoryTests.cs            # تست repository
├── readings.jsonl                         # داده خام
└── README.md
```

---

## تصمیمات طراحی

| تصمیم | انتخاب | دلیل |
|-------|--------|------|
| معماری | Clean Architecture | جداسازی مسئولیت‌ها، تست‌پذیری |
| دیتابیس | SQLite | بدون سرور، ساده، مناسب حجم |
| ORM | EF Core | LINQ، migration، type-safe |
| مستندات API | Scalar | مدرن، سریع، تعاملی |
| تست | xUnit + Moq | استاندارد صنعتی |
| پاکسازی | Flag anomalies | حفظ یکپارچگی داده |
