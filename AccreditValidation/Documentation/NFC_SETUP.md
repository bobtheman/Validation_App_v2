# NFC Integration Guide — AccreditValidation (.NET 9 MAUI)

## 1. NuGet package

```
dotnet add package Plugin.NFC
```

The package (by franckbour) targets Android 21+ and iOS 11+.  
It exposes the `CrossNFC` singleton used in `NfcService.cs`.

---

## 2. DI registration — MauiProgram.cs

```csharp
using AccreditValidation.Components.Services;
using AccreditValidation.Components.Services.Interface;

builder.Services.AddSingleton<INfcService, NfcService>();
```

Register as a **singleton** so the NFC session survives page navigation.

---

## 3. Android setup

### AndroidManifest.xml
Add inside `<manifest>`:

```xml
<uses-permission android:name="android.permission.NFC" />
<uses-feature android:name="android.hardware.nfc" android:required="false" />
```

`android:required="false"` keeps the app installable on devices without NFC;
`IsNfcAvailable` in the service will return `false` on those devices and the
button will simply not appear.

### MainActivity.cs
Plugin.NFC requires you to forward `OnNewIntent` and `OnResume`:

```csharp
using Plugin.NFC;

protected override void OnResume()
{
    base.OnResume();
    if (CrossNFC.IsSupported)
        CrossNFC.Current.StartListening();
}

protected override void OnNewIntent(Android.Content.Intent intent)
{
    base.OnNewIntent(intent);
    if (CrossNFC.IsSupported)
        CrossNFC.Current.OnNewIntent(intent);
}
```

---

## 4. iOS setup

### Info.plist
```xml
<key>NFCReaderUsageDescription</key>
<string>This app reads NFC badges for accreditation validation.</string>
```

### Entitlements.plist
```xml
<key>com.apple.developer.nfc.readersession.formats</key>
<array>
    <string>NDEF</string>
</array>
```

### Apple Developer Portal
Enable **Near Field Communication Tag Reading** in your App ID's capabilities,
then regenerate your provisioning profile.

---

## 5. Localization keys to add

Add these keys to every `*.resx` / `*.json` locale file:

| Key                  | English value                          |
|----------------------|----------------------------------------|
| `NfcScan`            | Scan NFC Badge                         |
| `NfcStopListening`   | Stop NFC (tap again to cancel)         |
| `NfcReady`           | Hold badge near device…                |
| `NfcNotAvailable`    | NFC is not available or is disabled    |
| `NfcError`           | NFC Error                              |

---

## 6. Flow summary

```
User taps "Scan NFC Badge"
  → ToggleNfcListening() → StartNfc()
  → NfcService subscribes to CrossNFC events and calls StartListening()
  → UI shows pulsing icon + "Hold badge near device…"

User taps NFC tag to device
  → CrossNFC.OnMessageReceived fires
  → NfcService.OnNfcTagRead extracts NDEF text (or falls back to UID hex)
  → Raises TagRead event → Validation.OnNfcTagRead()
  → StopNfc() called immediately (prevents double-read)
  → ValidateEntry(payload) called on main thread
  → Normal barcode validation flow executes
```

---

## 7. Tag encoding recommendation

For best results, encode badges as **NDEF / NFC Forum Text Records (TNF 0x01)**
containing the bare barcode value as a UTF-8 string.  
If tags are pre-encoded as URIs (e.g. `https://example.com/badge/12345`),
`ExtractText` in `NfcService` will return the full URI string — you may want
to strip the prefix in `OnNfcTagRead` before calling `ValidateEntry`.
