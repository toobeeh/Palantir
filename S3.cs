using System;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Palantir
{
    internal class S3Handler
    {

        private BasicAWSCredentials credentials = new BasicAWSCredentials(Program.S3AccessKey, Program.S3SecretKey);

        private AmazonS3Config config = new AmazonS3Config()
        {
            ServiceURL = "https://eu2.contabostorage.com/",
        };

        public async Task<string> UploadPng(string path, string key)
        {
            key = key + ".png";
            var client = new AmazonS3Client(credentials, config);
            PutObjectRequest putRequest = new PutObjectRequest
            {
                BucketName = "palantir",
                Key = key,
                FilePath = path,
                ContentType = "image/png",
            };

            PutObjectResponse response = await client.PutObjectAsync(putRequest);

            return "https://eu2.contabostorage.com/45a0651c8baa459daefd432c0307bb5b:palantir/" + key;
        }
    }
}
