using System;
using System.IO;
using UnityEngine;
using System.Security.Cryptography;

public class AESController : MonoBehaviour
{
    private static string key = "cS/hSmmbGkwk6q0x4aw8PPYAdeE6x5N6KwIcFSF588Y="; // for AES-256, placeholder
    private static string iv = "/+CRbHsVEgeUqS8zyuMKhw=="; // placeholder
    
    // adapted via https://jonathancrozier.com/blog/how-to-generate-a-cryptographically-secure-random-string-in-dot-net-with-c-sharp
    public static void GenerateRandomKey(EncryptionType encryptionType)
    {
        int byteLength = (encryptionType == EncryptionType.AES128) ? 16 : 32;
        
        byte[] randomBytes = new byte[byteLength];
        using (var rng = new RNGCryptoServiceProvider()) 
        {
            rng.GetBytes(randomBytes);
        }

        key = Convert.ToBase64String(randomBytes);
        EventController.TriggerOnAESKeyGenerated(key);
    }

    // adapted via https://jonathancrozier.com/blog/how-to-generate-a-cryptographically-secure-random-string-in-dot-net-with-c-sharp
    public static void GenerateRandomIV(EncryptionType encryptionType)
    {
        // i think IV-size is independent of key-size actually?
        int byteLength = 16;
        
        byte[] randomBytes = new byte[byteLength];
        using (var rng = new RNGCryptoServiceProvider()) 
        {
            rng.GetBytes(randomBytes);
        }

        iv = Convert.ToBase64String(randomBytes);

        EventController.TriggerOnAESIVGenerated(iv);
    }

    public static string Key => key;
    public static string IV => iv;
    
    public static string Encrypt(string s)
    {
        byte[] bytes;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Convert.FromBase64String(key);
            aes.IV = Convert.FromBase64String(iv);

            ICryptoTransform ict = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, ict, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(s);
                    }
                    bytes = memoryStream.ToArray();
                }
            }
        }
        return Convert.ToBase64String(bytes);
    }
    
    public static string Encrypt(byte[] data)
    {
        byte[] bytes;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Convert.FromBase64String(key);
            aes.IV = Convert.FromBase64String(iv);

            ICryptoTransform ict = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, ict, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();

                    bytes = memoryStream.ToArray();
                }
            }
        }
        return Convert.ToBase64String(bytes);
    }
    
    public static string Decrypt(string encrypted)
    {
        string plaintext = "";

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Convert.FromBase64String(key);
            aesAlg.IV = Convert.FromBase64String(iv);

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(encrypted)))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                    {
                        plaintext = streamReader.ReadToEnd();
                    }
                }
            }
        }

        return plaintext;
    }
    
    public static byte[] Decrypt(byte[] cipherTextBytes)
    {
        byte[] plaintextBytes;

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Convert.FromBase64String(key);
            aesAlg.IV = Convert.FromBase64String(iv);

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (MemoryStream tempMemoryStream = new MemoryStream())
                    {
                        cryptoStream.CopyTo(tempMemoryStream);
                        plaintextBytes = tempMemoryStream.ToArray();
                    }
                }
            }
        }

        return plaintextBytes;
    }
}
