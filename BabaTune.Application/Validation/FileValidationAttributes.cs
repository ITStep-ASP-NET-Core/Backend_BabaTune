using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using static BabaTune.Application.Common.Constants;

namespace BabaTune.Application.Validation
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
	public abstract class FileValidationAttribute : ValidationAttribute
	{
		private readonly string _contentTypePrefix;
		private readonly long _maxSize;
		private readonly string[] _extensions;

		protected FileValidationAttribute ( string contentTypePrefix, long maxSize, string[] extensions )
		{
			_contentTypePrefix = contentTypePrefix;
			_maxSize = maxSize;
			_extensions = extensions;
		}

		protected override ValidationResult? IsValid ( object? value, ValidationContext validationContext )
		{
			if (value is null)
				return ValidationResult.Success;

			var members = validationContext.MemberName is null ? null : new[] { validationContext.MemberName };

			if (value is not IFormFile file)
				return new ValidationResult("Invalid file.", members);

			if (file.Length == 0)
				return new ValidationResult("File is empty.", members);

			if (file.Length > _maxSize)
				return new ValidationResult($"File must not exceed {_maxSize / 1024 / 1024} MB.", members);

			if (!file.ContentType.StartsWith(_contentTypePrefix, StringComparison.OrdinalIgnoreCase))
				return new ValidationResult($"File must be of type {_contentTypePrefix}*.", members);

			var extension = Path.GetExtension(file.FileName);
			if (!_extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
				return new ValidationResult($"Allowed extensions: {string.Join(", ", _extensions)}.", members);

			return ValidationResult.Success;
		}
	}

	public sealed class ImageFileAttribute : FileValidationAttribute
	{
		public ImageFileAttribute ( )
			: base("image/", Limits.MaxImageSize, [".jpg", ".jpeg", ".png", ".webp", ".gif"]) { }
	}

	public sealed class AudioFileAttribute : FileValidationAttribute
	{
		public AudioFileAttribute ( )
			: base("audio/", Limits.MaxAudioSize, [".mp3", ".wav", ".flac", ".ogg", ".m4a", ".aac"]) { }
	}
}