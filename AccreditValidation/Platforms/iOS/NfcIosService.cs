namespace AccreditValidation.Platforms.iOS;

using CoreNFC;
using Foundation;
using global::AccreditValidation.Components.Services.Interface;
using ObjCRuntime;
using System;
using System.Text;

// iOS NFC service using NFCTagReaderSession (iOS 13+).
//
// Plugin.MAUI.NFC's NFCNdefReaderSession is blind to ISO 7816 / HCE tags
// (Google Wallet event passes). NFCTagReaderSession handles both NDEF and
// ISO 7816 in a single session, so we use it exclusively on iOS.

public class NfcIosService : INfcService
{
    private NFCTagReaderSession? _session;

    public bool IsAvailable => NFCTagReaderSession.ReadingAvailable;
    public bool IsEnabled => NFCTagReaderSession.ReadingAvailable;

    public event EventHandler<string>? TagRead;
    public event EventHandler<string>? TagError;

    public void StartListening()
    {
        if (!IsAvailable)
            return;

        _session?.InvalidateSession();

        _session = new NFCTagReaderSession(
            NFCPollingOption.Iso14443 | NFCPollingOption.Iso15693,
            new TagDelegate(this),
            queue: null)
        {
            AlertMessage = "Hold your device near the NFC tag."
        };

        _session.BeginSession();
    }

    public void StopListening()
    {
        _session?.InvalidateSession();
        _session = null;
    }

    private void RaiseTagRead(string value) => TagRead?.Invoke(this, value);
    private void RaiseTagError(string message) => TagError?.Invoke(this, message);

    private sealed class TagDelegate : NSObject, INFCTagReaderSessionDelegate
    {
        private readonly NfcIosService _svc;

        internal TagDelegate(NfcIosService svc) => _svc = svc;

        public void DidDetectTags(NFCTagReaderSession session, INFCTag[] tags)
        {
            var tag = tags[0];

            session.ConnectTo(tag, connectError =>
            {
                if (connectError != null)
                {
                    session.InvalidateSession(connectError.LocalizedDescription);
                    _svc.RaiseTagError(connectError.LocalizedDescription);
                    return;
                }

                // ISO 7816 path — Google Wallet HCE event passes.
                if (tag.Type == NFCTagType.Iso7816Compatible)
                {
                    var iso7816 = Runtime.GetINativeObject<INFCIso7816Tag>(tag.Handle, false);
                    if (iso7816 != null)
                    {
                        var uid = iso7816.Identifier?.ToArray();
                        if (uid != null && uid.Length > 0)
                        {
                            var hex = BitConverter.ToString(uid).Replace("-", string.Empty);
                            session.AlertMessage = "Tag read successfully.";
                            session.InvalidateSession();
                            _svc.RaiseTagRead(hex);
                            return;
                        }
                    }
                }

                // NDEF path — standard stickers, wristbands.
                var ndefTag = Runtime.GetINativeObject<INFCNdefTag>(tag.Handle, false);
                if (ndefTag != null)
                {
                    ndefTag.ReadNdef((message, ndefError) =>
                    {
                        if (ndefError != null || message?.Records == null)
                        {
                            session.InvalidateSession();
                            _svc.RaiseTagError("Failed to read NDEF data from tag.");
                            return;
                        }

                        foreach (var record in message.Records)
                        {
                            var text = DecodeNdefPayload(record);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                session.AlertMessage = "Tag read successfully.";
                                session.InvalidateSession();
                                _svc.RaiseTagRead(text.Trim());
                                return;
                            }
                        }

                        session.InvalidateSession();
                        _svc.RaiseTagError("NFC tag contained no readable payload.");
                    });
                    return;
                }

                session.InvalidateSession("Tag type not supported.");
                _svc.RaiseTagError("NFC tag type is not supported on this device.");
            });
        }

        public void DidInvalidateWithError(NFCTagReaderSession session, NSError error)
        {
            // Code 200 = user cancelled — not a real error.
            if (error.Code != 200)
            {
                _svc.RaiseTagError(error.LocalizedDescription);
            }
        }

        private static string DecodeNdefPayload(NFCNdefPayload record)
        {
            if (record?.Payload == null || record.Payload.Length == 0)
                return string.Empty;

            try
            {
                var bytes = record.Payload.ToArray();

                if (record.TypeNameFormat == NFCTypeNameFormat.NFCWellKnown && bytes.Length > 1)
                {
                    var statusByte = bytes[0];
                    var langCodeLen = statusByte & 0x3F;
                    var isUtf16 = (statusByte & 0x80) != 0;
                    var textStartIdx = 1 + langCodeLen;

                    if (textStartIdx < bytes.Length)
                    {
                        var textBytes = bytes[textStartIdx..];
                        return isUtf16
                            ? Encoding.Unicode.GetString(textBytes)
                            : Encoding.UTF8.GetString(textBytes);
                    }
                }

                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
