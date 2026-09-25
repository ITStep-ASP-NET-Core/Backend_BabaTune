using BabaTune.Application.Interfaces;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace BabaTune.Application.Implementations
{
	public class Argon2PasswordHasher : IPasswordHasher
	{
		private const int SaltSize = 16;
		private const int HashSize = 32;
		private const int DegreeOfParallelism = 8;
		private const int Iterations = 4;
		private const int MemorySize = 1024 * 128;

		public string HashPassword ( string password )
		{
			byte[] salt = new byte[SaltSize];
			using(var rng = RandomNumberGenerator.Create())
				rng.GetBytes(salt);

			byte[] hash = ComputeHash(password, salt);

			var combined = new byte[SaltSize + HashSize];
			Array.Copy(salt, 0, combined, 0, SaltSize);
			Array.Copy(hash, 0, combined, SaltSize, HashSize);

			return Convert.ToBase64String(combined);
		}

		public bool VerifyPassword ( string password, string hashedPassword )
		{
			byte[] combined = Convert.FromBase64String(hashedPassword);

			byte[] salt = new byte[SaltSize];
			byte[] hash = new byte[HashSize];
			Array.Copy(combined, 0, salt, 0, SaltSize);
			Array.Copy(combined, SaltSize, hash, 0, HashSize);

			byte[] newHash = ComputeHash(password, salt);

			return CryptographicOperations.FixedTimeEquals(hash, newHash);
		}

		private byte[] ComputeHash ( string password, byte[] salt )
		{
			var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
			{
				Salt = salt,
				DegreeOfParallelism = DegreeOfParallelism,
				Iterations = Iterations,
				MemorySize = MemorySize
			};

			return argon2.GetBytes(HashSize);
		}
	}
}
