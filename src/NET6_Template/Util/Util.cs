using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NET6_Template
{
    public static class Util
    {
        public static string ParseCompressedOrUncompressedMessage(byte[] input, bool isBase64Encoded = false)
        {
            try
            {
                return UnZipStr(input, isBase64Encoded);
            }
            catch
            {
                if(isBase64Encoded)
                {
                    try
                    {
                        return Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(input)));
                    }
                    catch
                    {
                        return Encoding.UTF8.GetString(input);
                    }
                }
                    
                return Encoding.UTF8.GetString(input);
            }
        }

        public static string UnZipStr(byte[] input, bool isBase64Encoded = false)
        {
            if(isBase64Encoded)
                input = Convert.FromBase64String(Encoding.UTF8.GetString(input));

            using MemoryStream inputStream = new(input);
            using DeflateStream gzip =
                new(inputStream, CompressionMode.Decompress);
            using StreamReader reader =
                new(gzip, Encoding.UTF8);

            return reader.ReadToEnd();
        }
    }
}
