using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MagicApi.Services
{
    public class ImageService
    {
        private const string ContainerName = "run-images";
        private readonly Random _random;
        private readonly CloudBlobClient _blobClient;
        private readonly Dictionary<string, Bitmap> _images;

        public ImageService(string accountName, string accountKey)
        {
            if (string.IsNullOrWhiteSpace(accountName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(accountName));
            if (string.IsNullOrWhiteSpace(accountKey))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(accountKey));

            _random = new Random();

            // use the connection string to get the storage account
            var storageAccount = CloudStorageAccount.Parse($"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net");

            // using the storage account, create the blob client
            _blobClient = storageAccount.CreateCloudBlobClient();

            _images = new Dictionary<string, Bitmap>();
        }

        public string CreateImage(string imageName, int width, int height)
        {
            if (!_images.ContainsKey(imageName))
            {
                _images.Add(imageName, new Bitmap(width, height, PixelFormat.Format32bppArgb));
            }
            else
            {
                _images[imageName] = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            }

            return imageName;
        }

        public void DrawPoint(string imageName, int x, int y)
        {
            var bitmap = _images[imageName];

            var incrementColor = Color.FromArgb(
                255,
                255,
                _random.Next(128, 215),
                0);

            var currentColor = bitmap.GetPixel(x, y);

            var newColor = Color.FromArgb(
                (incrementColor.A + currentColor.A) / 2,
                (incrementColor.R + currentColor.R) / 2,
                (incrementColor.G + currentColor.G) / 2,
                (incrementColor.B + currentColor.B) / 2);

            bitmap.SetPixel(x, y, newColor);

            _images[imageName] = bitmap;
        }

        public Uri SaveImage(string imageName)
        {
            //  get the container reference
            var container = GetImagesBlobContainer();
            // using the container reference, get a block blob reference and set its type
            var blockBlob = container.GetBlockBlobReference(imageName);
            blockBlob.Properties.ContentType = "image/png";
            var stream = new MemoryStream();

            var bitmap = _images[imageName];

            bitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);
            blockBlob.UploadFromStream(stream);
            return blockBlob.Uri;
        }
        private CloudBlobContainer GetImagesBlobContainer()
        {
            var container = _blobClient.GetContainerReference(ContainerName);
            container.CreateIfNotExists();
            container.SetPermissions(
                new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
            return container;
        }
    }
}
