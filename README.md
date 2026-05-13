# Messager.EskizUz

[Eskiz.uz](https://eskiz.uz/sms) SMS API uchun zamonaviy .NET kutubxonasi: DI-friendly, multi-target, kriptografik OTP, boy `SendResultSMS`.

## O'rnatish

```bash
dotnet add package Messager.EskizUz --version 2.1.0
```

## Targets

`net6.0` / `net7.0` / `net8.0` / `net9.0` / `net10.0`.

JSON uchun `System.Text.Json` ishlatiladi — `Newtonsoft.Json` dependency yo'q.

## Konfiguratsiya

`appsettings.json`:

```json
"Eskiz": {
    "Email": "your@email.uz",
    "SecretKey": "your-secret-key",
    "SenderId": "4546",
    "CallbackUrl": null,
    "TokenLifetime": "25.00:00:00"
}
```

`SenderId` har bir mijoz uchun Eskiz tomonidan beriladi, default `4546` (test sender). `TokenLifetime` Eskiz token TTL (~30 kun), default 25 kun — token refresh muddati tugashidan oldin proactive ravishda ishlaydi.

## Ro'yxatdan o'tkazish (tavsiya etiladi — DI)

```csharp
// appsettings.json'dan bind:
builder.Services.AddEskizMessenger(builder.Configuration);

// yoki dasturiy ravishda:
builder.Services.AddEskizMessenger(o =>
{
    o.Email = "...";
    o.SecretKey = "...";
    o.SenderId = "4546";
});
```

DI orqali `IMessagerAgent` singleton sifatida ro'yxatga olinadi va `IHttpClientFactory` orqali named `HttpClient` ulanadi (socket-exhaustion muammosi yo'q).

## Foydalanish

```csharp
public class OtpController(IMessagerAgent eskiz) : ControllerBase
{
    [HttpPost("send-otp")]
    public async Task<IActionResult> Send([FromBody] string phone, CancellationToken ct)
    {
        var result = await eskiz.SendOtpAsync(phone, ct);
        if (!result.IsSuccess)
            return Problem(detail: result.ResponseBody, statusCode: (int)result.StatusCode);
        return Ok(new { result.MessageId });
    }

    [HttpPost("send-sms")]
    public async Task<IActionResult> Send([FromBody] SmsRequest req, CancellationToken ct)
    {
        var result = await eskiz.SendSMSAsync(req.Phone, req.Text, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
```

`SendResultSMS` quyidagi maydonlarni qaytaradi: `IsSuccess`, `StatusCode`, `ResponseBody`, `MessageId` (Eskiz response'dan ajratib olinadi) va OTP holatida `Code`.

## Legacy konstruktor

```csharp
// Hali ham ishlaydi, lekin har bir method [Obsolete] shim sifatida ko'rsatadi:
using var agent = new MessagerAgent("email@example.uz", "secret-key");
await agent.SendOtpAsync("+998901234567"); // [Obsolete] — CancellationToken overload'iga ko'ching
```

Legacy yo'lda process-wide static `HttpClient` ishlatiladi. Yangi loyihalar uchun DI variantini tanlang.

## Xatolarni boshqarish

Exception ierarxiyasi:

- `EskizException` — barcha Eskiz xatolari uchun base.
- `EskizAuthException` — 401 (login muvaffaqiyatsiz yoki refresh urinishidan keyin ham token ishlamadi). `ResponseBody` mavjud.
- `EskizApiException(StatusCode, ResponseBody)` — boshqa non-success HTTP javoblari.

401 olganda agent token'ni majburan yangilab, so'rovni bir marta qaytadan yuboradi. Ikkinchi 401 — `EskizAuthException`.

## 2.1.0 da yangiliklar

- Multi-target `net6.0`–`net10.0`, `Newtonsoft.Json` olib tashlandi (`System.Text.Json` only).
- `IMessagerAgent` interfeysi — test/mock-friendly.
- `AddEskizMessenger` DI extension + `EskizOptions` typed config (`IConfiguration` bind yoki `Action<EskizOptions>` overload).
- Lazy async token init + 25-kunlik proactive refresh + 401 holatida bir marta retry.
- Static `HttpClient` (legacy yo'l) / named `HttpClient` orqali `IHttpClientFactory` (DI yo'l) — socket-exhaustion to'g'rilandi.
- OTP uchun `RandomNumberGenerator.GetInt32` (kriptografik tasodifiy).
- Boy `SendResultSMS`: `StatusCode`, `ResponseBody`, `MessageId`, `IsSuccess`.
- Har bir public async metodda `CancellationToken` qabul qilinadi.
- `SenderId` endi konfiguratsiya orqali sozlanadi (avval hardcoded `4546` edi).

## Litsenziya

Ushbu kutubxona [MIT litsenziyasi](https://opensource.org/license/mit/) asosida tarqalgan.
