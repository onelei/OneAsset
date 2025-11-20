using System.IO;
using UnityEngine;

namespace OneAsset.Runtime.Rule
{
    public class StreamEntryptRule : IEntryptRule
    {
        private const byte XorKey = 0xAB;

        public byte[] Encrypt(byte[] bytes)
        {
            var encryptedBytes = new byte[bytes.Length];
            for (var i = 0; i < bytes.Length; i++)
            {
                encryptedBytes[i] = (byte) (bytes[i] ^ XorKey);
            }

            return encryptedBytes;
        }

        public AssetBundle Decrypt(string path, uint crc)
        {
            var decryptedStream = new XorDecryptStream(path, XorKey);
            return AssetBundle.LoadFromStream(decryptedStream, crc);
        }

        private class XorDecryptStream : FileStream
        {
            private readonly byte _key;

            public XorDecryptStream(string path, byte key)
                : base(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            {
                this._key = key;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var bytesRead = base.Read(buffer, offset, count);
                for (var i = 0; i < bytesRead; i++)
                {
                    buffer[offset + i] ^= _key;
                }
                return bytesRead;
            }
        }
    }
}