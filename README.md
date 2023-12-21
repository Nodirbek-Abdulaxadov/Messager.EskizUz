## Tavsif
Messager, SMS xabarlarini yuborish uchun ma'lum [Eskiz.uz](https://eskiz.uz/sms) API xizmatidan foydalanish uchun yaratilgan .NET kutubxonasi. U bir martalik parollar (OTP) va odatiy matnli xabarlarni yuborish jarayonini sodda funksionallik orqali osonlashtiradi.

## O'rnatish
Messager NuGet paketini quyidagi buyruq yordamida o'rnatishingiz mumkin:

```bash
dotnet add package Messager.EskizUz
```

## Boshlash
Messagerdan foydalanish uchun siz e-mail va maxfiy kalitni taqdim etib, Messager klassining misolini yaratishingiz kerak. Klass autentifikatsiya va belgilangan so'rov uchun belgilangan to'kenni olishni avtomatik ravishda boshlaydi.

```csharp
using Messager.EskizUz;

// Messager obyektini yarating
var messager = new MessagerAgent("sizning-email@example.com", "sizning-maxfiy-kalitingiz");
```

## OTP SMS Yuborish
Messager orqali OTP SMS xabarlarni osonlik bilan yuborishingiz mumkin. Telefon raqamiga bir martalik OTP kod yuborish uchun SendOtpAsync metodidan foydalaning.

```csharp
// OTP SMS yuborish
var natija = await messager.SendOtpAsync("+998901234567");
if (natija.Success)
{
    Console.WriteLine($"OTP muvaffaqiyatli yuborildi. Kod: {natija.Code}");
}
else
{
    Console.WriteLine("OTP SMS yuborishda xatolik yuz berdi.");
}
```

## Odatiy SMS Yuborish
SendSMSAsync metodidan foydalanib odatiy matnli SMS xabarlarni yuborishingiz mumkin.

```csharp
// Odatiy SMS yuborish
var yuborildi = await messager.SendSMSAsync("+998901234567", "Salom Messager.EskizUz dan!");
if (yuborildi)
{
    Console.WriteLine("SMS muvaffaqiyatli yuborildi.");
}
else
{
    Console.WriteLine("SMS yuborishda xatolik yuz berdi.");
}
```

## Muhim Eslatmalar
Ma'lum email va maxfiy kalitingiz bilan "sizning-email@example.com" va "sizning-maxfiy-kalitingiz" ni almashtiring.

## Ogohlantirish
Ushbu kutubxona sodda shaklda taqdim etilgan va har bir yuz berishi mumkin bo'lgan xatolar boshqaruvini o'z ichiga olmaydi. Sizning mahsulotingizga mos ravishda xato boshqaruvini yanada rivojlantirish tavsiya etiladi.

## Litsenziya
Ushbu kutubxona [MIT litsenziyasi](https://opensource.org/license/mit/) asosida tarqalgan.
