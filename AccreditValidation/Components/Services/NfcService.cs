namespace AccreditValidation.Components.Services;

using global::AccreditValidation.Components.Services.Interface;
using Plugin.NFC;
using System;
using System.Linq;
using System.Text;

public class NfcService : INfcService
{
    // ── Public surface ────────────────────────────────────────────────────

    public bool IsAvailable => CrossNFC.IsSupported;
    public bool IsEnabled => CrossNFC.Current.IsEnabled;

    public event EventHandler<string>? TagRead;
    public event EventHandler<string>? TagError;

    // ── Listening ─────────────────────────────────────────────────────────

    public void StartListening()
    {
        if (!IsAvailable || !IsEnabled)
            return;

        // Guard against double-subscription
        CrossNFC.Current.OnMessageReceived -= OnMessageReceived;
        CrossNFC.Current.OnNfcStatusChanged -= OnNfcStatusChanged;

        CrossNFC.Current.OnMessageReceived += OnMessageReceived;
        CrossNFC.Current.OnNfcStatusChanged += OnNfcStatusChanged;

        CrossNFC.Current.StartListening();
    }

    public void StopListening()
    {
        if (!IsAvailable)
            return;

        CrossNFC.Current.OnMessageReceived -= OnMessageReceived;
        CrossNFC.Current.OnNfcStatusChanged -= OnNfcStatusChanged;

        CrossNFC.Current.StopListening();
    }

    // ── Private handlers ──────────────────────────────────────────────────

    private void OnMessageReceived(ITagInfo tagInfo)
    {
        if (tagInfo == null || tagInfo.IsEmpty)
        {
            TagError?.Invoke(this, "NFC tag was empty or unreadable.");
            return;
        }

        foreach (var record in tagInfo.Records ?? Array.Empty<NFCNdefRecord>())
        {
            var payload = ExtractText(record);
            if (!string.IsNullOrWhiteSpace(payload))
            {
                TagRead?.Invoke(this, payload.Trim());
                return;
            }
        }

        // Fallback: use raw tag UID as the barcode value
        var uid = tagInfo.Identifier;
        if (uid != null && uid.Length > 0)
        {
            var hexId = BitConverter.ToString(uid).Replace("-", string.Empty);
            TagRead?.Invoke(this, hexId);
            return;
        }

        TagError?.Invoke(this, "NFC tag contained no readable payload.");
    }

    private void OnNfcStatusChanged(bool isEnabled)
    {
        if (!isEnabled)
        {
            TagError?.Invoke(this, "NFC was disabled on the device.");
        }  
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string ExtractText(NFCNdefRecord record)
    {
        if (record?.Payload == null || record.Payload.Length == 0)
            return string.Empty;

        try
        {
            if (record.TypeFormat == NFCNdefTypeFormat.WellKnown &&
                record.Payload.Length > 1)
            {
                var statusByte = record.Payload[0];
                var langCodeLen = statusByte & 0x3F;
                var isUtf16 = (statusByte & 0x80) != 0;
                var textStartIdx = 1 + langCodeLen;

                if (textStartIdx < record.Payload.Length)
                {
                    var textBytes = record.Payload.Skip(textStartIdx).ToArray();
                    return isUtf16
                        ? Encoding.Unicode.GetString(textBytes)
                        : Encoding.UTF8.GetString(textBytes);
                }
            }

            // Fallback: interpret whole payload as UTF-8
            return Encoding.UTF8.GetString(record.Payload);
        }
        catch
        {
            return string.Empty;
        }
    }
}