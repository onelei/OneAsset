using System.IO;
using UnityEngine;

namespace OneAsset.Runtime.Rule
{
    public class OffsetEncryptRule : IEncryptRule
    {
        private const int Offset = 6;

        public byte[] Encrypt(byte[] bytes)
        {
            var encryptedBytes = new byte[bytes.Length + Offset];
            for (var i = 0; i < Offset; i++)
            {
                encryptedBytes[i] = (byte) i;
            }
            System.Array.Copy(bytes, 0, encryptedBytes, Offset, bytes.Length);
            return encryptedBytes;
        }

        public AssetBundle Decrypt(string path, uint crc)
        {
            return AssetBundle.LoadFromFile(path, crc, Offset);
        }
        
        public AssetBundleCreateRequest DecryptAsync(string path, uint crc)
        {
            return AssetBundle.LoadFromFileAsync(path, crc, Offset);
        }
    }
}