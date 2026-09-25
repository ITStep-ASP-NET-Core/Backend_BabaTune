using System.ComponentModel.DataAnnotations;

namespace BabaTune.WebApi.Requests;

public record RefreshRequest ( [Required] string RefreshToken );

public record UpdatePasswordRequest ( [Required] string CurrentPassword, [Required] string NewPassword );

public record MoveSongRequest ( int NewPosition );
