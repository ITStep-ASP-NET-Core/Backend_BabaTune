using BabaTune.Infrastructure.Interfaces;
using BabaTune.Infrastructure.Options;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using System.Net;

namespace BabaTune.Infrastructure.Repositories
{
	public class FirebaseStorageRepository : IStorageRepository
	{
		private readonly string _bucket;
		private readonly StorageClient _storageClient;

		public FirebaseStorageRepository ( IOptions<FirebaseStorageOptions> options )
		{
			_bucket = options.Value.BucketName;

			var googleCredential = CredentialFactory.FromFile<ServiceAccountCredential>(options.Value.Credentials!).ToGoogleCredential();

			_storageClient = StorageClient.Create(googleCredential);
		}

		public async Task<string> UploadAsync ( Stream fileStream, string fileName, string contentType, string folder )
		{
			var extension = Path.GetExtension(fileName);
			var objectName = $"{folder}/{Guid.NewGuid()}{extension}";
			var downloadToken = Guid.NewGuid().ToString();

			var storageObject = await _storageClient.UploadObjectAsync(
				bucket: _bucket,
				objectName: objectName,
				contentType: contentType,
				source: fileStream);

			storageObject.Metadata = new Dictionary<string, string>
			{
				["firebaseStorageDownloadTokens"] = downloadToken
			};
			await _storageClient.UpdateObjectAsync(storageObject);

			var encodedPath = Uri.EscapeDataString(objectName);
			return $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o/{encodedPath}?alt=media&token={downloadToken}";
		}

		public async Task<string> EditAsync ( string fileUrl, Stream fileStream, string contentType )
		{
			var (objectName, existingToken) = ParseFileUrl(fileUrl);
			if(objectName is null)
				throw new ArgumentException("The given URL is not a recognizable Firebase Storage file URL.", nameof(fileUrl));

			var token = existingToken ?? Guid.NewGuid().ToString();

			var storageObject = await _storageClient.UploadObjectAsync(
				bucket: _bucket,
				objectName: objectName,
				contentType: contentType,
				source: fileStream);

			storageObject.Metadata = new Dictionary<string, string>
			{
				["firebaseStorageDownloadTokens"] = token
			};
			await _storageClient.UpdateObjectAsync(storageObject);

			var encodedPath = Uri.EscapeDataString(objectName);
			return $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o/{encodedPath}?alt=media&token={token}";
		}

		public async Task DeleteAsync ( string fileUrl )
		{
			var (objectName, _) = ParseFileUrl(fileUrl);
			if(objectName is null)
				return;

			try
			{
				await _storageClient.DeleteObjectAsync(_bucket, objectName);
			}
			catch(GoogleApiException ex) when(ex.HttpStatusCode == HttpStatusCode.NotFound)
			{
			}
		}

		private static (string? ObjectName, string? Token) ParseFileUrl ( string fileUrl )
		{
			const string marker = "/o/";
			var markerIndex = fileUrl.IndexOf(marker, StringComparison.Ordinal);
			if(markerIndex < 0)
				return (null, null);

			var start = markerIndex + marker.Length;
			var queryIndex = fileUrl.IndexOf('?', start);
			var encodedPath = queryIndex < 0 ? fileUrl[start..] : fileUrl[start..queryIndex];
			var objectName = Uri.UnescapeDataString(encodedPath);

			if(queryIndex < 0)
				return (objectName, null);

			var query = fileUrl[(queryIndex + 1)..];
			var tokenParam = query
				.Split('&')
				.Select(p => p.Split('=', 2))
				.FirstOrDefault(p => p.Length == 2 && p[0] == "token");

			return (objectName, tokenParam?[1]);
		}
	}
}