namespace Server.Dtos
{
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public required string Token { get; set; }
    }
}
