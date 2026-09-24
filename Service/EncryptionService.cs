using System.Security.Cryptography;

namespace SecureShare.Services
{
    public class EncryptionService
    {
        private readonly byte[] _key;

        public EncryptionService(IConfiguration configuration) //Iconfiguration is used to find the key which we have stored in user secrets area
        {
            string? keyString = configuration["Encryption:AesKey"]; //finds for the AES key

            if (string.IsNullOrEmpty(keyString))
            {
                throw new InvalidOperationException(
                    "Encryption key is not configured."
                );
            }

            _key = Convert.FromBase64String(keyString);

            if (_key.Length != 32)
            {
                throw new InvalidOperationException(
                    "Encryption key must be exactly 32 bytes."
                );
            }
        }
        public async Task EncryptFileAsync(
            string inputFilePath, //input file name
            string outputFilePath //output file name
            ) //AES key
        {
            using Aes aes = Aes.Create(); //created the AES object used for encryption

            aes.Key = _key;
            aes.GenerateIV(); //generates the random initializer used inside each file for encryption

            using FileStream inputFile = new FileStream(
                inputFilePath,
                FileMode.Open,
                FileAccess.Read
            ); //opening the input file in read mode

            using FileStream outputFile = new FileStream(
                outputFilePath,
                FileMode.Create,
                FileAccess.Write
            ); //opening the output file in writing mode

            // Store the IV at the beginning of the encrypted file
            await outputFile.WriteAsync(aes.IV);

            using CryptoStream cryptoStream = new CryptoStream(
                outputFile,
                aes.CreateEncryptor(), //acutal object which encript the file
                CryptoStreamMode.Write
            ); //we cant encrypt the normal file so we pass the normal file in crypto stream which will apply AES key and encript that and stores th
                // ecrypted data in output file

            await inputFile.CopyToAsync(cryptoStream);

            await cryptoStream.FlushFinalBlockAsync(); //We've reached the end of the file. Finish the encryption and write the remaining encrypted data.
                // it performs encrption block by block so flush the blockn after the encription of chunk
        }
    }
}