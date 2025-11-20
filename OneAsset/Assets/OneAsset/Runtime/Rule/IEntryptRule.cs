using UnityEngine;

namespace OneAsset.Runtime.Rule
{
    public interface IEntryptRule
    {
        byte[] Encrypt(byte[] bytes);
        AssetBundle Decrypt(string path, uint crc);
    }

    public class EntryptDisable : IEntryptRule
    {
        public byte[] Encrypt(byte[] bytes)
        {
            return bytes;
        }

        public AssetBundle Decrypt(string path, uint crc)
        {
            return AssetBundle.LoadFromFile(path, crc);
        }
    }
}