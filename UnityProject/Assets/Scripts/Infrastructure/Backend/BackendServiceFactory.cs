namespace StockPicker.Infrastructure.Backend
{
    public static class BackendServiceFactory
    {
        public static (IAuthService auth, IScoreboardService scoreboard) Create(BackendConfig config, bool useLocalMock)
        {
            if (useLocalMock)
            {
                var auth = new LocalMockAuthService();
                var board = new LocalMockScoreboardService();
                return (auth, board);
            }

            var cloudAuth = new PlayFabGoogleAuthService(config);
            var cloudBoard = new PlayFabScoreboardService(config, cloudAuth);
            return (cloudAuth, cloudBoard);
        }
    }
}
