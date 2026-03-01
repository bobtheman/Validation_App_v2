namespace AccreditValidation.Platforms.Android;

using global::AccreditValidation.Components.Services.Interface;
using Plugin.NFC;
using System;
using System.Linq;
using System.Text;

public class NfcAndroidService : INfcService
{
    public bool IsAvailable => CrossNFC.IsSupported;
    public bool IsEnabled => CrossNFC.Current.IsEnabled;

    public event EventHandler<string>? TagRead;
    public event EventHandler<string>? TagError;

    public void StartListening()
    {
        if (!IsAvailable || !IsEnabled)
            return;

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

        try
        {
            CrossNFC.Current.StopListening();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NfcAndroidService] StopListening error (activity may have paused): {ex.Message}");
        }
    }

    private void OnMessageReceived(ITagInfo tagInfo)
    {
        if (tagInfo == null)
        {
            TagError?.Invoke(this, "NFC tag was unreadable.");
            return;
        }

        // NDEF path — classic stickers, wristbands.
        // IsEmpty/IsSupported are false for HCE tags (Google Wallet) so only
        // attempt this when records are actually present.
        if (!tagInfo.IsEmpty && tagInfo.Records != null)
        {
            foreach (var record in tagInfo.Records)
            {
                var payload = ExtractText(record);
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    TagRead?.Invoke(this, payload.Trim());
                    return;
                }
            }
        }

        // UID path — Google Wallet event passes use HCE / ISO-DEP and have no
        // NDEF records, but the tag identifier is always populated.
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

    private static string ExtractText(NFCNdefRecord record)
    {
        if (record?.Payload == null || record.Payload.Length == 0)
            return string.Empty;

        try
        {
            if (record.TypeFormat == NFCNdefTypeFormat.WellKnown && record.Payload.Length > 1)
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

            return Encoding.UTF8.GetString(record.Payload);
        }
        catch
        {
            return string.Empty;
        }
    }
}
