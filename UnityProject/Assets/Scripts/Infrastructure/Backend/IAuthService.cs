using System;
using System.Threading;
using System.Threading.Tasks;

namespace StockPicker.Infrastructure.Backend
{
    public interface IAuthService
    {
        AuthState State { get; }
        event Action<AuthState> StateChanged;

        Task<AuthResult> SignInAsync(CancellationToken cancellationToken = default);
        void SignOut();
    }
}
