using System;

[Serializable] public class LoginRequest { public string username; public string password; }
[Serializable] public class LoginResponse { public string accessToken; public string refreshToken; }

[Serializable] public class RefreshRequest { public string refreshToken; }
[Serializable] public class RefreshResponse { public string accessToken; public string refreshToken; }
