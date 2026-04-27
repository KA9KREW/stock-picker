using System;

namespace StockPicker.Infrastructure.Backend
{
    public enum AuthPhase
    {
        SignedOut = 0,
        SigningIn = 1,
        SignedIn = 2,
        Error = 3
    }

    [Serializable]
    public struct AuthState
    {
        public AuthPhase Phase;
        public string PlayerId;
        public string DisplayName;
        public string StatusMessage;

        public bool IsAuthenticated => Phase == AuthPhase.SignedIn && !string.IsNullOrWhiteSpace(PlayerId);

        public static AuthState SignedOut(string status) => new()
        {
            Phase = AuthPhase.SignedOut,
            PlayerId = string.Empty,
            DisplayName = "Guest",
            StatusMessage = status
        };
    }

    public readonly struct AuthResult
    {
        public readonly bool Success;
        public readonly string Error;
        public readonly AuthState State;

        public AuthResult(bool success, string error, AuthState state)
        {
            Success = success;
            Error = error ?? string.Empty;
            State = state;
        }
    }
}
